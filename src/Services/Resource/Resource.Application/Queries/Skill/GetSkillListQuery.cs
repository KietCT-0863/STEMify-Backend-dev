using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Skill
{
    public class GetSkillListQuery : IRequest<SkillList> { }
}
