using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Curriculum;
using Resource.Application.Queries.Curriculum;
using Shared.Extensions;
using Shared.Protos.Resource;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
            var userId = string.IsNullOrWhiteSpace(request.CreatedByUserId)
                ? ExtractUserIdOrThrow(context)
                : request.CreatedByUserId;

            var command = new CreateCurriculumCommand
            {
                Title = request.Title,
                ImageBytes = request.Image?.ToByteArray(),
                Code = request.Code,
                Description = request.Description,
                CreatedByUserId = userId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        private string ExtractUserIdOrThrow(ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();

            // Try principal claims
            var principal = httpContext.User;
            var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal?.FindFirst("sub")?.Value
                         ?? httpContext.Request.Headers["X-User-Id"].FirstOrDefault();

            // Fallback: parse Authorization header (no validation)
            if (string.IsNullOrWhiteSpace(userId))
            {
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? authHeader.Substring("Bearer ".Length)
                        : authHeader;
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                    userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(userId))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing user identity"));

            return userId;
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
