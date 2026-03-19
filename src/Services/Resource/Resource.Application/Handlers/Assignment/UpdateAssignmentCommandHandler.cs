using MediatR;
using Resource.Application.Commands.Assignment;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Assignment
{
    public class UpdateAssignmentsCommandHandler
        : IRequestHandler<UpdateAssignmentsCommand, GrpcAssignmentModel>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateAssignmentsCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcAssignmentModel> Handle(UpdateAssignmentsCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _unitOfWork.Assignments.FindByIdAsync(request.Id, cancellationToken);
            if (assignment == null)
                throw new KeyNotFoundException($"Assignment with ID {request.Id} not found.");

            if (request.PassingScore.HasValue)
                assignment.PassingScore = request.PassingScore.Value;

            if (request.Title != null)
                assignment.Title = request.Title;

            if (request.DurationDays.HasValue)
                assignment.DurationDays = request.DurationDays;

            if (request.CooldownHours.HasValue)
                assignment.CooldownHours = request.CooldownHours;

            if (request.MaxAttemptAllowed.HasValue)
                assignment.MaxAttemptAllowed = request.MaxAttemptAllowed;

            var existingQuestions = (await _unitOfWork.AssignmentQuestions
                .FindAsync(q => q.AssignmentId == request.Id, cancellationToken))
                .ToList();

            var incomingIds = new HashSet<int>();

            foreach (var qDto in request.AssignmentQuestions)
            {
                Domain.Entities.AssignmentQuestion questionEntity;

                if (!qDto.Id.HasValue || qDto.Id.Value == 0)
                {
                    questionEntity = new Domain.Entities.AssignmentQuestion
                    {
                        AssignmentId = assignment.Id,
                        OrderIndex = qDto.OrderIndex,
                        Content = qDto.Content,
                        Type = qDto.AssignmentQuestionType,
                        Points = qDto.Points
                    };

                    await _unitOfWork.AssignmentQuestions.AddAsync(questionEntity, cancellationToken);
                }
                else
                {
                    questionEntity = existingQuestions.FirstOrDefault(x => x.Id == qDto.Id.Value)
                        ?? throw new KeyNotFoundException($"Assignment question with ID {qDto.Id.Value} not found.");

                    questionEntity.Content = qDto.Content;
                    questionEntity.OrderIndex = qDto.OrderIndex;
                    questionEntity.Points = qDto.Points;
                    questionEntity.Type = qDto.AssignmentQuestionType;

                    await _unitOfWork.AssignmentQuestions.UpdateAsync(questionEntity);
                    incomingIds.Add(qDto.Id.Value);
                }
            }

            var toDelete = existingQuestions
                .Where(q => !incomingIds.Contains(q.Id))
                .ToList();

            foreach (var del in toDelete)
                await _unitOfWork.AssignmentQuestions.DeleteAsync(del);

            await _unitOfWork.Assignments.UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new GrpcAssignmentModel
            {
                Id = assignment.Id,
                ContentId = assignment.ContentId,
                PassingScore = (double)assignment.PassingScore,
                DurationDays = assignment.DurationDays ?? 0,
                TotalScore = (double)assignment.TotalScore,
                MaxAttemptAllowed = assignment.MaxAttemptAllowed,
                CooldownHours = assignment.CooldownHours,
                Title = assignment.Title
            };

            var updatedQuestions = await _unitOfWork.AssignmentQuestions
                .FindAsync(q => q.AssignmentId == assignment.Id, cancellationToken);

            foreach (var question in updatedQuestions.OrderBy(q => q.OrderIndex))
            {
                response.Questions.Add(new GrpcAssignmentQuestionModel
                {
                    Id = question.Id,
                    OrderIndex = question.OrderIndex,
                    Content = question.Content,
                    Type = question.Type.ToString(),
                    Points = (double)question.Points
                });
            }

            return response;
        }
    }
}