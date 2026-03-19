using MediatR;
using Resource.Application.Commands.Quiz;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class CreateQuizCommandHandler : IRequestHandler<CreateQuizCommand, QuizResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateQuizCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuizResponse> Handle(
            CreateQuizCommand request,
            CancellationToken cancellationToken
        )
        {
            // Create Content first
            var content = new Domain.Entities.Content
            {
                SectionId = request.SectionId,
                ContentType = Domain.Enums.ContentType.Quiz,
            };
            await _unitOfWork.Contents.AddAsync(content, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Then create Quiz linked to the Content
            var quiz = new Domain.Entities.Quiz
            {
                Title = request.Title,
                TotalMarks = request.TotalMarks,
                PassingMarks = request.PassingMarks,
                DurationDays = request.DurationDays,
                TimeLimitInMinutes = request.TimeLimitMinutes,
                ContentId = content.Id,
                Description = request.Description,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
            };

            await _unitOfWork.Quizzes.AddAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new QuizResponse
            {
                Id = quiz.Id,
                TotalMarks = quiz.TotalMarks,
                PassingMarks = quiz.PassingMarks,
                DurationDays = quiz.DurationDays,
                Status = quiz.Content.Status.ToString(),
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimitMinutes = quiz.TimeLimitInMinutes,
                CooldownHours = quiz.CooldownHours,
                MaxAttemptAllowed = quiz.MaxAttemptAllowed,
                ContentId = quiz.ContentId,
            };

            return response;
        }
    }
}
