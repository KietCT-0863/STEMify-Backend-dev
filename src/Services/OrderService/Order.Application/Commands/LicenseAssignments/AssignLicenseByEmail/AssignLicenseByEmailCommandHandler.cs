using EventBus.Messages.License;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Order.Domain.Entities;
using Order.Domain.Enums;
using Shared.Protos.Order;

namespace Order.Application.Commands.LicenseAssignments.AssignLicenseByEmail;

public class AssignLicenseByEmailCommandHandler
    : IRequestHandler<AssignLicenseByEmailCommand, AssignLicenseByEmailResponse>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IGrpcUserClient _grpcUserClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AssignLicenseByEmailCommandHandler> _logger;

    public AssignLicenseByEmailCommandHandler(
        IOrderUnitOfWork unitOfWork,
        IGrpcUserClient grpcUserClient,
        IPublishEndpoint publishEndpoint,
        ILogger<AssignLicenseByEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _grpcUserClient = grpcUserClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<AssignLicenseByEmailResponse> Handle(
        AssignLicenseByEmailCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning {LicenseType} license to {UserEmail} for subscription {SubscriptionOrderId} in organization {OrganizationId}",
            request.LicenseType, request.UserEmail, request.SubscriptionOrderId, request.OrganizationId);

        try
        {
            // Step 1: Validate organization exists
            var orgSpec = new OrganizationByIdSpecification(request.OrganizationId);
            var organization = await _unitOfWork.Organizations.FirstOrDefaultAsync(orgSpec, cancellationToken);

            if (organization == null)
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Organization with ID {request.OrganizationId} not found."
                };
            }

            // Step 2: Validate subscription order exists and belongs to the organization
            var subscription = await _unitOfWork.OrganizationSubscriptionOrders.FindByIdAsync(
                request.SubscriptionOrderId, cancellationToken);

            if (subscription == null)
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Subscription order with ID {request.SubscriptionOrderId} not found."
                };
            }

            if (subscription.OrganizationId != request.OrganizationId)
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Subscription order {request.SubscriptionOrderId} does not belong to organization {request.OrganizationId}."
                };
            }

            // Check subscription is active
            if (subscription.Status != Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Subscription order {request.SubscriptionOrderId} is not active (status: {subscription.Status})."
                };
            }

            // Step 3: Parse and validate license type
            if (!Enum.TryParse<Domain.Enums.LicenseType>(request.LicenseType, true, out var licenseType))
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"Invalid license type: {request.LicenseType}. Valid types are: Student, Teacher, OrganizationAdmin."
                };
            }

            // Step 4: Check if user exists in identity service
            var checkResp = await _grpcUserClient.CheckUserExists(new List<string> { request.UserEmail });

            if (checkResp?.Results == null || !checkResp.Results.Any())
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"User with email {request.UserEmail} not found in identity service."
                };
            }

            var userResult = checkResp.Results.FirstOrDefault(r =>
                r.Email.Equals(request.UserEmail, StringComparison.OrdinalIgnoreCase));

            if (userResult == null || string.IsNullOrWhiteSpace(userResult.UserId))
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"User with email {request.UserEmail} not found in identity service."
                };
            }

            var userId = userResult.UserId;

            // Step 5: Check if license already assigned
            var existsSpec = new LicenseAssignmentBySubscriptionUserAndTypeSpecification(
                request.SubscriptionOrderId, userId, licenseType);
            var alreadyAssigned = await _unitOfWork.LicenseAssignments.AnyAsync(existsSpec, cancellationToken);

            if (alreadyAssigned)
            {
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"User {request.UserEmail} already has a {request.LicenseType} license for subscription {request.SubscriptionOrderId}."
                };
            }

            // Step 6: Check license availability
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
                return new AssignLicenseByEmailResponse
                {
                    Success = false,
                    LicenseAssignmentId = 0,
                    ErrorMessage = $"No available {request.LicenseType} seats for subscription {request.SubscriptionOrderId} (used: {currentCount}/{maxSeats})."
                };
            }

            // Step 7: Create license assignment
            var licenseAssignment = new LicenseAssignment
            {
                OrganizationSubscriptionOrderId = request.SubscriptionOrderId,
                OrganizationUserId = userId,
                AssignedAt = DateTime.UtcNow,
                RevokedAt = null,
                LicenseType = licenseType,
                Status = Domain.Enums.LicenseAssignmentStatus.Active
            };

            await _unitOfWork.LicenseAssignments.AddAsync(licenseAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdEvent = new LicenseAssignmentCreatedEvent(
                licenseAssignmentId: licenseAssignment.Id,
                userId: userId,
                organizationSubscriptionOrderId: request.SubscriptionOrderId,
                licenseType: request.LicenseType,
                status: "Active",
                assignedAt: licenseAssignment.AssignedAt);

            await _publishEndpoint.Publish(createdEvent, cancellationToken);

            var activatedEvent = new LicenseAssignmentActivatedEvent(
                licenseAssignmentId: licenseAssignment.Id,
                userId: userId,
                organizationSubscriptionOrderId: request.SubscriptionOrderId,
                licenseType: request.LicenseType,
                activatedAt: licenseAssignment.AssignedAt);

            await _publishEndpoint.Publish(activatedEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully assigned license {LicenseAssignmentId} - UserId: {UserId}, Email: {Email}, Type: {Type}, SubscriptionId: {SubId}",
                licenseAssignment.Id, userId, request.UserEmail, request.LicenseType, request.SubscriptionOrderId);

            return new AssignLicenseByEmailResponse
            {
                Success = true,
                LicenseAssignmentId = licenseAssignment.Id,
                ErrorMessage = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning license to {UserEmail}", request.UserEmail);
            return new AssignLicenseByEmailResponse
            {
                Success = false,
                LicenseAssignmentId = 0,
                ErrorMessage = $"An error occurred while assigning the license: {ex.Message}"
            };
        }
    }
}
