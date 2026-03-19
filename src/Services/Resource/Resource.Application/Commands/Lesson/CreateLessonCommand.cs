using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Lesson
{
    public class CreateLessonCommand : IRequest<LessonResponse>
    {
        public string Title { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Description { get; set; }
        public string LearningOutcome { get; set; }
        public string? Requirement { get; set; }
        public int OrderIndex { get; set; }
        public string CreatedByUserId { get; set; }
        public int CourseId { get; set; }
        public List<int> SkillIds { get; set; } = new List<int>();
        public List<int> TopicIds { get; set; } = new List<int>();
        public List<int> StandardIds { get; set; } = new List<int>();
    }

    public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
    {
        public CreateLessonCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(255)
                .WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.ImageBytes)
                .Must(bytes => bytes == null || bytes.Length <= 5 * 1024 * 1024)
                .WithMessage("Image size must not exceed 5 MB.");

            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.LearningOutcome).NotEmpty().WithMessage("LearningOutcome is required.");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("OrderIndex must be 0 or greater.");

            RuleFor(x => x.CreatedByUserId).NotEmpty().WithMessage("CreatedByUserId is required.");

            RuleFor(x => x.CourseId)
                .GreaterThan(0)
                .WithMessage("CourseId must be greater than 0.")
                .NotEmpty()
                .WithMessage("CourseId is required.");
        }
    }
}
