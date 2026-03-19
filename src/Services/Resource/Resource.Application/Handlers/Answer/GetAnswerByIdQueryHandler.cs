using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Answer;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Answer
{
    public class GetAnswerByIdQueryHandler : IRequestHandler<GetAnswerByIdQuery, AnswerResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAnswerByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AnswerResponse> Handle(
            GetAnswerByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var answer = await _unitOfWork.Answers.FindByIdAsync(request.Id, cancellationToken);

                if (answer == null)
                    throw new KeyNotFoundException($"Answer with ID {request.Id} not found.");

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
                    $"An error occurred while retrieving the answer: {ex.Message}",
                    ex
                );
            }
        }
    }
}
