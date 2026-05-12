using MediatR;
using Resource.Application.Commands.Quiz;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class UpdateQuizCommandHandler : IRequestHandler<UpdateQuizCommand, QuizResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateQuizCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuizResponse> Handle(
            UpdateQuizCommand request,
            CancellationToken cancellationToken
        )
        {
            // Retrieve the existing Quiz with tracking enabled for update
            var quiz = await _unitOfWork.Quizzes.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with ID {request.Id} not found.");

            // Retrieve the associated Content with tracking enabled for update
            var content = await _unitOfWork.Contents.FindByIdForUpdateAsync(quiz.ContentId, cancellationToken);
            if (content == null)
                throw new KeyNotFoundException($"Content with ID {quiz.ContentId} not found.");

            // Update fields if they are provided in the request
            // EF Core will automatically track these changes
            if (request.TimeLimitMinutes.HasValue)
                quiz.TimeLimitInMinutes = request.TimeLimitMinutes.Value;
            if (request.DurationDays.HasValue)
                quiz.DurationDays = request.DurationDays.Value;
            if (request.CooldownHours.HasValue)
                quiz.CooldownHours = request.CooldownHours.Value;
            if (request.MaxAttemptAllowed.HasValue)
                quiz.MaxAttemptAllowed = request.MaxAttemptAllowed.Value;
            if (!string.IsNullOrEmpty(request.Description))
                quiz.Description = request.Description;
            if (request.TotalMarks.HasValue)
                quiz.TotalMarks = request.TotalMarks.Value;
            if (request.PassingMarks.HasValue)
                quiz.PassingMarks = request.PassingMarks.Value;
            if (!string.IsNullOrEmpty(request.Title))
                quiz.Title = request.Title;

            // Update Content status if provided
            if (request.Status.HasValue)
            {
                content.Status = request.Status.Value;
            }

            // SaveChanges will automatically update tracked entities
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new QuizResponse
            {
                Id = quiz.Id,
                TotalMarks = quiz.TotalMarks,
                PassingMarks = quiz.PassingMarks,
                DurationDays = quiz.DurationDays,
                TimeLimitMinutes = quiz.TimeLimitInMinutes,
                Description = quiz.Description,
                Status = content.Status.ToString(),
                Title = quiz.Title,
                ContentId = quiz.ContentId,
                CooldownHours = quiz.CooldownHours,
                MaxAttemptAllowed = quiz.MaxAttemptAllowed,
            };

            return response;
        }
    }
}
