using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.CourseLearningOutcome
{
    public class DeleteCourseLearningOutcomeCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteCourseLearningOutcomeCommandValidator : AbstractValidator<DeleteCourseLearningOutcomeCommand>
    {
        public DeleteCourseLearningOutcomeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("CourseLearningOutcome ID must be greater than 0.");
        }
    }
}
