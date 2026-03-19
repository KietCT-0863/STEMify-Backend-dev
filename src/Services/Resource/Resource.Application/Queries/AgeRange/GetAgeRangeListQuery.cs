using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.AgeRange
{
    public class GetAgeRangeListQuery : IRequest<AgeRangeList> { }
}
