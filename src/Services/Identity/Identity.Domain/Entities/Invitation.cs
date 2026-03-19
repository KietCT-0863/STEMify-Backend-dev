using Identity.Domain.Common;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents an invitation for a user to join an organization
/// </summary>
public class Invitation : BaseEntity<Guid>
{
    public int OrganizationId { get; private set; } 
    public Email InviteeEmail { get; private set; } = null!;
    public InvitationToken Token { get; private set; } = null!;
    public InvitationStatus Status { get; private set; }

    // Target user configuration
    public OrganizationRole TargetRole { get; private set; } // Student, Teacher, OrganizationAdmin
    public string LicenseType { get; private set; } = null!; // "Student", "Teacher", "OrganizationAdmin"

    // CSV import data
    public string? FullName { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? GroupName { get; private set; }
    public string? ExternalId { get; private set; }

    // Subscription reference 
    public int? SubscriptionOrderId { get; private set; }

    // Metadata
    public Guid InvitedBy { get; private set; }
    public DateTime InvitedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    
    public DateTime? ScheduledSendDate { get; private set; }

    public DateTime? SentAt { get; private set; } 
    public DateTime? AcceptedAt { get; private set; } 
    public Guid? AcceptedUserId { get; private set; } 
    public Guid? ProcessedByJobId { get; private set; } // Which bulk import job processed this

    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; }

    private Invitation() { }

    /// <summary>
    /// Factory method to create a new invitation
   /// </summary>
    public static Invitation Create(
        int organizationId,
        string email,
        OrganizationRole targetRole,
        string licenseType,
        Guid invitedBy,
        Guid? processedByJobId = null,
        string? fullName = null,
        string? firstName = null,
        string? lastName = null,
        string? groupName = null,
        string? externalId = null,
        int? subscriptionOrderId = null,
        int expirationDays = 7,
        DateTime? scheduledSendDate = null)
    {
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            InviteeEmail = Email.Create(email),
            Token = InvitationToken.Generate(),
            Status = InvitationStatus.Pending,
            TargetRole = targetRole,
            LicenseType = licenseType,
            FullName = fullName,
            FirstName = firstName,
            LastName = lastName,
            GroupName = groupName,
            ExternalId = externalId,
            SubscriptionOrderId = subscriptionOrderId,
            InvitedBy = invitedBy,
            InvitedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            ScheduledSendDate = scheduledSendDate,
            ProcessedByJobId = processedByJobId,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        invitation.AddDomainEvent(new InvitationCreatedEvent(
            invitationId: invitation.Id,
            organizationId: organizationId,
            inviteeEmail: email,
            targetRole: targetRole,
            licenseType: licenseType,
            processedByJobId: processedByJobId
        ));

        return invitation;
    }

    /// <summary>
    /// Mark invitation as sent after email delivery
    /// Business rule: Can only mark as sent if status is Pending
    /// </summary>
    public void MarkAsSent()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot mark invitation as sent. Current status: {Status}"
            );

        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark invitation as accepted when user completes registration
    /// Business rule: Must be sent and not expired
    /// </summary>
    public void MarkAsAccepted(Guid userId)
    {
        if (IsExpired())
            throw new InvalidOperationException(
                $"Cannot accept expired invitation. Expired at: {ExpiresAt}"
            );

        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot accept invitation with status: {Status}"
            );

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedUserId = userId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new InvitationAcceptedEvent(
            invitationId: Id,
            organizationId: OrganizationId,
            userId: userId,
            userEmail: InviteeEmail.Value,
            targetRole: TargetRole,
            licenseType: LicenseType
        ));
    }

    /// <summary>
    /// Mark invitation as failed with reason
    /// Used when email delivery fails or validation fails
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = InvitationStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;

        // Domain event for monitoring
        // AddDomainEvent(new InvitationFailedEvent(Id, reason));
    }

    /// <summary>
    /// Mark invitation as expired
    /// Called by scheduled cleanup job
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status == InvitationStatus.Pending && IsExpired())
        {
            Status = InvitationStatus.Expired;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new InvitationExpiredEvent(
                invitationId: Id,
                organizationId: OrganizationId,
                inviteeEmail: InviteeEmail.Value,
                expiresAt: ExpiresAt
            ));
        }
    }

    /// <summary>
    /// Revoke an invitation 
    /// </summary>
    public void Revoke()
    {
        if (Status == InvitationStatus.Accepted)
            throw new InvalidOperationException("Cannot revoke accepted invitation");

        Status = InvitationStatus.Revoked;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if invitation has expired
    /// Business rule: Invitation expires after ExpiresAt date
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Check if invitation can be retried (for failed email sends)
    /// Business rule: Max 3 retries
    /// </summary>
    public bool CanRetry(int maxRetries = 3)
    {
        return Status == InvitationStatus.Failed && RetryCount < maxRetries;
    }

    /// <summary>
    /// Increment retry count for failed invitations
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset invitation to pending for retry
    /// </summary>
    public void ResetForRetry()
    {
        if (!CanRetry())
            throw new InvalidOperationException("Cannot retry invitation");

        Status = InvitationStatus.Pending;
        FailureReason = null;
        IncrementRetryCount();
    }

    /// <summary>
    /// Get time remaining until expiration
    /// </summary>
    public TimeSpan GetTimeUntilExpiration()
    {
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Check if invitation was processed by a bulk import job
    /// </summary>
    public bool IsBulkImport()
    {
        return ProcessedByJobId.HasValue;
    }

    public bool IsScheduled()
    {
        return ScheduledSendDate.HasValue && ScheduledSendDate.Value > DateTime.UtcNow;
    }

    public bool ShouldSendToday()
    {
        if (!ScheduledSendDate.HasValue)
            return false; 

        return ScheduledSendDate.Value.Date <= DateTime.UtcNow.Date;
    }
}
