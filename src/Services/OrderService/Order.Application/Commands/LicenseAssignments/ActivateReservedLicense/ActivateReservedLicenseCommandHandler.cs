using Elastic.CommonSchema;
using EventBus.Messages.License;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Shared.Protos.Order;

namespace Order.Application.Commands.LicenseAssignments.ActivateReservedLicense;

/// <summary>
/// Activates a reserved license assignment (Pending -> Active)
/// Called when user accepts invitation
/// </summary>
public class ActivateReservedLicenseCommandHandler
    : IRequestHandler<ActivateReservedLicenseCommand, ActivateReservedLicenseResponse>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IGrpcUserClient _grpcUserClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ActivateReservedLicenseCommandHandler> _logger;

    public ActivateReservedLicenseCommandHandler(
        IOrderUnitOfWork unitOfWork,
        IGrpcUserClient grpcUserClient,
        IPublishEndpoint publishEndpoint,
        ILogger<ActivateReservedLicenseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _grpcUserClient = grpcUserClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ActivateReservedLicenseResponse> Handle(
        ActivateReservedLicenseCommand request,
        CancellationToken cancellationToken)
    {
        
        try
        {
            // Step 1: Get OrganizationUser by ID from Identity Service
            if (!Guid.TryParse(request.OrganizationUserId, out var organizationUserId))
            {
                return new ActivateReservedLicenseResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Invalid OrganizationUserId format: {request.OrganizationUserId}"
                };
            }

            var orgUserResponse = await _grpcUserClient.GetOrganizationUserByIdAsync(organizationUserId, cancellationToken);

            if (orgUserResponse == null || string.IsNullOrWhiteSpace(orgUserResponse.UserId))
            {
                return new ActivateReservedLicenseResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"OrganizationUser with id {request.OrganizationUserId} not found in identity service."
                };
            }

            var userId = Guid.Parse(orgUserResponse.UserId);
            var organizationUserIdString = organizationUserId.ToString();

            // Step 2: Parse and validate license type
            if (!Enum.TryParse<Domain.Enums.LicenseType>(request.LicenseType, true, out var licenseType))
            {
                return new ActivateReservedLicenseResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Invalid license type: {request.LicenseType}."
                };
            }

            // Step 3: Find existing Pending license assignment 
            var existsSpec = new LicenseAssignmentBySubscriptionUserAndTypeSpecification(
                request.SubscriptionOrderId, organizationUserIdString, licenseType);
            var existingAssignment = await _unitOfWork.LicenseAssignments.FirstOrDefaultAsync(existsSpec, cancellationToken);

            if (existingAssignment == null)
            {
                _logger.LogWarning(
                    "No Pending license found for OrganizationUserId {OrganizationUserId}, creating new Active assignment",
                    organizationUserId);

                var licenseAssignment = new Domain.Entities.LicenseAssignment
                {
                    OrganizationSubscriptionOrderId = request.SubscriptionOrderId,
                    OrganizationUserId = organizationUserIdString,
                    AssignedAt = DateTime.UtcNow,
                    RevokedAt = null,
                    LicenseType = licenseType,
                    Status = Order.Domain.Enums.LicenseAssignmentStatus.Active
                };

                await _unitOfWork.LicenseAssignments.AddAsync(licenseAssignment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var createdEvent = new LicenseAssignmentCreatedEvent(
                    licenseAssignmentId: licenseAssignment.Id,
                    userId: userId.ToString(),
                    organizationSubscriptionOrderId: request.SubscriptionOrderId,
                    licenseType: request.LicenseType,
                    status: "Active",
                    assignedAt: licenseAssignment.AssignedAt);

                await _publishEndpoint.Publish(createdEvent, cancellationToken);

                // Also publish activated event since it's created as Active
                var activatedEventForNew = new LicenseAssignmentActivatedEvent(
                    licenseAssignmentId: licenseAssignment.Id,
                    userId: userId.ToString(),
                    organizationSubscriptionOrderId: request.SubscriptionOrderId,
                    licenseType: request.LicenseType,
                    activatedAt: licenseAssignment.AssignedAt);

                await _publishEndpoint.Publish(activatedEventForNew, cancellationToken);

                return new ActivateReservedLicenseResponse
                {
                    Success = true,
                    LicenseAssignmentId = licenseAssignment.Id,
                    ErrorMessage = string.Empty
                };
            }

            // Step 4: Update status from Pending to Active
            if (existingAssignment.Status == Order.Domain.Enums.LicenseAssignmentStatus.Active)
            {
                // Already active - idempotent
                _logger.LogInformation(
                    "License already active - AssignmentId: {AssignmentId}, UserId: {UserId}",
                    existingAssignment.Id, userId);

                return new ActivateReservedLicenseResponse
                {
                    Success = true,
                    LicenseAssignmentId = existingAssignment.Id,
                    ErrorMessage = string.Empty
                };
            }

            if (existingAssignment.Status != Order.Domain.Enums.LicenseAssignmentStatus.Pending)
            {
                return new ActivateReservedLicenseResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"License assignment {existingAssignment.Id} is not in Pending status (current: {existingAssignment.Status})."
                };
            }

            // Activate the reserved license
            existingAssignment.Status = Order.Domain.Enums.LicenseAssignmentStatus.Active;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish domain event for Identity Service sync
            var activatedEvent = new LicenseAssignmentActivatedEvent(
                licenseAssignmentId: existingAssignment.Id,
                userId: userId.ToString(),
                organizationSubscriptionOrderId: existingAssignment.OrganizationSubscriptionOrderId,
                licenseType: existingAssignment.LicenseType.ToString(),
                activatedAt: DateTime.UtcNow);

            await _publishEndpoint.Publish(activatedEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully activated reserved license {LicenseAssignmentId} - OrganizationUserId: {OrganizationUserId}, UserId: {UserId}, Type: {Type}",
                existingAssignment.Id, organizationUserId, userId, request.LicenseType);

            return new ActivateReservedLicenseResponse
            {
                Success = true,
                LicenseAssignmentId = existingAssignment.Id,
                ErrorMessage = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating reserved license for OrganizationUserId {OrganizationUserId}", request.OrganizationUserId);
            return new ActivateReservedLicenseResponse
            {
                Success = false,
                LicenseAssignmentId = 0,
                ErrorMessage = $"An error occurred while activating the license: {ex.Message}"
            };
        }
    }
}

