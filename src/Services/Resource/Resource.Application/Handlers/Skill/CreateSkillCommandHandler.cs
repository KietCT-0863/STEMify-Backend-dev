using MediatR;
using Resource.Application.Commands.Skill;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Skill
{
    public class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, SkillResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateSkillCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SkillResponse> Handle(
            CreateSkillCommand request,
            CancellationToken cancellationToken
        )
        {
            var skill = new Domain.Entities.Skill { SkillName = request.SkillName };

            await _unitOfWork.Skills.AddAsync(skill, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new SkillResponse() { Id = skill.Id, SkillName = skill.SkillName };
        }
    }
}
