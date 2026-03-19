using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.CurriculumCourse
{
    public class DeleteCurriculumCourseCommand : IRequest
    {
        public int CurriculumId { get; set; }
        public List<int> CourseIds { get; set; }
    }

    public class DeleteCurriculumCourseCommandValidator : AbstractValidator<DeleteCurriculumCourseCommand>
    {
        public DeleteCurriculumCourseCommandValidator()
        {
            RuleFor(x => x.CurriculumId)
                .GreaterThan(0).WithMessage("CurriculumId must be greater than 0.");

            RuleFor(x => x.CourseIds)
                .NotNull().WithMessage("CourseIds cannot be null.")
                .NotEmpty().WithMessage("At least one CourseId must be provided.");
        }
    }
}
