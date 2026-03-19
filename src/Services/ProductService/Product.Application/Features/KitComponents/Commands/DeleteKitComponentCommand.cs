using MediatR;

namespace Product.Application.Features.KitComponents.Commands
{
    public class DeleteKitComponentCommand : IRequest<bool>
    {
        public List<int> Ids { get; set; } = new();
    }
}
