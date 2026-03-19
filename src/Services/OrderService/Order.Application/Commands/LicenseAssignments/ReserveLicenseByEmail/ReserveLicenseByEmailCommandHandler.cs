using EventBus.Messages.License;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Shared.Protos.Order;

namespace Order.Application.Commands.LicenseAssignments.ReserveLicenseByEmail;

/// <summary>
/// Reserves a license assignment with Pending status (for bulk invitation)
/// </summary>
public class ReserveLicenseByEmailCommandHandler
    : IRequestHandler<ReserveLicenseByEmailCommand, ReserveLicenseByEmailResponse>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReserveLicenseByEmailCommandHandler> _logger;

    public ReserveLicenseByEmailCommandHandler(
        IOrderUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ReserveLicenseByEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ReserveLicenseByEmailResponse> Handle(
        ReserveLicenseByEmailCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reserving {LicenseType} license for UserId {UserId} in organization {OrganizationId}, subscription {SubscriptionOrderId}",
            request.LicenseType, request.OrganizationUserId, request.OrganizationId, request.SubscriptionOrderId);

        try
        {
            // Step 1: Validate subscription order exists and belongs to the organization
            var subscription = await _unitOfWork.OrganizationSubscriptionOrders.FindByIdAsync(
                request.SubscriptionOrderId, cancellationToken);

            if (subscription == null)
            {
                return new ReserveLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Subscription order with ID {request.SubscriptionOrderId} not found."
                };
            }

            if (subscription.OrganizationId != request.OrganizationId)
            {
                return new ReserveLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Subscription order {request.SubscriptionOrderId} does not belong to organization {request.OrganizationId}."
                };
            }

            //// Check subscription is active
            //if (subscription.Status != Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
            //{
            //    return new ReserveLicenseByEmailResponse
            //    {
            //        Success = false,
            //        LicenseAssignmentId = 0,
            //        ErrorMessage = $"Subscription order {request.SubscriptionOrderId} is not active (status: {subscription.Status})."
            //    };
            //}

            // Step 2: Parse and validate license type
            if (!Enum.TryParse<Domain.Enums.LicenseType>(request.LicenseType, true, out var licenseType))
            {
                return new ReserveLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Invalid license type: {request.LicenseType}. Valid types are: Student, Teacher, OrganizationAdmin."
                };
            }

            // Step 3: Check if license already assigned (idempotent check)
            var existsSpec = new LicenseAssignmentBySubscriptionUserAndTypeSpecification(
                request.SubscriptionOrderId, request.OrganizationUserId, licenseType);
            var existingAssignment = await _unitOfWork.LicenseAssignments.FirstOrDefaultAsync(existsSpec, cancellationToken);

            if (existingAssignment != null)
            {
                // If already exists with Pending or Active status, return existing ID
                if (existingAssignment.Status == Order.Domain.Enums.LicenseAssignmentStatus.Pending ||
                    existingAssignment.Status == Order.Domain.Enums.LicenseAssignmentStatus.Active)
                {
                    _logger.LogInformation(
                        "License already reserved/assigned - UserId: {UserId}, SubId: {SubId}, Type: {Type}, Status: {Status}, AssignmentId: {AssignmentId}",
                        request.OrganizationUserId, request.SubscriptionOrderId, request.LicenseType, existingAssignment.Status, existingAssignment.Id);

                    return new ReserveLicenseByEmailResponse
                    {
                        Success = true,
                        LicenseAssignmentId = existingAssignment.Id,
                        ErrorMessage = string.Empty
                    };
                }
            }

            // Step 4: Check license availability (count Active + Pending)
            var currentCount = await _unitOfWork.LicenseAssignments.CountAsync(
                new ActiveOrPendingBySubscriptionAndTypeSpecification(request.SubscriptionOrderId, licenseType),
                cancellationToken);

            int maxSeats = licenseType switch
            {
                Domain.Enums.LicenseType.Student => subscription.MaxStudentSeats,
                Domain.Enums.LicenseType.Teacher => subscription.MaxTeacherSeats,
                Domain.Enums.LicenseType.OrganizationAdmin => 10, // Default value until MaxOrganizationAdminSeats is added to entity
                _ => 0
            };

            if (maxSeats > 0 && currentCount >= maxSeats)
            {
                return new ReserveLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"No available {request.LicenseType} seats for subscription {request.SubscriptionOrderId} (used: {currentCount}/{maxSeats})."
                };
            }

            // Step 5: Create license assignment with Pending status
            var licenseAssignment = new LicenseAssignment
            {
                OrganizationSubscriptionOrderId = request.SubscriptionOrderId,
                OrganizationUserId = request.OrganizationUserId,
                AssignedAt = DateTime.UtcNow,
                RevokedAt = null,
                LicenseType = licenseType,
                Status = Order.Domain.Enums.LicenseAssignmentStatus.Pending 
            };

            await _unitOfWork.LicenseAssignments.AddAsync(licenseAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain event for Identity Service sync
            var createdEvent = new LicenseAssignmentCreatedEvent(
                licenseAssignmentId: licenseAssignment.Id,
                userId: request.OrganizationUserId,
                organizationSubscriptionOrderId: request.SubscriptionOrderId,
                licenseType: request.LicenseType,
                status: "Pending",
                assignedAt: licenseAssignment.AssignedAt);

            await _publishEndpoint.Publish(createdEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully reserved license {LicenseAssignmentId} (Pending) - UserId: {UserId}, Type: {Type}, SubscriptionId: {SubId}",
                licenseAssignment.Id, request.OrganizationUserId, request.LicenseType, request.SubscriptionOrderId);

            return new ReserveLicenseByEmailResponse
            {
                Success = true,
                LicenseAssignmentId = licenseAssignment.Id,
                ErrorMessage = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving license for UserId {OrganizationUserId}", request.OrganizationUserId);
            return new ReserveLicenseByEmailResponse
            {
                Success = false,
                LicenseAssignmentId = 0,
                ErrorMessage = $"An error occurred while reserving the license: {ex.Message}"
            };
        }
    }
}

