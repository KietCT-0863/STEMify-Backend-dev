using DnsClient.Internal;
using Emulator.API.Protos;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Shared.Protos.Order;
using Shared.Protos.Resource;


namespace Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumList
{
    public class GetOrganizationCurriculumListQueryHandler : IRequestHandler<GetOrganizationCurriculumListQuery, GrpcOrganizationCurriculumList>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrganizationCurriculumListQueryHandler> _logger;
        public GetOrganizationCurriculumListQueryHandler(
            IOrderUnitOfWork unitOfWork,
            ILogger<GetOrganizationCurriculumListQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<GrpcOrganizationCurriculumList> Handle(GetOrganizationCurriculumListQuery request, CancellationToken cancellationToken)
        {
            var spec = new OrganizationCurriculumByOrganizationIdSpecification(request.OrgId, request.Status);
            var organizationSubscriptions = await _unitOfWork.OrganizationSubscriptionOrders.GetAllAsync(spec, cancellationToken);
            if (organizationSubscriptions.Any()) {
                _logger.LogInformation("Found {Count} organization subscriptions for OrgId {OrgId} with Status {Status}",
                    organizationSubscriptions.Count(), request.OrgId, request.Status);
            } else {
                _logger.LogWarning("No organization subscriptions found for OrgId {OrgId} with Status {Status}",
                    request.OrgId, request.Status);
            }

            var flatItems = organizationSubscriptions
                .SelectMany(orgSub => orgSub.SubscriptionOrderCurriculums.Select(sc => new
                {
                    CurriculumId = sc.CurriculumId,
                    Curriculum = sc,
                    Subscription = orgSub
                }));
            var groupedByCurriculum = flatItems
                .GroupBy(x => x.CurriculumId);

            var response = new GrpcOrganizationCurriculumList();

            foreach (var curriculumGroup in groupedByCurriculum)
            {
                var first = curriculumGroup.First().Curriculum;

                var grpcCurriculum = new OrganizationCurriculumModel
                {
                    Id = first.CurriculumId,
                    Title = first.CurriculumTitle,
                    Code = first.CurriculumCode,
                    Description = first.CurriculumDescription,
                    ImageUrl = first.CurriculumImageUrl,
                    CourseCount = first.CoursesSnapshot.Count
                };

                // Courses from Snapshot
                var courses = first.CoursesSnapshot
                    .Select(c => new CourseDetails
                    {
                        Id = c.Id,
                        Title = c.Title,
                        ImageUrl = c.ImageUrl,
                        Description = c.Description,
                        Level = c.Level,
                        Code = c.Code
                    })
                    .DistinctBy(c => c.Id)
                    .ToList();
                grpcCurriculum.Courses.AddRange(courses);

                // Emulators from Snapshot
                var emulators = first.EmulatorsSnapshot
                    .Select(e => new EmulationListItem
                    {
                        EmulationId = e.EmulationId,
                        Name = e.Name,
                        Description = e.Description,
                        ThumbnailUrl = e.ThumbnailUrl
                    })
                    .DistinctBy(e => e.EmulationId)
                    .ToList();
                grpcCurriculum.Emulations.AddRange(emulators);

                // KitIds
                grpcCurriculum.KitIds.AddRange(
                    first.CoursesSnapshot
                        .Where(c => c.KitId.HasValue)
                        .Select(c => c.KitId.Value)
                        .Distinct()
                );

                // GROUP SUBSCRIPTIONS BY STATUS
                var subscriptionGroups = curriculumGroup.GroupBy(x => x.Subscription.Status);
                foreach (var statusGroup in subscriptionGroups)
                {
                    var grpcStatusGroup = new CurriculumSubscriptionGroup
                    {
                        Status = statusGroup.Key.ToString()
                    };

                    grpcStatusGroup.Subscriptions.AddRange(
                        statusGroup.Select(x => new CurriculumSubscriptionInfo
                        {
                            SubscriptionId = x.Subscription.Id,
                            StartDate = x.Subscription.StartDate.ToString("o"),
                            EndDate = x.Subscription.EndDate.ToString("o"),
                            PlanName = x.Subscription.PlanName ?? "Stemify Pro",
                        })
                    );

                    grpcCurriculum.SubscriptionGroups.Add(grpcStatusGroup);
                }

                response.Curriculums.Add(grpcCurriculum);
            }
            return response;
        }
    }
}
