using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.CourseLearningOutcome
{
    public class CreateCourseLearningOutcomeCommand : IRequest<CourseLearningOutcomeResponse>
    {
        public string? Description { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public List<int> ProgramLearningOutcomeIds { get; set; } = new List<int>();
    }

    public class CreateCourseLearningOutcomeCommandValidator : AbstractValidator<CreateCourseLearningOutcomeCommand>
    {
        public CreateCourseLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("CourseLearningOutcome name is required.")
                .MaximumLength(255)
                .WithMessage("CourseLearningOutcome name must not exceed 255 characters.");
        }
    }
}
