using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Quiz
{
    public class GetQuizListQuery : IRequest<QuizList> { }
}
