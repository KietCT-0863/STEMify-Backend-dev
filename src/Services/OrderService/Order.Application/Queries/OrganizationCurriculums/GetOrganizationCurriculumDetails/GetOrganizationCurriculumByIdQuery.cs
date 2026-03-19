using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumDetails
{
    public class GetOrganizationCurriculumByIdQuery : IRequest<OrganizationCurriculumModel>
    {
        public int OrgId { get; set; }
        public int CurriculumId { get; set; }
    }
}
