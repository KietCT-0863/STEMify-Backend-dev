using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Skill;
using Resource.Application.Specifications.Skills;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Skill
{
    public class GetSkillByIdQueryHandler : IRequestHandler<GetSkillByIdQuery, SkillResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetSkillByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SkillResponse> Handle(
            GetSkillByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new SkillByIdSpecification(request.Id);
            var skill = await _unitOfWork.Skills.FirstOrDefaultAsync(spec, cancellationToken);
            if (skill == null)
                throw new KeyNotFoundException($"Skill with ID {request.Id} not found.");

            var response = new SkillResponse() { Id = skill.Id, SkillName = skill.SkillName };

            return response;
        }
    }
}
