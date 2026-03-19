using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.RubricCriterion;
using Resource.Application.Specifications.RubricCriterions;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.RubricCriterion
{
    public class GetRubricCriterionByIdQueryHandler
        : IRequestHandler<GetRubricCriterionByIdQuery, RubricCriterionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetRubricCriterionByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RubricCriterionResponse> Handle(
            GetRubricCriterionByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new RubricCriterionByIdSpecification(request.Id);
            var rubricCriterion = await _unitOfWork.RubricCriterions.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );
            if (rubricCriterion == null)
                throw new KeyNotFoundException($"RubricCriterion with ID {request.Id} not found.");

            var response = new RubricCriterionResponse()
            {
                Id = rubricCriterion.Id,
                AssignmentQuestionId = rubricCriterion.AssignmentQuestionId,
                CriterionName = rubricCriterion.CriterionName,
                Description = rubricCriterion.Description,
                MaxPoints = (double)rubricCriterion.MaxPoints,
            };

            return response;
        }
    }
}
