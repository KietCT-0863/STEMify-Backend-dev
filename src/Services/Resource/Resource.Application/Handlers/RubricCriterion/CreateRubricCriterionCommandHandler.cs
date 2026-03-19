using MediatR;
using Resource.Application.Commands.RubricCriterion;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.RubricCriterion
{
    public class CreateRubricCriterionCommandHandler
        : IRequestHandler<CreateRubricCriterionCommand, RubricCriterionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateRubricCriterionCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RubricCriterionResponse> Handle(
            CreateRubricCriterionCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var rubricCriterion = new Domain.Entities.RubricCriterion
                {
                    CriterionName = request.CriterionName,
                    Description = request.Description,
                    AssignmentQuestionId = request.AssignmentQuestionId,
                    MaxPoints = request.MaxPoints,
                };

                await _unitOfWork.RubricCriterions.AddAsync(rubricCriterion, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new RubricCriterionResponse()
                {
                    Id = rubricCriterion.Id,
                    CriterionName = rubricCriterion.CriterionName,
                    Description = rubricCriterion.Description,
                    MaxPoints = (double)rubricCriterion.MaxPoints,
                    AssignmentQuestionId = rubricCriterion.AssignmentQuestionId,
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while creating the RubricCriterion: {ex.Message}",
                    ex
                );
            }
        }
    }
}
