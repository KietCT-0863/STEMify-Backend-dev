using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Curriculum;
using Resource.Application.Queries.Curriculum;
using Shared.Extensions;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CurriculumGrpcService : CurriculumService.CurriculumServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CurriculumGrpcService> _logger;

        public CurriculumGrpcService(IMediator mediator, ILogger<CurriculumGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<CurriculumResponse> CreateCurriculum(
            CreateCurriculumRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateCurriculumCommand
            {
                Title = request.Title,
                ImageBytes = request.Image?.ToByteArray(),
                Code = request.Code,
                Description = request.Description,
                CreatedByUserId = request.CreatedByUserId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CurriculumDetails> GetCurriculum(
            GetCurriculumRequest request,
            ServerCallContext context
        )
        {
            var query = new GetCurriculumByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Curriculum with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<CurriculumRelationsResponse> GetCurriculumRelations(
            GetCurriculumRelationsRequest request,
            ServerCallContext context
        )
        {
            var query = new GetCurriculumRelationsQuery(request.Id);
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<CurriculumResponse> UpdateCurriculum(
            UpdateCurriculumRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCurriculumCommand
            {
                Id = request.Id,
                Title = request.Title,
                Code = request.Code,
                ImageBytes = request.Image?.ToByteArray(),
                Description = request.Description,
                Status = request.Status.ToEnumOrNull<Domain.Enums.CurriculumStatus>(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteCurriculum(
            DeleteCurriculumRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCurriculumCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<CurriculumList> ListCurriculums(Empty request, ServerCallContext context)
        {
            try
            {
                var result = await _mediator.Send(new GetCurriculumListQuery());

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ListCurriculums failed");
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListCurriculums failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedCurriculumList> QueryCurriculums(
            QueryCurriculumsRequest request,
            ServerCallContext context
        )
        {
            Domain.Enums.CurriculumStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.CurriculumStatus>(
                        request.Status,
                        true,
                        out var parsedStatus
                    )
                )
                {
                    status = parsedStatus;
                }
            }

            Shared.Enums.SortDirection? sortDirection = null;
            if (!string.IsNullOrWhiteSpace(request.SortDirection))
            {
                if (
                    System.Enum.TryParse<Shared.Enums.SortDirection>(
                        request.SortDirection,
                        true,
                        out var parsedSortDirection
                    )
                )
                {
                    sortDirection = parsedSortDirection;
                }
            }
            var query = new QueryCurriculumsQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                CreatedByUserId = request.CreatedByUserId,
                Status = status,
                SortDirection = sortDirection,
                Code = request.Code,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}
