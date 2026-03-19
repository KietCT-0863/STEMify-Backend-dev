using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Content;
using Resource.Application.Queries.Content;
using ServiceStack;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class ContentGrpcService : ContentService.ContentServiceBase
    {
        private readonly IMediator _mediator;

        public ContentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ContentResponse> CreateContent(
            CreateContentRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateContentCommand
            {
                ContentType = request.ContentType.ToEnumOrDefault(Domain.Enums.ContentType.Text),
                ContentBody = request.ContentBody,
                FileName = request.FileName,
                FileBytes = request.File?.ToByteArray(),
                SectionId = request.SectionId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<ContentResponse> GetContent(
            GetContentRequest request,
            ServerCallContext context
        )
        {
            var query = new GetContentByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Content with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<ContentResponse> UpdateContent(
            UpdateContentRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateContentCommand
            {
                Id = request.Id,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.ContentStatus.Published),
                //ContentType = string.IsNullOrEmpty(request.ContentType) ? null : request.Status.ToEnumOrDefault(Domain.Enums.ContentType.Text),
                ContentName = request.ContentBody,
                FileName = request.FileName,
                FileBytes = request.File?.ToByteArray(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteContent(
            DeleteContentRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteContentCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<ContentList> ListContents(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetContentListQuery());

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListContents failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedContentList> QueryContents(
            QueryContentsRequest request,
            ServerCallContext context
        )
        {
            Domain.Enums.ContentStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.ContentStatus>(
                        request.Status,
                        true,
                        out var parsedStatus
                    )
                )
                {
                    status = parsedStatus;
                }
            }

            Domain.Enums.ContentType? type = null;
            if (!string.IsNullOrWhiteSpace(request.ContentType))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.ContentType>(
                        request.ContentType,
                        true,
                        out var parsedType
                    )
                )
                {
                    type = parsedType;
                }
            }

            var query = new QueryContentsQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                Status = status,
                ContentType = type,
                SectionId = request.SectionId,
            };
            var result = await _mediator.Send(query);
            return result;
        }
    }
}
