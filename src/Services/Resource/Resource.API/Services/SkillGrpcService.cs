using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Skill;
using Resource.Application.Queries.Skill;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class SkillGrpcService : SkillService.SkillServiceBase
    {
        private readonly IMediator _mediator;

        public SkillGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<SkillResponse> CreateSkill(
            CreateSkillRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateSkillCommand { SkillName = request.SkillName };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<SkillResponse> GetSkill(
            GetSkillRequest request,
            ServerCallContext context
        )
        {
            var query = new GetSkillByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Skill with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<SkillResponse> UpdateSkill(
            UpdateSkillRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateSkillCommand
            {
                Id = request.Id,
                SkillName = request.SkillName,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteSkill(
            DeleteSkillRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteSkillCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<SkillList> ListSkills(Empty request, ServerCallContext context)
        {
            try
            {
                var result = await _mediator.Send(new GetSkillListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListSkills failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedSkillList> QuerySkills(
            QuerySkillsRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new QuerySkillsQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                };
                var result = await _mediator.Send(query);

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QuerySkills failed: {ex.Message}")
                );
            }
        }
    }
}
