using Resource.Application.Commands.RubricCriterion;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.RubricCriterion
{
    public class UpdateRubricCriterionCommandHandler
        : MediatR.IRequestHandler<UpdateRubricCriterionCommand, RubricCriterionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateRubricCriterionCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RubricCriterionResponse> Handle(
            UpdateRubricCriterionCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var rubricCriterion = await _unitOfWork.RubricCriterions.FindByIdForUpdateAsync(
                    request.Id,
                    cancellationToken
                );

                if (rubricCriterion == null)
                    throw new KeyNotFoundException($"RubricCriterion with ID {request.Id} not found.");

                // Apply updates only when values are provided
                if (!string.IsNullOrEmpty(request.CriterionName))
                {
                    rubricCriterion.CriterionName = request.CriterionName!;
                }

                if (request.Description != null)
                {
                    rubricCriterion.Description = request.Description;
                }

                if (request.MaxPoints.HasValue)
                {
                    rubricCriterion.MaxPoints = request.MaxPoints.Value;
                }

                await _unitOfWork.RubricCriterions.UpdateAsync(rubricCriterion, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new RubricCriterionResponse
                {
                    Id = rubricCriterion.Id,
                    AssignmentQuestionId = rubricCriterion.AssignmentQuestionId,
                    CriterionName = rubricCriterion.CriterionName ?? string.Empty,
                    Description = rubricCriterion.Description ?? string.Empty,
                    MaxPoints = (double)rubricCriterion.MaxPoints
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while updating the RubricCriterion: {ex.Message}",
                    ex
                );
            }
        }
    }
}