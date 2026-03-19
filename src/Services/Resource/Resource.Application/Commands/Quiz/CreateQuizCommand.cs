using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Quiz
{
    public class CreateQuizCommand : IRequest<QuizResponse>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double TotalMarks { get; set; }
        public double PassingMarks { get; set; }
        public int DurationDays { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public int SectionId { get; set; }
    }

    public class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
    {
        public CreateQuizCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(255)
                .WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.DurationDays)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Duration must be 1 or greater.");

            RuleFor(x => x.SectionId)
                .GreaterThan(0)
                .WithMessage("SectionId must be greater than 0.");

            RuleFor(x => x.TotalMarks)
                .GreaterThan(0)
                .WithMessage("TotalMarks must be greater than 0.");

            RuleFor(x => x.PassingMarks)
                .GreaterThanOrEqualTo(0)
                .WithMessage("PassingMarks must be greater than or equal to 0.")
                .LessThanOrEqualTo(x => x.TotalMarks)
                .WithMessage("PassingMarks must not exceed TotalMarks.");

            RuleFor(x => x)
                .Must(x => x.PassingMarks <= x.TotalMarks)
                .WithMessage("PassingMarks must not exceed TotalMarks.");

            When(x => x.CooldownHours.HasValue, () =>
            {
                RuleFor(x => x.CooldownHours.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("CooldownHours must be 0 or greater.");
            });

            When(x => x.MaxAttemptAllowed.HasValue, () =>
            {
                RuleFor(x => x.MaxAttemptAllowed.Value)
                    .GreaterThan(0)
                    .WithMessage("MaxAttemptAllowed must be greater than 0.");
            });
        }
    }
}
