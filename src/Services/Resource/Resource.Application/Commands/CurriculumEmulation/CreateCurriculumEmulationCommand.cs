using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.CurriculumEmulation
{
    public class CreateCurriculumEmulationCommand : IRequest
    {
        public int CurriculumId { get; set; }
        public List<string> EmulationIds { get; set; } = [];
    }

    public class CreateCurriculumEmulationCommandValidator : AbstractValidator<CreateCurriculumEmulationCommand>
    {
        public CreateCurriculumEmulationCommandValidator()
        {
            RuleFor(x => x.CurriculumId).GreaterThan(0).WithMessage("Curriculum ID must be greater than 0.");
            RuleFor(x => x.EmulationIds)
            .NotNull().WithMessage("EmulationIds cannot be null.")
            .NotEmpty().WithMessage("At least one CourseId must be provided.");
        }
    }
}
