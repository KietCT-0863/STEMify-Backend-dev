using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Assignment
{
    public class GetQuizTemplateQuery : IRequest<QuizQuestionsTemplate>
    { }
}
