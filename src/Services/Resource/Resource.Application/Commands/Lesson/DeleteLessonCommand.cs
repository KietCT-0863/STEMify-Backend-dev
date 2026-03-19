using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Lesson
{
    public class DeleteLessonCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
    {
        public DeleteLessonCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Lesson ID must be greater than 0.");
        }
    }
}
