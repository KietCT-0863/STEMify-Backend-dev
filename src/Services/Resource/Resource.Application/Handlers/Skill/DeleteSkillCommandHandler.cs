using MediatR;
using Resource.Application.Commands.Skill;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Skill
{
    public class DeleteSkillCommandHandler : IRequestHandler<DeleteSkillCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteSkillCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteSkillCommand request, CancellationToken cancellationToken)
        {
            var skill = await _unitOfWork.Skills.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (skill == null)
                throw new KeyNotFoundException($"Skill with ID {request.Id} not found.");

            await _unitOfWork.Skills.DeleteAsync(skill, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
