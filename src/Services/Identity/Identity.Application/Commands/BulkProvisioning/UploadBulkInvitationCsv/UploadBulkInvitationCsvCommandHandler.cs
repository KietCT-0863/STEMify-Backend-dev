using System.Text.Json;
using Common.Logging.Metrics;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.BulkProvisioning.UploadBulkInvitationCsv;

public class UploadBulkInvitationCsvCommandHandler
    : IRequestHandler<UploadBulkInvitationCsvCommand, BulkImportJobDto>
{
    private readonly IBulkImportJobRepository _jobRepository;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly ICsvParserService _csvParser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<UploadBulkInvitationCsvCommandHandler> _logger;

    public UploadBulkInvitationCsvCommandHandler(
        IBulkImportJobRepository jobRepository,
        IOrderLicenseService orderLicenseService,
        ICsvParserService csvParser,
        IIdentityUnitOfWork unitOfWork,
        ILogger<UploadBulkInvitationCsvCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _orderLicenseService = orderLicenseService;
        _csvParser = csvParser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkImportJobDto> Handle(
        UploadBulkInvitationCsvCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing bulk invitation CSV upload for organization {OrganizationId}",
            request.OrganizationId);

        // 1. Get organization info with email domain
        var organization = await _orderLicenseService.GetOrganizationForBulkProvisioningAsync(
            request.OrganizationId,
            cancellationToken);

        if (!organization.IsActive)
        {
            throw new InvalidOperationException(
                $"Organization {request.OrganizationId} is not active");
        }
        if (request.SubscriptionOrderId.HasValue)
        {
            var isSubscriptionInOrganization =
                organization.Subscriptions != null &&
                organization.Subscriptions.Any(s => s.SubscriptionOrderId == request.SubscriptionOrderId.Value);

            if (!isSubscriptionInOrganization)
            {
                throw new InvalidOperationException(
                    $"SubscriptionOrderId {request.SubscriptionOrderId.Value} is not in {request.OrganizationId}");
            }
        }

        //if (string.IsNullOrEmpty(organization.EmailDomain))
        //{
        //    throw new InvalidEmailDomainException(
        //        "Organization does not have an email domain configured");
        //}

        _logger.LogInformation(
            "Organization {OrganizationName} email domain: {EmailDomain}",
            organization.Name,
            organization.EmailDomain);

        // 2. Parse CSV file
        using var stream = request.CsvFile.OpenReadStream();
        var parseResult = await _csvParser.ParseBulkInvitationCsvAsync(
            stream,
            organization.EmailDomain,
            cancellationToken);

        if (!parseResult.Success || parseResult.HasErrors)
        {
            var errorDetails = string.Join("; ",
                parseResult.Errors.Take(5).Select(e =>
                    $"Row {e.RowNumber}: {e.ErrorMessage}"));

            var errorDataList = parseResult.Errors.Select(e => new CsvParseErrorData
            {
                RowNumber = e.RowNumber,
                FieldName = e.FieldName,
                ErrorMessage = e.ErrorMessage,
                RawValue = e.RawValue
            }).ToList();

            throw new InvalidCsvDataException(
                totalRowCount: parseResult.TotalRows,
                validRowCount: parseResult.ValidRowCount,
                errors: errorDataList,
                summaryMessage: $"CSV validation failed. {parseResult.ErrorCount} errors found. First errors: {errorDetails}");
        }

        if (parseResult.ValidRowCount == 0)
        {
            throw new InvalidCsvDataException("CSV file contains no valid rows");
        }

        _logger.LogInformation(
            "CSV parsed successfully. {ValidCount} valid rows",
            parseResult.ValidRowCount);

        // 3. Group invitations by license type and check availability
        var licenseRequests = parseResult.ValidRows
            .GroupBy(r => r.GetLicenseType())
            .ToDictionary(g => g.Key, g => g.Count());

        var licenseCheck = await _orderLicenseService.BulkCheckLicensesAsync(
            request.OrganizationId,
            licenseRequests,
            cancellationToken);

        if (!licenseCheck.AllAvailable)
        {
            var unavailableTypes = licenseCheck.Results
                .Where(r => !r.Value.Available)
                .Select(r => $"{r.Key} (requested: {r.Value.RequestedCount}, available: {r.Value.AvailableCount})")
                .ToList();

            throw new LicenseAllocationException(
                request.OrganizationId,
                parseResult.ValidRowCount,
                licenseCheck.Results.Sum(r => r.Value.AvailableCount));
        }

        _logger.LogInformation(
            "License availability check passed for {Count} invitations",
            parseResult.ValidRowCount);

        // 4. Serialize CSV data to JSON for background worker
        var csvDataJson = JsonSerializer.Serialize(
            parseResult.ValidRows,
            new JsonSerializerOptions
            {
                WriteIndented = false,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        // 5. Create BulkImportJob entity (will publish domain events)
        var job = BulkImportJob.Create(
            organizationId: request.OrganizationId,
            csvDataJson: csvDataJson,
            totalCount: parseResult.ValidRowCount,
            createdBy: request.CreatedBy,
            subscriptionOrderId: request.SubscriptionOrderId);

        // 6. Save to database with Outbox pattern
        await _jobRepository.AddAsync(job, cancellationToken);

        _logger.LogInformation(
            "Saving BulkImportJob with {Count} domain events - will be published via Outbox in DbContext",
            job.DomainEvents.Count);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Bulk import job {JobId} created successfully for organization {OrganizationId}. " +
            "{TotalCount} invitations will be processed asynchronously.",
            job.Id,
            request.OrganizationId,
            job.TotalCount);

        IdentityMetrics.RecordBulkInvitationJob("created");

        // 7. Return DTO
        return new BulkImportJobDto
        {
            Id = job.Id,
            OrganizationId = job.OrganizationId,
            Status = job.Status,
            TotalCount = job.TotalCount,
            ProcessedCount = job.ProcessedCount,
            SuccessCount = job.SuccessCount,
            FailedCount = job.FailedCount,
            ProgressPercentage = job.ProgressPercentage,
            CreatedBy = job.CreatedBy,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Duration = job.ProcessingDuration
        };
    }
}
