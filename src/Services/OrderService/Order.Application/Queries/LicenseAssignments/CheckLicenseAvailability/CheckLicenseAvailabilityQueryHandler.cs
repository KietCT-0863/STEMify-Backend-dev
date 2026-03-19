using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Order.Domain.Enums;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.CheckLicenseAvailability;

public class CheckLicenseAvailabilityQueryHandler
    : IRequestHandler<CheckLicenseAvailabilityQuery, CheckLicenseAvailabilityResponse>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly ILogger<CheckLicenseAvailabilityQueryHandler> _logger;

    public CheckLicenseAvailabilityQueryHandler(
        IOrderUnitOfWork unitOfWork,
        ILogger<CheckLicenseAvailabilityQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CheckLicenseAvailabilityResponse> Handle(
        CheckLicenseAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking license availability for organization {OrganizationId}, type {LicenseType}, requested count {RequestedCount}",
            request.OrganizationId, request.LicenseType, request.RequestedCount);

        // Get organization's active subscriptions
        var orgSpec = new OrganizationByIdSpecification(request.OrganizationId);
        var organization = await _unitOfWork.Organizations.FirstOrDefaultAsync(orgSpec, cancellationToken);

        if (organization == null)
        {
            return new CheckLicenseAvailabilityResponse
            {
                Available = false,
                AvailableCount = 0,
                TotalLicenses = 0,
                UsedLicenses = 0,
                Message = $"Organization with ID {request.OrganizationId} not found."
            };
        }

        // Parse license type
        if (!Enum.TryParse<Domain.Enums.LicenseType>(request.LicenseType, true, out var licenseType))
        {
            return new CheckLicenseAvailabilityResponse
            {
                Available = false,
                AvailableCount = 0,
                TotalLicenses = 0,
                UsedLicenses = 0,
                Message = $"Invalid license type: {request.LicenseType}"
            };
        }

        // Get all active subscriptions for this organization
        var activeSubscriptions = (organization.SubscriptionOrders ?? Enumerable.Empty<Domain.Entities.OrganizationSubscriptionOrder>())
            .Where(s => s.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
            .ToList();

        if (activeSubscriptions.Count == 0)
        {
            return new CheckLicenseAvailabilityResponse
            {
                Available = false,
                AvailableCount = 0,
                TotalLicenses = 0,
                UsedLicenses = 0,
                Message = "No active subscriptions found for this organization."
            };
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
        var available = availableCount >= request.RequestedCount;

        var message = available
            ? $"{availableCount} {request.LicenseType} license(s) available."
            : $"Only {availableCount} {request.LicenseType} license(s) available, but {request.RequestedCount} requested.";

        _logger.LogInformation(
            "License availability check result - Organization: {OrganizationId}, Type: {LicenseType}, Total: {Total}, Used: {Used}, Available: {Available}, Requested: {Requested}, Result: {Result}",
            request.OrganizationId, request.LicenseType, totalLicenses, usedLicenses, availableCount, request.RequestedCount, available);

        return new CheckLicenseAvailabilityResponse
        {
            Available = available,
            AvailableCount = availableCount,
            TotalLicenses = totalLicenses,
            UsedLicenses = usedLicenses,
            Message = message
        };
    }
}
