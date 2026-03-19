using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Course
{
    public class UpdateCourseCommand : IRequest<CourseResponse>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? StudentTasks { get; set; }
        public string? Prerequisites { get; set; }
        public CourseStatus? Status { get; set; }
        public CourseLevel? Level { get; set; }
        public int? AgeRangeId { get; set; }
        public int? KitId { get; set; }
        public List<int> CurriculumIds { get; set; } = new List<int>();
    }

    public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
    {
        public UpdateCourseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Course ID must be greater than 0.");

            RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be a valid enum value.");
            RuleFor(x => x.Level).IsInEnum().WithMessage("Level must be a valid enum value.");
        }
    }
}
