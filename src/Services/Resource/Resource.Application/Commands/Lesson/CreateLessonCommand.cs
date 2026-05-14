using System;
using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Lesson
{
    public class CreateLessonCommand : IRequest<LessonResponse>
    {
        public string Title { get; set; }
        public string? Image { get; set; } // Base64 string from Frontend
        public string Description { get; set; }
        public string LearningOutcome { get; set; }
        public string? Requirement { get; set; }
        public int OrderIndex { get; set; }
        public string CreatedByUserId { get; set; }
        public int CourseId { get; set; }
        public List<int> SkillIds { get; set; } = new List<int>();
        public List<int> TopicIds { get; set; } = new List<int>();
        public List<int> StandardIds { get; set; } = new List<int>();
        
        // Helper property to convert base64 to bytes
        public byte[]? ImageBytes 
        { 
            get 
            {
                if (string.IsNullOrEmpty(Image))
                    return null;
                try
                {
                    return Convert.FromBase64String(Image);
                }
                catch
                {
                    return null;
                }
            }
        }
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

            RuleFor(x => x.Image)
                .Must(base64 => {
                    if (string.IsNullOrEmpty(base64)) return true;
                    try {
                        var bytes = Convert.FromBase64String(base64);
                        return bytes.Length <= 5 * 1024 * 1024;
                    } catch {
                        return false;
                    }
                })
                .WithMessage("Image must be valid base64 and size must not exceed 5 MB.");

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
