using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Section
{
    public class GetSectionListQuery : IRequest<SectionList> { }
}
