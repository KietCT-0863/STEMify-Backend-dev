using MediatR;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Queries
{
    public class GetComponentListQuery : IRequest<PagedComponentList>
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? Search { get; set; }
    }
}
