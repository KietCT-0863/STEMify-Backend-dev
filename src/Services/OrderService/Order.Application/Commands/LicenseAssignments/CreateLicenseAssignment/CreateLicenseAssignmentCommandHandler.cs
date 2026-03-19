using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Order.Domain.Entities;
using Shared.Protos.Order;
using EventBus.Messages.License;
using MassTransit;

namespace Order.Application.Commands.LicenseAssignments.CreateLicenseAssignment
{
    public class CreateLicenseAssignmentCommandHandler : IRequestHandler<CreateLicenseAssignmentCommand, GrpcLicenseAssignmentListModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _grpcUserClient;
        private readonly ILogger<CreateLicenseAssignmentCommandHandler> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateLicenseAssignmentCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IGrpcUserClient grpcUserClient,
            ILogger<CreateLicenseAssignmentCommandHandler> logger,
            IPublishEndpoint publishEndpoint)
        {
            _unitOfWork = unitOfWork;
            _grpcUserClient = grpcUserClient;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<GrpcLicenseAssignmentListModel> Handle(
            CreateLicenseAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            if (request?.LicenseAssignments == null || request.LicenseAssignments.Count == 0)
                throw new ArgumentException("No license assignments provided.", nameof(request));

            _logger.LogInformation("Processing {Count} license assignment(s)", request.LicenseAssignments.Count);

            // STEP 1: Validate subscription orders exist
            var subscriptionCache = new Dictionary<int, OrganizationSubscriptionOrder>();
            var subscriptionIds = request.LicenseAssignments
                .Select(x => x.OrganizationSubscriptionOrderId)
                .Distinct()
                .ToList();

            foreach (var subId in subscriptionIds)
            {
                var sub = await _unitOfWork.OrganizationSubscriptionOrders.FindByIdAsync(subId, cancellationToken);
                if (sub == null)
                    throw new KeyNotFoundException($"Subscription order with ID {subId} not found.");
                subscriptionCache[subId] = sub;
                _logger.LogInformation(
                    "Subscription {SubId} - MaxStudentSeats: {MaxStudent}, MaxTeacherSeats: {MaxTeacher}",
                    subId, sub.MaxStudentSeats, sub.MaxTeacherSeats);
            }

            // STEP 2: Batch check users from identity service
            var ids = request.LicenseAssignments
                .Select(x => x.UserId ?? string.Empty)
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation("Checking {Count} unique user id(s) in identity service", ids.Count);

            // STEP 1 (continued): Validate seat availability & check existing assignments
            var validationErrors = new List<string>();
            var seatUsage = new Dictionary<(int subId, Domain.Enums.LicenseType type), int>(); // Track within batch

            foreach (var model in request.LicenseAssignments)
            {
                var subscription = subscriptionCache[model.OrganizationSubscriptionOrderId];
                var userId = model.UserId;

                // Check if this exact combination already exists in DB
                var existsSpec = new LicenseAssignmentBySubscriptionUserAndTypeSpecification(
                    model.OrganizationSubscriptionOrderId, userId, model.Type);
                var alreadyAssigned = await _unitOfWork.LicenseAssignments.AnyAsync(existsSpec, cancellationToken);

                if (alreadyAssigned)
                {
                    validationErrors.Add(
                        $"User {userId} already has a {model.Type} license for subscription {model.OrganizationSubscriptionOrderId}");
                    continue;
                }

                // Check seat limits per type
                if (model.Type == Domain.Enums.LicenseType.Student)
                {
                    var key = (model.OrganizationSubscriptionOrderId, Domain.Enums.LicenseType.Student);

                    var currentCount = await _unitOfWork.LicenseAssignments.CountAsync(
                        new ActiveOrPendingBySubscriptionAndTypeSpecification(
                            model.OrganizationSubscriptionOrderId, Domain.Enums.LicenseType.Student),
                        cancellationToken);

                    // Track usage within this batch
                    seatUsage.TryGetValue(key, out var batchUsed);
                    var projectedTotal = currentCount + batchUsed + 1;

                    _logger.LogInformation(
                        "Student seat check - SubId: {SubId}, Current: {Current}, BatchUsed: {BatchUsed}, Projected: {Projected}, Max: {Max}",
                        model.OrganizationSubscriptionOrderId, currentCount, batchUsed, projectedTotal, subscription.MaxStudentSeats);

                    if (subscription.MaxStudentSeats > 0 && projectedTotal > subscription.MaxStudentSeats)
                    {
                        validationErrors.Add(
                            $"No available student seats for subscription {model.OrganizationSubscriptionOrderId} " +
                            $"(Current: {currentCount}, Batch: {batchUsed}, Projected: {projectedTotal}/{subscription.MaxStudentSeats})");
                    }
                    else
                    {
                        seatUsage[key] = batchUsed + 1;
                    }
                }

                if (model.Type == Domain.Enums.LicenseType.Teacher)
                {
                    var key = (model.OrganizationSubscriptionOrderId, Domain.Enums.LicenseType.Teacher);

                    var currentCount = await _unitOfWork.LicenseAssignments.CountAsync(
                        new ActiveOrPendingBySubscriptionAndTypeSpecification(
                            model.OrganizationSubscriptionOrderId, Domain.Enums.LicenseType.Teacher),
                        cancellationToken);

                    seatUsage.TryGetValue(key, out var batchUsed);
                    var projectedTotal = currentCount + batchUsed + 1;

                    _logger.LogInformation(
                        "Teacher seat check - SubId: {SubId}, Current: {Current}, BatchUsed: {BatchUsed}, Projected: {Projected}, Max: {Max}",
                        model.OrganizationSubscriptionOrderId, currentCount, batchUsed, projectedTotal, subscription.MaxTeacherSeats);

                    if (subscription.MaxTeacherSeats > 0 && projectedTotal > subscription.MaxTeacherSeats)
                    {
                        validationErrors.Add(
                            $"No available teacher seats for subscription {model.OrganizationSubscriptionOrderId} " +
                            $"(Current: {currentCount}, Batch: {batchUsed}, Projected: {projectedTotal}/{subscription.MaxTeacherSeats})");
                    }
                    else
                    {
                        seatUsage[key] = batchUsed + 1;
                    }
                }

                // ORGANIZATIONADMIN typically has no seat limit
                if (model.Type == Domain.Enums.LicenseType.OrganizationAdmin)
                {
                    _logger.LogInformation(
                        "OrganizationAdmin license - no seat limit check needed for SubId: {SubId}",
                        model.OrganizationSubscriptionOrderId);
                }
            }

            if (validationErrors.Count > 0)
            {
                var errorMessage = string.Join("; ", validationErrors);
                _logger.LogError("License assignment validation failed: {Errors}", errorMessage);
                throw new InvalidOperationException($"License assignment validation failed: {errorMessage}");
            }

            // STEP 3: Create licenses - process all valid assignments
            var createdAssignments = new List<LicenseAssignment>();

            foreach (var model in request.LicenseAssignments)
            {
                var userId = model.UserId;

                // Double-check it doesn't already exist (in case of race condition)
                var existsSpec = new LicenseAssignmentBySubscriptionUserAndTypeSpecification(
                    model.OrganizationSubscriptionOrderId, userId, model.Type);
                var alreadyExists = await _unitOfWork.LicenseAssignments.AnyAsync(existsSpec, cancellationToken);

                if (alreadyExists)
                {
                    _logger.LogWarning(
                        "Skipping - License already exists for UserId: {UserId}, SubId: {SubId}, Type: {Type}",
                        userId, model.OrganizationSubscriptionOrderId, model.Type);
                    continue;
                }
                var subscriptionOrder = subscriptionCache[model.OrganizationSubscriptionOrderId];

                // Validate organization users exist
                var orgUser = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(userId), cancellationToken);
                if (orgUser == null)
                {
                    _logger.LogError(
                        "Organization user not found - UserId: {UserId}, OrganizationId: {OrgId}",
                        userId, subscriptionOrder.OrganizationId);
                    throw new KeyNotFoundException(
                        $"Người dùng này chưa tồn tại trong tổ chức!");
                }

                var licenseAssignment = new LicenseAssignment
                {
                    OrganizationSubscriptionOrderId = model.OrganizationSubscriptionOrderId,
                    OrganizationUserId = orgUser.OrganizationUserId,
                    AssignedAt = DateTime.UtcNow,
                    RevokedAt = null,
                    LicenseType = model.Type,
                    Status = Domain.Enums.LicenseAssignmentStatus.Active
                };

                await _unitOfWork.LicenseAssignments.AddAsync(licenseAssignment, cancellationToken);
                createdAssignments.Add(licenseAssignment);

                _logger.LogInformation(
                    "License created - UserId: {UserId}, SubscriptionId: {SubId}, Type: {Type}",
                    userId, model.OrganizationSubscriptionOrderId, model.Type);
            }

            // Save all changes at once
            await _unitOfWork.SaveChangesAsync(cancellationToken);

           foreach (var la in createdAssignments)
            {
                var createdEvent = new LicenseAssignmentCreatedEvent(
                    licenseAssignmentId: la.Id,
                    userId: la.OrganizationUserId,
                    organizationSubscriptionOrderId: la.OrganizationSubscriptionOrderId,
                    licenseType: la.LicenseType.ToString(),
                    status: la.Status.ToString(),
                    assignedAt: la.AssignedAt);

                await _publishEndpoint.Publish(createdEvent, cancellationToken);
            }

            // Build response
            var response = new GrpcLicenseAssignmentListModel();
            foreach (var la in createdAssignments)
            {
                response.LicenseAssignments.Add(new GrpcLicenseAssignmentModel
                {
                    Id = la.Id,
                    OrganizationSubscriptionOrderId = la.OrganizationSubscriptionOrderId,
                    UserId = la.OrganizationUserId,
                    AssignedAt = Timestamp.FromDateTime(la.AssignedAt.ToUniversalTime()),
                    RevokedAt = la.RevokedAt.HasValue ? Timestamp.FromDateTime(la.RevokedAt.Value.ToUniversalTime()) : null,
                    Status = la.Status.ToString(),
                    Type = la.LicenseType.ToString()
                });
            }

            _logger.LogInformation("Successfully created {Count} license assignment(s)", createdAssignments.Count);
            return response;
        }
    }
}