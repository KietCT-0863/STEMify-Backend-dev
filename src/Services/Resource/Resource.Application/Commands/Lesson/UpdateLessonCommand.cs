using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Lesson
{
    public class UpdateLessonCommand : IRequest<LessonResponse>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Image { get; set; } // Base64 string from Frontend
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
        public string? Description { get; set; }
        public string? LearningOutcome { get; set; }
        public string? Requirement { get; set; }
        public int? OrderIndex { get; set; }
        public int? Duration { get; set; }
        public Domain.Enums.LessonStatus? Status { get; set; }
        public List<int> SkillIds { get; set; } = new List<int>();
        public List<int> TopicIds { get; set; } = new List<int>();
        public List<int> StandardIds { get; set; } = new List<int>();
    }

    public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
    {
        public UpdateLessonCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Lesson ID must be greater than 0.");

            RuleFor(x => x.Image)
                .Must(base64 =>
                {
                    if (string.IsNullOrEmpty(base64)) return true;
                    try
                    {
                        var bytes = Convert.FromBase64String(base64);
                        return bytes.Length <= 5 * 1024 * 1024;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Image must be valid base64 and size must not exceed 5 MB.");

            RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be a valid enum value.");
        }
    }
}
