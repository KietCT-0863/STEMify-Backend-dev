using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Order.Domain.Enums;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.BulkCheckLicenses;

public class BulkCheckLicensesQueryHandler
    : IRequestHandler<BulkCheckLicensesQuery, BulkCheckLicensesResponse>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ILogger<BulkCheckLicensesQueryHandler> _logger;

    public BulkCheckLicensesQueryHandler(
        IOrderUnitOfWork unitOfWork,
        ILogger<BulkCheckLicensesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkCheckLicensesResponse> Handle(
        BulkCheckLicensesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Bulk checking licenses for organization {OrganizationId}, {Count} license type(s)",
            request.OrganizationId, request.LicenseRequests.Count);

        var response = new BulkCheckLicensesResponse
        {
            AllAvailable = true,
            Message = string.Empty
        };

        // Get organization's active subscriptions
        var orgSpec = new OrganizationByIdSpecification(request.OrganizationId);
        var organization = await _unitOfWork.Organizations.FirstOrDefaultAsync(orgSpec, cancellationToken);

        if (organization == null)
        {
            response.AllAvailable = false;
            response.Message = $"Organization with ID {request.OrganizationId} not found.";
            return response;
        }

        // Get all active subscriptions for this organization
        var activeSubscriptions = (organization.SubscriptionOrders ?? Enumerable.Empty<Domain.Entities.OrganizationSubscriptionOrder>())
            .Where(s => (s.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Active || s.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Pending))
            .ToList();

        if (activeSubscriptions.Count == 0)
        {
            response.AllAvailable = false;
            response.Message = "No active subscriptions found for this organization.";
            return response;
        }

        // Check each license type request
        foreach (var licenseRequest in request.LicenseRequests)
        {
            // Parse license type
            if (!Enum.TryParse<Domain.Enums.LicenseType>(licenseRequest.LicenseType, true, out var licenseType))
            {
                response.AllAvailable = false;
                response.Results.Add(new LicenseCheckResult
                {
                    LicenseType = licenseRequest.LicenseType,
                    Available = false,
                    AvailableCount = 0,
                    RequestedCount = licenseRequest.Count
                });
                continue;
            }

            // Calculate total licenses and used licenses across all active subscriptions
            int totalLicenses = 0;
            int usedLicenses = 0;

            foreach (var subscription in activeSubscriptions)
            {
                // Get max seats based on license type
                int maxSeats = licenseType switch
                {
                    Domain.Enums.LicenseType.Student => subscription.MaxStudentSeats,
                    Domain.Enums.LicenseType.Teacher => subscription.MaxTeacherSeats,
                    Domain.Enums.LicenseType.OrganizationAdmin => 10, // Default value until MaxOrganizationAdminSeats is added to entity
                    _ => 0
                };

                totalLicenses += maxSeats;

                // Count used licenses for this subscription and type
                var usedCount = await _unitOfWork.LicenseAssignments.CountAsync(
                    new ActiveOrPendingBySubscriptionAndTypeSpecification(subscription.Id, licenseType),
                    cancellationToken);

                usedLicenses += usedCount;
            }

            var availableCount = totalLicenses - usedLicenses;
            var available = availableCount >= licenseRequest.Count;

            if (!available)
            {
                response.AllAvailable = false;
            }

            response.Results.Add(new LicenseCheckResult
            {
                LicenseType = licenseRequest.LicenseType,
                Available = available,
                AvailableCount = availableCount,
                RequestedCount = licenseRequest.Count
            });

            _logger.LogInformation(
                "License check - Type: {LicenseType}, Total: {Total}, Used: {Used}, Available: {Available}, Requested: {Requested}, Result: {Result}",
                licenseRequest.LicenseType, totalLicenses, usedLicenses, availableCount, licenseRequest.Count, available);
        }

        response.Message = response.AllAvailable
            ? "All requested licenses are available."
            : "Some requested licenses are not available.";

        return response;
    }
}
