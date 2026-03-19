using MediatR;
using Resource.Application.Commands.Assignment;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Assignment
{
    public class CreateAssignmentCommandHandler
        : IRequestHandler<CreateAssignmentCommand, GrpcAssignmentModel>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateAssignmentCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcAssignmentModel> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var section = await _unitOfWork.Sections.FindByIdAsync(request.SectionId, cancellationToken);
            if (section == null)
            {
                throw new KeyNotFoundException($"Section with ID {request.SectionId} not found.");
            }
            // Create Content first
            var content = new Domain.Entities.Content
            {
                SectionId = request.SectionId,
                ContentType = Domain.Enums.ContentType.Assignment,
            };
            await _unitOfWork.Contents.AddAsync(content, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Then create Assignment linked to the Content
            var assignment = new Domain.Entities.Assignment
            {
                ContentId = content.Id,
                Title = request.Title,
                PassingScore = request.PassingScore,
                DurationDays = request.DurationDays,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                AssignmentQuestions = request.AssignmentQuestions.Select(q => new Domain.Entities.AssignmentQuestion
                {
                    Type = q.AssignmentQuestionType,
                    Content = q.Content,
                    OrderIndex = q.OrderIndex,
                    RubricCriterions = q.RubricCriterion.Select(r => new Domain.Entities.RubricCriterion
                    {
                        CriterionName = r.CriterionName,
                        Description = r.Description,
                        MaxPoints = r.MaxPoints
                    }).ToList(),
                    Points = q.RubricCriterion.Sum(r => r.MaxPoints)
                }).ToList()
            };

            await _unitOfWork.Assignments.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var responses = new GrpcAssignmentModel
            {
                Id = assignment.Id,
                ContentId = assignment.ContentId,
                Title = assignment.Title,
                TotalScore = (double)assignment.TotalScore,
                PassingScore = (double)assignment.PassingScore,
                DurationDays = assignment.DurationDays,
                CooldownHours = assignment.CooldownHours,
                MaxAttemptAllowed = assignment.MaxAttemptAllowed,
                Questions =
                {
                    assignment.AssignmentQuestions.Select(q => new GrpcAssignmentQuestionModel
                    {
                        Id = q.Id,
                        Type = q.Type.ToString(),
                        OrderIndex = q.OrderIndex,
                        Points = (double)q.Points,
                        Content = q.Content,
                        RubricCriterion =
                        {
                            q.RubricCriterions.Select(q => new RubricCriterionResponse
                            {
                                Id = q.Id,
                                CriterionName = q.CriterionName,
                                Description = q.Description,
                                MaxPoints = (double)q.MaxPoints
                            })
                        }
                    })
                }
            };

            return responses;
        }
    }
}
