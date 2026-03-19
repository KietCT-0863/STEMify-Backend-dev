using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Application.Commands.Contracts.CreateContract;
using Order.Application.Commands.Contracts.DeleteContract;
using Order.Application.Commands.Contracts.UpdateContract;
using Order.Application.Queries.Contracts.GetContractById;
using Order.Application.Queries.Contracts.GetContractList;
using ServiceStack;
using Shared.Enums;
using Shared.Protos.Order;

namespace Order.API.Services
{
    public class ContractGrpcService : GrpcContractService.GrpcContractServiceBase
    {
        private readonly IMediator _mediator;

        public ContractGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcContractDetail> CreateContract(
            CreateContractRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateContractCommand
            {
                Name = request.Name,
                Description = request.Description,
                FileBytes = request.File?.ToByteArray(),
                OrganizationId = request.OrganizationId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcContractDetail> GetContractById(
            GetContractRequest request,
            ServerCallContext context
        )
        {
            var query = new GetContractByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GrpcContractDetail> UpdateContract(
            UpdateContractRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateContractCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                FileBytes = request.File?.ToByteArray(),
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.ContractStatus.Active)
                //OrganizationId = request.OrganizationId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteContract(
            DeleteContractRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteContractCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedContractResponse> GetPagedContract(
            GetContractParams request,
            ServerCallContext context
        )
        {
            var query = new GetContractListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                OrganizationId = request.OrganizationId,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.ContractStatus.Active)
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}