using MediatR;
using Order.Domain.Enums;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumList
{
    public class GetOrganizationCurriculumListQuery : IRequest<GrpcOrganizationCurriculumList>
    {
        public int OrgId { get; set; }
        public OrganizationSubscriptionOrderStatus Status { get; set; } = OrganizationSubscriptionOrderStatus.Active;
    }
}
