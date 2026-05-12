using MediatR;
using Resource.Application.Commands.Curriculum;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Curriculum
{
    public class DeleteCurriculumCommandHandler : IRequestHandler<DeleteCurriculumCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCurriculumCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCurriculumCommand request, CancellationToken cancellationToken)
        {
            var curriculum = await _unitOfWork.Curriculums.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (curriculum == null)
                throw new KeyNotFoundException($"Curriculum with ID {request.Id} not found.");

            curriculum.Status = Domain.Enums.CurriculumStatus.Deleted;

            await _unitOfWork.Curriculums.UpdateAsync(curriculum, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
