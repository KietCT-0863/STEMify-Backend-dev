using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.ProgramLearningOutcome
{
    public class CreateProgramLearningOutcomeCommand : IRequest<ProgramLearningOutcomeResponse>
    {
        public string? Description { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurriculumId { get; set; }
    }

    public class CreateProgramLearningOutcomeCommandValidator : AbstractValidator<CreateProgramLearningOutcomeCommand>
    {
        public CreateProgramLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("ProgramLearningOutcome name is required.")
                .MaximumLength(255)
                .WithMessage("ProgramLearningOutcome name must not exceed 255 characters.");
        }
    }
}
