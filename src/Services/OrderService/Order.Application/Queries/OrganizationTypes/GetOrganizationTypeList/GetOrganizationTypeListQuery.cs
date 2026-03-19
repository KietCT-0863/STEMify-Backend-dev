using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationTypes.GetOrganizationTypeList
{
    public class GetOrganizationTypeListQuery : IRequest<GrpcPagedOrganizationTypeResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
    }
}
