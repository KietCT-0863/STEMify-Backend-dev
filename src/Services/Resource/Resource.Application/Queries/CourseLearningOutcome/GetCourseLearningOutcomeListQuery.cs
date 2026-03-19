using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.CourseLearningOutcome
{
    public class GetCourseLearningOutcomeListQuery : IRequest<CourseLearningOutcomeList> { }
}
