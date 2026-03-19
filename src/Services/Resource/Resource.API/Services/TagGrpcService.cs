using Grpc.Core;
using MediatR;
using Resource.Application.Queries.Tags;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class TagGrpcService : TagService.TagServiceBase
    {
        private readonly IMediator _mediator;
        public TagGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async override Task<PagedTagList> GetTagList(TagParams request, ServerCallContext context)
        {
            try
            {
                var query = new GetTagListQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                };
                var result = await _mediator.Send(query);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"Get Tag List failed: {ex.Message}")
                );
            }
        }
    }
}
