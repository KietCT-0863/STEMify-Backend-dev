using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Content
{
    public class GetContentListQuery : IRequest<ContentList> { }
}
