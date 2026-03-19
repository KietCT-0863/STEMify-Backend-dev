using MediatR;
using Resource.Application.Commands.Skill;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Skills;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Skill
{
    public class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, SkillResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateSkillCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SkillResponse> Handle(
            UpdateSkillCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new SkillByIdSpecification(request.Id);
            var skill = await _unitOfWork.Skills.FirstOrDefaultAsync(spec, cancellationToken);
            if (skill == null)
                throw new KeyNotFoundException($"Skill with ID {request.Id} not found.");

            skill.SkillName = request.SkillName;

            await _unitOfWork.Skills.UpdateAsync(skill, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new SkillResponse() { Id = skill.Id, SkillName = skill.SkillName };

            return response;
        }
    }
}
