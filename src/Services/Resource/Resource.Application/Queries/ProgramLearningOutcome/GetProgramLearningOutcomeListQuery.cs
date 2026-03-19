using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.ProgramLearningOutcome
{
    public class GetProgramLearningOutcomeListQuery : IRequest<ProgramLearningOutcomeList> { }
}
