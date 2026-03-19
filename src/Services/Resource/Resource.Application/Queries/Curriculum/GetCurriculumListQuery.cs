using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Curriculum
{
    public class GetCurriculumListQuery : IRequest<CurriculumList> { }
}
