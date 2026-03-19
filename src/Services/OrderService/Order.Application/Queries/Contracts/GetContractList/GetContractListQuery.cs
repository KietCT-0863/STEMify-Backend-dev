using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Contracts.GetContractList
{
    public class GetContractListQuery : IRequest<GrpcPagedContractResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public int? OrganizationId { get; set; }
        public Domain.Enums.ContractStatus? Status { get; set; }
    }
}
