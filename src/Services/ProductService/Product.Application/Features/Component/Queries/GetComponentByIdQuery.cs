using MediatR;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Queries
{
    public class GetComponentByIdQuery : IRequest<ComponentResponse>
    {
        public int Id { get; set; }
    }
}
