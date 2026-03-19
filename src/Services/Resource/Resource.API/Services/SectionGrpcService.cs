using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Section;
using Resource.Application.Queries.Section;
using ServiceStack;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class SectionGrpcService : SectionService.SectionServiceBase
    {
        private readonly IMediator _mediator;

        public SectionGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<SectionResponse> CreateSection(
            CreateSectionRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateSectionCommand
            {
                Title = request.Title,
                Description = request.Description,
                Duration = request.Duration,
                LessonId = request.LessonId,
                IsVisibleToStudent = request.IsVisibleToStudent,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<SectionResponse> GetSection(
            GetSectionRequest request,
            ServerCallContext context
        )
        {
            var query = new GetSectionByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Section with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<SectionResponse> UpdateSection(
            UpdateSectionRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateSectionCommand
            {
                Id = request.Id,
                Description = request.Description,
                Duration = request.Duration,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.SectionStatus.Published),
                Title = request.Title,
                IsVisibleToStudent = request.IsVisibleToStudent,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteSection(
            DeleteSectionRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteSectionCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<SectionList> ListSections(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetSectionListQuery());

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListSections failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedSectionList> QuerySections(
            QuerySectionsRequest request,
            ServerCallContext context
        )
        {
            Domain.Enums.SectionStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.SectionStatus>(
                        request.Status,
                        true,
                        out var parsedStatus
                    )
                )
                {
                    status = parsedStatus;
                }
            }

            var query = new QuerySectionsQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                Status = status,
                LessonId = request.LessonId,
            };
            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<Empty> UpdateSectionsOrder(
            UpdateSectionsOrderRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateSectionsOrderCommand
            {
                LessonId = request.LessonId,
                OrderedSectionIds = request.OrderedSectionIds.ToList(),
            };

            await _mediator.Send(command);
            return new Empty();
        }
    }
}
