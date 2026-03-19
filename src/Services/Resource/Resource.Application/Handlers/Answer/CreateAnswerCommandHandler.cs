using MediatR;
using Resource.Application.Commands.Answer;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Answer
{
    public class CreateAnswerCommandHandler : IRequestHandler<CreateAnswerCommand, AnswerResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateAnswerCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AnswerResponse> Handle(
            CreateAnswerCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var answer = new Domain.Entities.Answer
                {
                    Content = request.Content,
                    IsCorrect = request.IsCorrect,
                    QuestionId = request.QuestionId,
                };

                await _unitOfWork.Answers.AddAsync(answer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new AnswerResponse
                {
                    Id = answer.Id,
                    Content = answer.Content,
                    IsCorrect = answer.IsCorrect,
                    QuestionId = answer.QuestionId,
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while creating the answer: {ex.Message}",
                    ex
                );
            }
        }
    }
}
