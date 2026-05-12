using MediatR;
using Resource.Application.Commands.Answer;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Answer
{
    public class UpdateAnswerCommandHandler : IRequestHandler<UpdateAnswerCommand, AnswerResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateAnswerCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AnswerResponse> Handle(
            UpdateAnswerCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var answer = await _unitOfWork.Answers.FindByIdForUpdateAsync(request.Id, cancellationToken);
                if (answer == null)
                    return null;

                answer.Content = request.Content;
                answer.IsCorrect = request.IsCorrect;

                await _unitOfWork.Answers.UpdateAsync(answer, cancellationToken);
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
                    $"An error occurred while updating the answer: {ex.Message}",
                    ex
                );
            }
        }
    }
}
