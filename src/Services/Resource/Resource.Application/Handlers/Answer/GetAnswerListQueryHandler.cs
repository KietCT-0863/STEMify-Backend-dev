using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Answer;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Answer
{
    public class GetAnswerListQueryHandler : IRequestHandler<GetAnswerListQuery, AnswerList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAnswerListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AnswerList> Handle(
            GetAnswerListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var answers = await _unitOfWork.Answers.GetAllAsync(cancellationToken);

                var list = new AnswerList();
                foreach (var answer in answers)
                {
                    var response = new AnswerResponse
                    {
                        Id = answer.Id,
                        Content = answer.Content,
                        IsCorrect = answer.IsCorrect,
                        QuestionId = answer.QuestionId,
                    };

                    list.Answers.Add(response);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the answer list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
