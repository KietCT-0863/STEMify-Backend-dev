using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Quiz
{
    public class UpdateQuizCommand : IRequest<QuizResponse>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public double? TotalMarks { get; set; }
        public double? PassingMarks { get; set; }
        public int? DurationDays { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public Domain.Enums.ContentStatus? Status { get; set; }
    }

    public class UpdateQuizCommandValidator : AbstractValidator<UpdateQuizCommand>
    {
        public UpdateQuizCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");

            When(x => !string.IsNullOrWhiteSpace(x.Title), () =>
            {
                RuleFor(x => x.Title!)
                    .NotEmpty()
                    .WithMessage("Title must not be empty when provided.")
                    .MaximumLength(255)
                    .WithMessage("Title must not exceed 255 characters.");
            });

            When(x => x.TotalMarks.HasValue, () =>
            {
                RuleFor(x => x.TotalMarks!.Value)
                    .GreaterThan(0)
                    .WithMessage("TotalMarks must be greater than 0 when provided.");
            });

            When(x => x.PassingMarks.HasValue, () =>
            {
                RuleFor(x => x.PassingMarks!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("PassingMarks must be greater than or equal to 0 when provided.");
            });

            // Ensure PassingMarks does not exceed TotalMarks when both are provided
            RuleFor(x => x)
                .Must(x => !x.PassingMarks.HasValue
                           || !x.TotalMarks.HasValue
                           || x.PassingMarks!.Value <= x.TotalMarks!.Value)
                .WithMessage("PassingMarks must not exceed TotalMarks when both are provided.");

            When(x => x.DurationDays.HasValue, () =>
            {
                RuleFor(x => x.DurationDays!.Value)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("DurationDays must be 1 or greater when provided.");
            });

            When(x => x.CooldownHours.HasValue, () =>
            {
                RuleFor(x => x.CooldownHours!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("CooldownHours must be 0 or greater when provided.");
            });

            When(x => x.MaxAttemptAllowed.HasValue, () =>
            {
                RuleFor(x => x.MaxAttemptAllowed!.Value)
                    .GreaterThan(0)
                    .WithMessage("MaxAttemptAllowed must be greater than 0 when provided.");
            });
        }
    }
}
