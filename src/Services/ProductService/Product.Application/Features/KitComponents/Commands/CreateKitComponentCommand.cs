using MediatR;


namespace Product.Application.Features.KitComponents.Commands
{
    public class CreateKitComponentCommand : IRequest<bool>
    {
        public int KitId { get; set; }
        public List<CreateKitComponentDto> KitComponents { get; set; } = new List<CreateKitComponentDto>();
    }
    public class CreateKitComponentDto
    {
        public int ComponentId { get; set; }
        public int Quantity { get; set; }
        public bool IsMainComponent { get; set; } = false;
    }
}
