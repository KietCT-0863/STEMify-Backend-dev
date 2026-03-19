using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Standard
{
    public class GetStandardListQuery : IRequest<StandardList> { }
}
