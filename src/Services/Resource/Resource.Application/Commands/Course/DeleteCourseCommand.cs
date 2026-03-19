using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Course
{
    public class DeleteCourseCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteCourseCommandValidator : AbstractValidator<DeleteCourseCommand>
    {
        public DeleteCourseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Course ID must be greater than 0.");
        }
    }
}
