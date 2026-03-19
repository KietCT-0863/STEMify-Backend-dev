using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Course
{
    public class CreateCourseCommand : IRequest<CourseResponse>
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string StudentTasks { get; set; }
        public string? Prerequisites { get; set; }
        public string CreatedByUserId { get; set; }
        public int AgeRangeId { get; set; }
        public int? KitId { get; set; }
        public CourseLevel Level { get; set; } = CourseLevel.Beginner;
        public List<int> CurriculumIds { get; set; } = new List<int>();
    }

    public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
    {
        public CreateCourseCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(255)
                .WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Code is required.")
                .MaximumLength(255)
                .WithMessage("Code must not exceed 50 characters.");

            RuleFor(x => x.Slug)
                .NotEmpty()
                .WithMessage("Slug is required.")
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must be lowercase and hyphen-separated.");

            RuleFor(x => x.ImageBytes)
                .Must(bytes => bytes == null || bytes.Length <= 5 * 1024 * 1024)
                .WithMessage("Image size must not exceed 5 MB.");

            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.StudentTasks).NotEmpty().WithMessage("Student tasks is required.");

            RuleFor(x => x.Level).IsInEnum().WithMessage("Status must be a valid enum value.");

            RuleFor(x => x.CreatedByUserId).NotEmpty().WithMessage("CreatedByUserId is required.");
        }
    }
}
