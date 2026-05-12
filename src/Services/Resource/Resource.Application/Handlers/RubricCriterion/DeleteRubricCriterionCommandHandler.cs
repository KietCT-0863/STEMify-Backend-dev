using MediatR;
using Resource.Application.Commands.RubricCriterion;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.RubricCriterion
{
    public class DeleteRubricCriterionCommandHandler : IRequestHandler<DeleteRubricCriterionCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteRubricCriterionCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteRubricCriterionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var rubricCriterion = await _unitOfWork.RubricCriterions.FindByIdForUpdateAsync(
                    request.Id,
                    cancellationToken
                );
                if (rubricCriterion == null)
                    throw new KeyNotFoundException($"RubricCriterion with ID {request.Id} not found.");

                await _unitOfWork.RubricCriterions.DeleteAsync(rubricCriterion, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while deleting the RubricCriterion: {ex.Message}",
                    ex
                );
            }
        }
    }
}
