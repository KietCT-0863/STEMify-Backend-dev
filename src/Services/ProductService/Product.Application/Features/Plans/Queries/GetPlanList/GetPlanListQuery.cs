using MediatR;
using Product.Domain.Enums;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Queries.GetPlanList
{
    public class GetPlanListQuery : IRequest<GrpcPagedPlanResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public PlanStatus? Status { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public Domain.Enums.BillingCycle? BillingCycle { get; set; }
        public bool? IsAddOn { get; set; }
    }
}
