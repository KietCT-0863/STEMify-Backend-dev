using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationList
{
    public class GetOrganizationListQuery : IRequest<GrpcPagedOrganizationResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public int? OrganizationTypeId { get; set; }
        public Domain.Enums.OrganizationStatus? Status { get; set; }
    }
}
