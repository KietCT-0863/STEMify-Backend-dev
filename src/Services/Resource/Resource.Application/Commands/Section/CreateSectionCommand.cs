using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Section
{
    public class CreateSectionCommand : IRequest<SectionResponse>
    {
        public string Description { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        //public Domain.Enums.SectionStatus Status { get; set; }
        public int LessonId { get; set; }
        public bool IsVisibleToStudent { get; set; } = true;
    }

    public class CreateSectionCommandValidator : AbstractValidator<CreateSectionCommand>
    {
        public CreateSectionCommandValidator()
        {
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.Duration)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Duration must be 0 or greater.");

            //RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be a valid enum value.");

            RuleFor(x => x.LessonId).GreaterThan(0).WithMessage("LessonId must be greater than 0.");
        }
    }
}
