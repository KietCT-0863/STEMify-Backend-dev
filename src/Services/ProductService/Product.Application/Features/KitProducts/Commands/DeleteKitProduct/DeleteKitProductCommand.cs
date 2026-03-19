using MediatR;

namespace Product.Application.Features.KitProducts.Commands.DeleteKitProduct
{
    public class DeleteKitProductCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }
}
