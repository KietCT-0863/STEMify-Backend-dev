using MediatR;
using Resource.Application.Commands.Question;
using Resource.Application.Common.Interfaces;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Specifications.Questions;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Question
{
    public class UpdateQuestionsCommandHandler
    : IRequestHandler<UpdateQuestionsCommand, QuestionList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateQuestionsCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuestionList> Handle(UpdateQuestionsCommand request, CancellationToken cancellationToken)
        {
            var quiz = await _unitOfWork.Quizzes.FindByIdForUpdateAsync(request.QuizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {request.QuizId} not found.");
            }

            var spec = new QuestionByQuizIdSpecification(request.QuizId);
            var existingQuestions = await _unitOfWork.Questions
                .GetAllAsync(spec, cancellationToken);

            var incomingIds = new HashSet<int>();
            var responses = new QuestionList();

            foreach (var q in request.Questions)
            {
                Domain.Entities.Question question;

                // Create new question if Id is null
                if (q.Id == null)
                {
                    question = await CreateQuestionAsync(q, request.QuizId, cancellationToken);
                }
                // Update existing question
                else
                {
                    question = existingQuestions.FirstOrDefault(x => x.Id == q.Id.Value)
                               ?? throw new KeyNotFoundException($"Question ID {q.Id} not found.");

                    UpdateQuestion(question, q);
                    incomingIds.Add(q.Id.Value);
                }

                await SyncAnswers(question, q.Answers);

                var questionResponse = question.ToGrpcQuestionResponse();

                responses.Questions.Add(questionResponse);
            }

            var toDelete = existingQuestions.Where(q => !incomingIds.Contains(q.Id)).ToList();
            await _unitOfWork.Questions.DeleteRangeAsync(toDelete, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return responses;
        }


        private async Task<Domain.Entities.Question> CreateQuestionAsync(UpdateQuestionModel q, int quizId, CancellationToken ct)
        {
            var question = new Domain.Entities.Question
            {
                QuizId = quizId,
                Content = q.Content,
                QuestionType = q.QuestionType,
                OrderIndex = q.OrderIndex,
                FileUrl = q.FileUrl,
                AnswerExplanation = q.AnswerExplanation,
                Points = q.Points,
                Answers = new List<Domain.Entities.Answer>()
            };

            await _unitOfWork.Questions.AddAsync(question, ct);
            return question;
        }

        private void UpdateQuestion(Domain.Entities.Question entity, UpdateQuestionModel dto)
        {
            entity.Content = dto.Content;
            entity.QuestionType = dto.QuestionType;
            entity.OrderIndex = dto.OrderIndex;
            entity.FileUrl = dto.FileUrl;
            entity.AnswerExplanation = dto.AnswerExplanation;
            entity.Points = dto.Points;
        }

        private async Task SyncAnswers(Domain.Entities.Question question, List<UpdateAnswerModel> answerDtos)
        {
            var existing = question.Answers.ToList();
            var requestIds = answerDtos.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet();

            foreach (var a in answerDtos)
            {
                if (a.Id.HasValue)
                {
                    var answer = existing.FirstOrDefault(e => e.Id == a.Id.Value);
                    if (answer != null)
                    {
                        answer.Content = a.Content;
                        answer.IsCorrect = a.IsCorrect;
                    }
                }
                else
                {
                    question.Answers.Add(new Domain.Entities.Answer
                    {
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    });
                }
            }

            var toDelete = existing.Where(e => !requestIds.Contains(e.Id)).ToList();
            await _unitOfWork.Answers.DeleteRangeAsync(toDelete);
        }

    }
}
