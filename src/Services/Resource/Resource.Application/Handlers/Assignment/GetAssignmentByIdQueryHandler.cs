using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Assignment;
using Resource.Application.Specifications.Assignments;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Assignment
{
    public class GetAssignmentByIdQueryHandler
        : IRequestHandler<GetAssignmentByIdQuery, GrpcAssignmentModel>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAssignmentByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcAssignmentModel> Handle(
            GetAssignmentByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new AssignmentByIdSpecification(request.Id);
            var assignment = await _unitOfWork.Assignments.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with ID {request.Id} not found.");

            var response = new GrpcAssignmentModel
            {
                Id = assignment.Id,
                ContentId = assignment.ContentId,
                Title = assignment.Title,
                TotalScore = (double)assignment.TotalScore,
                PassingScore = (double)assignment.PassingScore,
                DurationDays = assignment.DurationDays,
                CooldownHours = assignment.CooldownHours,
                MaxAttemptAllowed = assignment.MaxAttemptAllowed,
                Questions = {
                    assignment.AssignmentQuestions.Select(q => new GrpcAssignmentQuestionModel
                    {
                        Id = q.Id,
                        Type = q.Type.ToString(),
                        OrderIndex = q.OrderIndex,
                        Points = (double)q.Points,
                        Content = q.Content,
                        RubricCriterion =
                        {
                            q.RubricCriterions.Select(r => new RubricCriterionResponse
                            {
                                Id = r.Id,
                                AssignmentQuestionId = q.Id,
                                CriterionName = r.CriterionName,
                                Description = r.Description,
                                MaxPoints = (double)r.MaxPoints
                            })
                        }
                    })
                }
            };

            return response;
        }
    }
}
