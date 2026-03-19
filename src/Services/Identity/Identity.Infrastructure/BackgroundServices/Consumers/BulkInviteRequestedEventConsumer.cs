using Common.Logging.Metrics;
using Contracts.Abstractions.Services;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Dtos.Grpc;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Identity.Application.Common.Interfaces.Grpc;
using GroupEntity = Identity.Domain.Entities.Group;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Consumer that processes bulk invitation requests asynchronously
/// Reads CSV data from BulkImportJob and creates individual invitations
/// </summary>
public class BulkInviteRequestedEventConsumer : IConsumer<BulkInviteRequestedEvent>
{
    private readonly IBulkImportJobRepository _jobRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IInvitationEmailService _invitationEmailService;
    private readonly ILogger<BulkInviteRequestedEventConsumer> _logger;
    private const int BatchSize = 50;
    private const int MaxRetries = 3;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
    private static readonly JsonSerializerOptions CsvSerializationOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public BulkInviteRequestedEventConsumer(
        IBulkImportJobRepository jobRepository,
        IInvitationRepository invitationRepository,
        IInvitationEmailService invitationEmailService,
        IOrderLicenseService orderLicenseService,
        IIdentityUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IOrganizationUserRepository organizationUserRepository,
        IGroupRepository groupRepository,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        ILogger<BulkInviteRequestedEventConsumer> logger)
    {
        _jobRepository = jobRepository;
        _invitationRepository = invitationRepository;
        _invitationEmailService = invitationEmailService;
        _orderLicenseService = orderLicenseService;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _organizationUserRepository = organizationUserRepository;
        _groupRepository = groupRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BulkInviteRequestedEvent> context)
    {
        var @event = context.Message;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. Get job from database
            var job = await _jobRepository.FindByIdAsync(@event.BulkImportJobId);
            if (job == null)
            {
                _logger.LogError("Job {JobId} not found", @event.BulkImportJobId);
                return;
            }

            // 2. Mark job as started
            job.Start();
            await _jobRepository.UpdateAsync(job);
            
            IdentityMetrics.RecordBulkInvitationJob("processing");

        var organizationProvisioning =
            await _orderLicenseService.GetOrganizationForBulkProvisioningAsync(
                @event.OrganizationId,
                context.CancellationToken);

        var organizationDetails = await _orderLicenseService.GetOrganizationAsync(
            @event.OrganizationId,
            context.CancellationToken);

        // Use domain service to build organization code prefix
        var organizationCode = GroupCodeBuilder.BuildOrganizationCodePrefix(
            organizationDetails?.Code,
            @event.OrganizationId);

        var groupCache = new Dictionary<string, GroupEntity>(StringComparer.OrdinalIgnoreCase);

            // 3. Deserialize CSV data
            var csvRows = JsonSerializer.Deserialize<List<CsvInvitationRowData>>(job.CsvDataJson, CsvSerializationOptions);
            if (csvRows == null || !csvRows.Any())
            {
                _logger.LogError("No CSV data found in job {JobId}", job.Id);
                job.MarkAsFailed("No CSV data to process");
                await _jobRepository.UpdateAsync(job);
                return;
            }

            _logger.LogInformation(
                "Job {JobId}: Processing {Count} invitations in batches of {BatchSize}",
                job.Id,
                csvRows.Count,
                BatchSize);

            // 4. Process invitations in batches
            var batches = csvRows.Chunk(BatchSize);

            foreach (var batch in batches)
            {
                await ProcessBatchAsync(
                    job,
                    batch.ToList(),
                    organizationProvisioning,
                    organizationCode,
                    groupCache,
                    @event.OrganizationId,
                    @event.RequestedBy,
                    context.CancellationToken);
            }

            // 5. Job will be marked as completed automatically via CheckCompletion() in RecordSuccess/RecordFailure

            stopwatch.Stop();
            IdentityMetrics.RecordBulkProvisioningDuration("process", stopwatch.Elapsed);
            
            var jobStatus = job.Status.ToString().ToLower();
            IdentityMetrics.RecordBulkInvitationJob(jobStatus);

            _logger.LogInformation(
                "Job {JobId} completed: {SuccessCount} succeeded, {FailedCount} failed",
                job.Id,
                job.SuccessCount,
                job.FailedCount);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            IdentityMetrics.RecordBulkProvisioningDuration("process", stopwatch.Elapsed);
            IdentityMetrics.RecordBulkInvitationJob("failed");
            
            _logger.LogError(ex, "Error processing BulkInviteRequestedEvent for job {JobId}", @event.BulkImportJobId);

            // Mark job as failed
            var job = await _jobRepository.FindByIdAsync(@event.BulkImportJobId);
            if (job != null)
            {
                job.MarkAsFailed(ex.Message);
                await _jobRepository.UpdateAsync(job);
            }
        }
    }

    private async Task ProcessBatchAsync(
        BulkImportJob job,
        List<CsvInvitationRowData> batch,
        OrganizationBulkProvisioningDto organization,
        string organizationCode,
        Dictionary<string, GroupEntity> groupCache,
        int organizationId,
        Guid requestedBy,
        CancellationToken cancellationToken = default)
    {
        int subscriptionOrderIdToUse;
        SubscriptionLicenseInfoDto? selectedSubscription = null;
        
        if (job.SubscriptionOrderId.HasValue)
        {
            var specified = organization.Subscriptions.FirstOrDefault(s => s.SubscriptionOrderId == job.SubscriptionOrderId.Value);
            if (specified != null)
            {
                subscriptionOrderIdToUse = specified.SubscriptionOrderId;
                selectedSubscription = specified;
            }
            else
            {
                var fallback = organization.Subscriptions.FirstOrDefault(s => s.IsActive);
                if (fallback == null)
                {
                    _logger.LogError("Specified subscription {SubId} not valid/active and no active subscription found for organization {OrganizationId}", job.SubscriptionOrderId, organizationId);
                    throw new InvalidOperationException($"No valid subscription found for organization {organizationId}");
                }
                _logger.LogWarning("Job {JobId} provided SubscriptionOrderId {SubId} but it is not valid/active for organization {OrganizationId}. Falling back to {FallbackSubId}", job.Id, job.SubscriptionOrderId, organizationId, fallback.SubscriptionOrderId);
                subscriptionOrderIdToUse = fallback.SubscriptionOrderId;
                selectedSubscription = fallback;
            }
        }
        else
        {
            var active = organization.Subscriptions.FirstOrDefault(s => s.IsActive);
            if (active == null)
            {
                 throw new InvalidOperationException($"No active subscription found for organization {organizationId}");
            }
            subscriptionOrderIdToUse = active.SubscriptionOrderId;
            selectedSubscription = active;
        }
        DateTime? scheduledSendDate = null;
        if (selectedSubscription?.StartDate.HasValue == true)
        {
            var startDate = selectedSubscription.StartDate.Value;
            if (startDate > DateTime.UtcNow)
            {
                scheduledSendDate = startDate.Date.AddHours(9);
            }
           
        }
       

        var invitations = new List<Invitation>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var row in batch)
        {
            var retryCount = 0;
            var success = false;

            while (retryCount < MaxRetries && !success)
            {
                try
                {
                    var existingInvitation = await _invitationRepository.ExistsForEmailAndSubscriptionAsync(
                        organizationId,
                        row.Email,
                        subscriptionOrderIdToUse,
                        cancellationToken);

                    if (existingInvitation)
                    {
                       
                        successCount++;
                        success = true;
                        continue;
                    }

                    //  Get or create user (Status = Pending)
                    var user = await _userRepository.GetByEmailAsync(row.Email);
                    if (user == null)
                    {
                        // Create user with Pending status
                        var firstName = row.FirstName ?? "User";
                        var lastName = row.LastName ?? string.Empty;
                        var password = $"{Guid.NewGuid():N}aA!1"; // Temporary password

                        var newUser = User.Create(
                            id: Guid.NewGuid(),
                            email: row.Email,
                            userName: row.Email,
                            firstName: firstName,
                            lastName: lastName,
                            role: UserRole.Member);

                        newUser.Activate();

                        var createResult = await _userManager.CreateAsync(newUser, password);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            throw new InvalidOperationException($"Failed to create user: {errors}");
                        }

                        await _userManager.AddToRoleAsync(newUser, UserRole.Member.ToString());
                        user = newUser;

                        _logger.LogInformation(
                            "Created user {UserId} with Pending status for {Email} (Job: {JobId})",
                            user.Id, row.Email, job.Id);
                    }
                    else
                    {
                        // // User exists - ensure it's in Pending status if not active
                        // if (user.Status != UserStatus.Active && user.Status != UserStatus.Pending)
                        // {
                        //     _logger.LogWarning(
                        //         "User {UserId} exists but with status {Status}. Skipping invitation.",
                        //         user.Id, user.Status);
                        //     successCount++;
                        //     success = true;
                        //     continue;
                        // }
                        // _logger.LogDebug(
                        //     "User {UserId} already exists for {Email} (Job: {JobId})",
                        //     user.Id, row.Email, job.Id);
                    }

                var existingOrgUser = await _organizationUserRepository.GetByUserAndOrganizationAsync(
                    user.Id,
                    organizationId,
                    cancellationToken);

                var licenseType = string.IsNullOrEmpty(row.LicenseType) ? row.Role.ToString() : row.LicenseType;

                OrganizationUser orgUser;
                if (existingOrgUser != null)
                {
                    orgUser = existingOrgUser;

                    _logger.LogInformation(
                        "Reusing existing OrganizationUser {OrgUserId} for UserId {UserId} in Organization {OrganizationId} to reserve license for subscription {SubscriptionOrderId}.",
                        orgUser.Id, user.Id, organizationId, subscriptionOrderIdToUse);
                }
                else
                {
                    orgUser = OrganizationUser.CreatePending(
                        organizationId: organizationId,
                        userId: user.Id,
                        organizationRole: row.Role,
                        licenseType: licenseType,
                        licenseAssignmentId: null,
                        subscriptionOrderId: subscriptionOrderIdToUse);

                    await _organizationUserRepository.AddAsync(orgUser, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var licenseResult = await _orderLicenseService.ReserveLicenseAsync(
                    organizationId,
                    orgUser.Id.ToString(),
                    licenseType,
                    subscriptionOrderIdToUse,
                    cancellationToken);

                    if (!licenseResult.Success)
                    {
                        throw new InvalidOperationException(
                            $"Failed to reserve license: {licenseResult.ErrorMessage}");
                    }

                // Always ensure group assignment is created/updated when a group code is provided
                var targetGroup = await GetOrCreateGroupAsync(
                    row.GroupCode,
                    row.GroupName,
                    row.Grade,
                    organizationCode,
                    organizationId,
                    requestedBy,
                    groupCache,
                    cancellationToken);

                if (targetGroup != null)
                {
                    if (orgUser.GroupId != targetGroup.Id)
                    {
                        orgUser.AssignToGroup(targetGroup);
                    }
                }

                    // orgUser is already persisted before license reservation to ensure
                    // license events can be correctly projected to the read model.

                    var invitation = Invitation.Create(
                        organizationId: organizationId,
                        email: row.Email,
                        targetRole: row.Role,
                        licenseType: licenseType,
                        invitedBy: requestedBy,
                        processedByJobId: job.Id,
                        fullName: row.FullName,
                        firstName: row.FirstName,
                        lastName: row.LastName,
                        groupName: row.GroupName,
                        externalId: row.ExternalId,
                        subscriptionOrderId: subscriptionOrderIdToUse,
                        expirationDays: 30,
                        scheduledSendDate: scheduledSendDate
                    );

                    if (invitation == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to create invitation for {row.Email}. Invitation.Create returned null.");
                    }

                    invitations.Add(invitation);
                    successCount++;
                    success = true;

                    _logger.LogDebug(
                        "Successfully prepared invitation for {Email} (Job: {JobId}). UserId: {UserId}, OrgUserId: {OrgUserId}, LicenseAssignmentId: {LicenseId}",
                        row.Email,
                        job.Id,
                        user.Id,
                        orgUser.Id,
                        licenseResult.LicenseAssignmentId);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount >= MaxRetries)
                    {
                        failureCount++;
                        job.RecordFailure(row.Email, ex.Message);
                    }
                    else
                    {
                        // Wait before retry with exponential backoff
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                        await Task.Delay(delay);

                        _logger.LogWarning(
                            "Retrying invitation for {Email} (Attempt {Attempt}/{MaxRetries})",
                            row.Email,
                            retryCount,
                            MaxRetries);
                    }
                }
            }
        }

        try
        {
            // Add all invitations to ChangeTracker
           
            foreach (var invitation in invitations)
            {
                if (invitation == null)
                {
                    _logger.LogWarning(
                        "Skipping null invitation in batch save for job {JobId}",
                        job.Id);
                    continue;
                }

                await _invitationRepository.AddAsync(invitation);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

             var emailSentCount = 0;
            var emailFailedCount = 0;
            var scheduledCount = 0;
            
            foreach (var invitation in invitations)
            {
                if (invitation == null) continue;

                // Only send email immediately if not scheduled or scheduled date is today/past
                if (invitation.IsScheduled())
                {
                    scheduledCount++;
                    _logger.LogInformation(
                        "Invitation {InvitationId} for {Email} is scheduled for {ScheduledDate}. Email will be sent by scheduled job.",
                        invitation.Id,
                        invitation.InviteeEmail.Value,
                        invitation.ScheduledSendDate);
                    continue;
                }

                try
                {
                    await _invitationEmailService.SendInvitationEmailAsync(
                        invitation,
                        organizationId,
                        cancellationToken);
                    
                    invitation.MarkAsSent(); 
                    emailSentCount++;
                     
                    IdentityMetrics.RecordBulkInvitationProcessed("sent");
                }
                catch (Exception emailEx)
                {
                    emailFailedCount++;
                    IdentityMetrics.RecordBulkInvitationProcessed("failed");
                    
                    // Record email send failure in job
                    job.RecordFailure(
                        invitation.InviteeEmail.Value,
                        $"Email send failed: {emailEx.Message}");
                    
                    _logger.LogWarning(emailEx,
                        "Failed to send email for invitation {Email} (Job: {JobId}). Invitation saved but email not sent.",
                        invitation.InviteeEmail.Value,
                        job.Id);
                }
            }
            
            if (emailSentCount > 0 || emailFailedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }
           
            
            for (int i = 0; i < successCount; i++)
            {
                job.RecordSuccess();
            }

            _logger.LogInformation(
                "Job {JobId}: Batch saved successfully. Saved {Count} invitations ({EmailSentCount} emails sent immediately, {ScheduledCount} scheduled, {EmailFailedCount} emails failed)",
                job.Id,
                invitations.Count,
                emailSentCount,
                scheduledCount,
                emailFailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to save batch for job {JobId}. {Count} invitations were prepared but not saved. No emails were sent.",
                job.Id,
                invitations.Count);

            // Record failures for all invitations that couldn't be saved
            // Note: Individual row failures are already recorded above with specific email
            // This is for system-level failures during batch save
            for (int i = 0; i < successCount; i++)
            {
                job.RecordFailure("BATCH_SAVE_ERROR", $"Failed to save batch: {ex.Message}");
            }

            throw;
        }
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Job {JobId}: Batch completed. Progress: {Processed}/{Total} ({Percentage}%), Success: {SuccessCount}, Failed: {FailedCount}",
            job.Id,
            job.ProcessedCount,
            job.TotalCount,
            job.ProgressPercentage,
            successCount,
        failureCount);
    }

    private async Task<GroupEntity?> GetOrCreateGroupAsync(
        string? requestedGroupCode,
        string? groupName,
        GroupGrade? grade,
        string organizationCode,
        int organizationId,
        Guid requestedBy,
        IDictionary<string, GroupEntity> groupCache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedGroupCode))
            return null;

        var finalCode = GroupCodeBuilder.BuildFullGroupCode(organizationCode, requestedGroupCode, organizationId);
        if (groupCache.TryGetValue(finalCode, out var cachedGroup))
            return cachedGroup;

        var existingGroup = await _groupRepository.GetByOrganizationAndCodeAsync(
            organizationId,
            finalCode,
            cancellationToken);

        if (existingGroup != null)
        {
            if (!string.IsNullOrWhiteSpace(groupName) &&
                !string.Equals(existingGroup.Name, groupName.Trim(), StringComparison.Ordinal))
            {
                existingGroup.UpdateInfo(groupName.Trim(), existingGroup.Description);
            }
            groupCache[finalCode] = existingGroup;
            return existingGroup;
        }

        var finalName = !string.IsNullOrWhiteSpace(groupName)
            ? groupName!.Trim()
            : requestedGroupCode.Trim();

        if (string.IsNullOrWhiteSpace(finalName))
        {
            finalName = finalCode;
        }

        // Use domain factory method that applies code building rules
        var newGroup = GroupEntity.CreateWithCode(
            organizationId,
            finalName,
            requestedBy,
            organizationCode,
            requestedGroupCode,
            description: null,
            grade: grade);

        await _groupRepository.AddAsync(newGroup, cancellationToken);
        groupCache[finalCode] = newGroup;
        return newGroup;
    }


}

/// <summary>
/// Data class for deserializing CSV rows from JSON
/// </summary>
public class CsvInvitationRowData
{
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public string LicenseType { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? GroupName { get; set; }
    public string? ExternalId { get; set; }
    public string? GroupCode { get; set; }
    public GroupGrade? Grade { get; set; }
}
