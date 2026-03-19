using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Product.Application.Features.KitComponents.Commands;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class KitComponentGrpcService : GrpcKitComponentService.GrpcKitComponentServiceBase
    {
        private readonly IMediator _mediator;
        public KitComponentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }
        public override async Task<Empty> CreateKitComponent
            (CreateKitComponentRequest request, ServerCallContext context)
        {
            var command = new CreateKitComponentCommand
            {
                KitId = request.KitId,
                KitComponents = request.Components.Select(kc => new CreateKitComponentDto
                {
                    ComponentId = kc.ComponentId,
                    Quantity = kc.Quantity,
                    IsMainComponent = kc.IsMainComponent
                }).ToList()
            };
            var result = await _mediator.Send(command);
            return new Empty();
        }

        public override async Task<Empty> DeleteKitComponent
            (DeleteKitComponentRequest request, ServerCallContext context)
        {
            var command = new DeleteKitComponentCommand
            {
                Ids = request.Ids.ToList()
            };
            await _mediator.Send(command);
            return new Empty();
        }

        public override async Task<Empty> UpdateKitComponent
            (UpdateKitComponentRequest request, ServerCallContext context)
        {
            var query = new UpdateKitComponentCommand
            {
                KitComponents = request.Components.Select(kc => new UpdateKitComponentDto
                {
                    Id = kc.Id,
                    Quantity = kc.Quantity,
                    IsMainComponent = kc.IsMainComponent
                }).ToList()
            };
            var result = await _mediator.Send(query);
            return new Empty();
        }


    }
}
