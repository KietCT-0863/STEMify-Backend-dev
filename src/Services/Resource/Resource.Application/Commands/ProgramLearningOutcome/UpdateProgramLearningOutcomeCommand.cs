using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.ProgramLearningOutcome
{
    public class UpdateProgramLearningOutcomeCommand : IRequest<ProgramLearningOutcomeResponse>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? CurriculumId { get; set; }
    }

    public class UpdateProgramLearningOutcomeCommandValidator : AbstractValidator<UpdateProgramLearningOutcomeCommand>
    {
        public UpdateProgramLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("ProgramLearningOutcome ID must be greater than 0.");
        }
    }
}
