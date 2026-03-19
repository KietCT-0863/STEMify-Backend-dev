
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationsWithAccessByUserId
{
    public class GetOrganizationsWithAccessByUserIdQueryHandler : IRequestHandler<GetOrganizationsWithAccessByUserIdQuery, GrpcOrganizationsWithAccessResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrganizationsWithAccessByUserIdQueryHandler> _logger;
        private readonly IGrpcUserClient _grpcUserClient;
        public GetOrganizationsWithAccessByUserIdQueryHandler(
            IOrderUnitOfWork unitOfWork,
            ILogger<GetOrganizationsWithAccessByUserIdQueryHandler> logger,
            IGrpcUserClient grpcUserClient)
        {
            _unitOfWork = unitOfWork;   
            _logger = logger;
            _grpcUserClient = grpcUserClient;
        }
        public async Task<GrpcOrganizationsWithAccessResponse> Handle(GetOrganizationsWithAccessByUserIdQuery request, CancellationToken cancellationToken)
        {
            List<string> organizationUserIds = [];
            if (Guid.TryParse(request.UserId, out var userId))
            {
                // get organization users with active status
                var orgUsers = await _grpcUserClient.GetOrganizationUsersByUserIdAsync(
                    userId,
                    cancellationToken);

                organizationUserIds = orgUsers.Items
                    .Select(u => u.OrganizationUserId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();
            }
            _logger.LogInformation("Found {Count} organization user IDs for user {UserId}", organizationUserIds.Count, request.UserId);
            if (organizationUserIds.Count == 0)
            {
                return new GrpcOrganizationsWithAccessResponse
                {
                    Organizations = { }
                };
            }

            // get license active
            var spec = new LicenseAssignmentsByUserSpecification(organizationUserIds, null, Domain.Enums.LicenseAssignmentStatus.Active);
            // filter license belong to active subscription
            var activeLicenses = (await _unitOfWork.LicenseAssignments.GetAllAsync(spec, cancellationToken))
                                .Where(l => l.OrganizationSubscriptionOrder?.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
                                .ToList();

            var response = new GrpcOrganizationsWithAccessResponse();
            var grouped = activeLicenses
            .GroupBy(l => l.OrganizationSubscriptionOrder.Organization)
            .ToList();

            foreach (var group in grouped)
            {
                var org = group.Key;

                var orgModel = new OrganizationWithAccessModel
                {
                    Id = org.Id,
                    Name = org.Name,
                    Code = org.Code,
                    ImageUrl = org.ImageUrl
                };

                foreach (var license in group)
                {
                    var sub = license.OrganizationSubscriptionOrder;

                    var subscriptionModel = new OrgSubscription
                    {
                        Id = sub.Id,
                        Status = license.Status.ToString(),         
                        LicenseType = license.LicenseType.ToString() 
                    };

                    // CurriculumIds
                    var curriculumIds = sub.SubscriptionOrderCurriculums
                        .Select(sc => sc.CurriculumId)
                        .Distinct()
                        .ToList();

                    subscriptionModel.CurriculumIds.AddRange(curriculumIds);

                    // CourseIds
                    var courseIds = sub.SubscriptionOrderCurriculums
                        .SelectMany(sc => sc.CoursesSnapshot.Select(c => c.Id))
                        .Distinct()
                        .ToList();

                    subscriptionModel.CourseIds.AddRange(courseIds);

                    // EmulatorIds
                    var emulatorIds = sub.SubscriptionOrderCurriculums
                        .SelectMany(sc => sc.EmulatorsSnapshot.Select(e => e.EmulationId))
                        .Distinct()
                        .ToList();

                    subscriptionModel.EmulatorModelIds.AddRange(emulatorIds);

                    orgModel.Subscriptions.Add(subscriptionModel);
                }

                response.Organizations.Add(orgModel);
            }
            return response;
        }
    }
}
