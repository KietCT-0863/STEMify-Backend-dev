using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Answer
{
    public class GetAnswerListQuery : IRequest<AnswerList> { }
}
