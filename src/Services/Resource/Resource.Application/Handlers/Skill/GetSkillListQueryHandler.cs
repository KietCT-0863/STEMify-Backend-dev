using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Skill;
using Resource.Application.Specifications.Skills;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Skill
{
    public class GetSkillListQueryHandler : IRequestHandler<GetSkillListQuery, SkillList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetSkillListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SkillList> Handle(
            GetSkillListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new SkillWithIncludesSpecification();
                var skills = await _unitOfWork.Skills.GetAllAsync(spec, cancellationToken);

                var skillList = new SkillList();
                foreach (var skill in skills)
                {
                    var response = new SkillResponse { Id = skill.Id, SkillName = skill.SkillName };
                    skillList.Skills.Add(response);
                }

                return skillList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the Skill list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
