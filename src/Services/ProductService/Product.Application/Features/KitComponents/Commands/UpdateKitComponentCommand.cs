using MediatR;

namespace Product.Application.Features.KitComponents.Commands
{
    public class UpdateKitComponentCommand : IRequest<bool>
    {
        public List<UpdateKitComponentDto> KitComponents { get; set; } = new();
    }

    public class UpdateKitComponentDto
    {
        public int Id { get; set; }
        public int? Quantity { get; set; }
        public bool? IsMainComponent { get; set; }
    }
}
