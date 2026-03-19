using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.ProgramLearningOutcome
{
    public class GetProgramLearningOutcomeByIdQuery : IRequest<ProgramLearningOutcomeResponse>
    {
        public int Id { get; set; }

        public GetProgramLearningOutcomeByIdQuery(int id)
        {
            Id = id;
        }
    }

    public class GetProgramLearningOutcomeByIdQueryValidator : AbstractValidator<GetProgramLearningOutcomeByIdQuery>
    {
        public GetProgramLearningOutcomeByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ID must be greater than 0.");
        }
    }
}
