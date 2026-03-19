using Google.Protobuf.WellKnownTypes;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Specifications;
using Shared.Protos.Order;
using Shared.Protos.Resource;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderById
{
    public class GetOrganizationSubscriptionOrderByIdQueryHandler
        : IRequestHandler<GetOrganizationSubscriptionOrderByIdQuery, GrpcOrganizationSubscriptionOrderDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;
        private readonly ICurriculumCacheService _curriculumCacheService;

        public GetOrganizationSubscriptionOrderByIdQueryHandler(
            IOrderUnitOfWork unitOfWork,
            IPlanBillingCycleCacheService planBillingCycleCacheService,
            ICurriculumCacheService curriculumCacheService)
        {
            _unitOfWork = unitOfWork;
            _planBillingCycleCacheService = planBillingCycleCacheService;
            _curriculumCacheService = curriculumCacheService;
        }

        public async Task<GrpcOrganizationSubscriptionOrderDetail> Handle(
            GetOrganizationSubscriptionOrderByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new OrganizationSubscriptionOrderByIdSpecification(request.Id);

            var organization = await _unitOfWork.OrganizationSubscriptionOrders
                .FirstOrDefaultAsync(spec, cancellationToken);

            if (organization == null)
            {
                throw new KeyNotFoundException($"OrganizationSubscriptionOrder with ID {request.Id} not found.");
            }

            var grpcOrganizationSubscriptionOrder = new GrpcOrganizationSubscriptionOrderDetail
            {
                Id = organization.Id,
                Code = organization.Code,
                OrganizationId = organization.OrganizationId,
                ContractId = organization.ContractId,
                PlanBillingCycleId = organization.PlanBillingCycleId,
                ParentSubscriptionId = organization.ParentSubscriptionId,
                CurriculumCount = organization.CurriculumCount,
                DiscountPercent = (double)organization.DiscountPercent,
                StartDate = Timestamp.FromDateTime(organization.StartDate.ToUniversalTime()),
                EndDate = Timestamp.FromDateTime(organization.EndDate.ToUniversalTime()),
                GrossAmount = (double)organization.GrossAmount,
                NetAmount = (double)organization.NetAmount,
                MaxStudentSeats = organization.MaxStudentSeats,
                MaxTeacherSeats = organization.MaxTeacherSeats,
                PlanName = organization.PlanName,
                Status = organization.Status.ToString(),
                CreatedDate = Timestamp.FromDateTimeOffset(organization.CreatedDate),
                LastModifiedDate = organization.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(organization.LastModifiedDate.Value)
                        : null,
            };
            grpcOrganizationSubscriptionOrder.LicenseAssignmentUserIds.AddRange(
                organization.LicenseAssignments?
                .Where(la => la.Status != Domain.Enums.LicenseAssignmentStatus.Revoked)
                .Select(la => la.OrganizationUserId) ?? Enumerable.Empty<string>());

            int currentStudentSeats = 0;
            int currentTeacherSeats = 0;
            if (organization.LicenseAssignments != null && organization.LicenseAssignments.Any())
            {
                currentStudentSeats = organization.LicenseAssignments.Count(la =>
                    la.OrganizationSubscriptionOrderId == organization.Id &&
                    la.LicenseType == Domain.Enums.LicenseType.Student &&
                    (la.Status != Domain.Enums.LicenseAssignmentStatus.Revoked));

                currentTeacherSeats = organization.LicenseAssignments.Count(la =>
                    la.OrganizationSubscriptionOrderId == organization.Id &&
                    la.LicenseType == Domain.Enums.LicenseType.Teacher &&
                    (la.Status != Domain.Enums.LicenseAssignmentStatus.Revoked));
            }

            grpcOrganizationSubscriptionOrder.CurrentStudentSeats = currentStudentSeats;
            grpcOrganizationSubscriptionOrder.CurrentTeacherSeats = currentTeacherSeats;

            if (organization.Organization != null)
            {
                grpcOrganizationSubscriptionOrder.Organization = new GrpcOrganizationInformation
                {
                    Id = organization.Organization.Id,
                    Name = organization.Organization.Name,
                    Code = organization.Organization.Code,
                    ImageUrl = organization.Organization.ImageUrl,
                    OrganizationType = organization.Organization.OrganizationType?.Name ?? string.Empty
                };
            }

            if (organization.SubscriptionOrderCurriculums != null && organization.SubscriptionOrderCurriculums.Any())
            {
                var tasks = organization.SubscriptionOrderCurriculums
                    .Select(async sc =>
                    {
                        var cur = await _curriculumCacheService.GetCurriculumByIdAsync(sc.CurriculumId, cancellationToken);
                        return new GrpcCurriculumInformation
                        {
                            Id = sc.CurriculumId,
                            Title = cur?.Title ?? string.Empty,
                            Code = cur?.Code ?? string.Empty,
                            CourseCount = cur?.CourseCount ?? 0,
                            ImageUrl = cur?.ImageUrl ?? string.Empty
                        };
                    })
                    .ToList();

                var results = await Task.WhenAll(tasks);
                grpcOrganizationSubscriptionOrder.Curriculums.AddRange(results);
            }

            if (organization.ContractId > 0)
            {
                var contract = organization.Contract ?? await _unitOfWork.Contracts.FindByIdAsync(organization.ContractId, cancellationToken);
                if (contract != null)
                {
                    grpcOrganizationSubscriptionOrder.Contract = new GrpcContractInformation
                    {
                        Id = contract.Id,
                        Name = contract.Name,
                        FileUrl = contract.FileUrl
                    };
                }
            }

            if (organization.PlanBillingCycleId > 0)
            {
                var pbc = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(organization.PlanBillingCycleId, cancellationToken);
                if (pbc != null)
                {
                    grpcOrganizationSubscriptionOrder.PlanBillingCycle = pbc.BillingCycle.ToString();
                    grpcOrganizationSubscriptionOrder.PlanName = pbc.Name;
                }
            }

            return grpcOrganizationSubscriptionOrder;
        }
    }
}