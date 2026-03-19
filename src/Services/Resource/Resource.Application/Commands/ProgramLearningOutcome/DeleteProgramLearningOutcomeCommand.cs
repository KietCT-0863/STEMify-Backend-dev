using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.ProgramLearningOutcome
{
    public class DeleteProgramLearningOutcomeCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteProgramLearningOutcomeCommandValidator : AbstractValidator<DeleteProgramLearningOutcomeCommand>
    {
        public DeleteProgramLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ProgramLearningOutcome ID must be greater than 0.");
        }
    }
}
