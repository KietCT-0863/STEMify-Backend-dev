using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Assignment
{
    public class UpdateAssignmentsCommand : IRequest<GrpcAssignmentModel>
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public decimal? PassingScore { get; set; }
        public int? DurationDays { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public List<UpdateAssignmentQuestionModel> AssignmentQuestions { get; set; } = new();
    }

    public class UpdateAssignmentQuestionModel
    {
        public int? Id { get; set; }
        public AssignmentQuestionType AssignmentQuestionType { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public decimal Points { get; set; }
    }

    public class UpdateAssignmentsCommandValidator : AbstractValidator<UpdateAssignmentsCommand>
    {
        private const decimal TotalPoints = 100m;
        private const decimal Epsilon = 0.0001m;

        public UpdateAssignmentsCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");

            When(x => x.PassingScore.HasValue, () =>
            {
                RuleFor(x => x.PassingScore.Value)
                    .GreaterThan(0m)
                    .WithMessage("PassingScore must be greater than 0.")
                    .LessThanOrEqualTo(100m)
                    .WithMessage("PassingScore must be less than or equal to 100.");
            });

            When(x => x.DurationDays.HasValue, () =>
            {
                RuleFor(x => x.DurationDays.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("DurationDays must be 0 or greater.");
            });

            When(x => x.AssignmentQuestions != null, () =>
            {
                RuleFor(x => x.AssignmentQuestions)
                    .NotEmpty()
                    .WithMessage("AssignmentQuestions must be provided and not empty.");

                RuleForEach(x => x.AssignmentQuestions)
                    .SetValidator(new UpdateAssignmentQuestionModelValidator());

                //RuleFor(x => x)
                //    .Must(cmd =>
                //        cmd.AssignmentQuestions != null
                //        && Math.Abs(cmd.AssignmentQuestions.Sum(q => q.Points) - TotalPoints) <= Epsilon)
                //    .WithMessage($"Sum of all question points must equal {TotalPoints}.");
            });

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

    public class UpdateAssignmentQuestionModelValidator : AbstractValidator<UpdateAssignmentQuestionModel>
    {
        public UpdateAssignmentQuestionModelValidator()
        {
            When(x => x.Id.HasValue, () =>
            {
                RuleFor(x => x.Id.Value)
                    .GreaterThan(0)
                    .WithMessage("Question Id must be greater than 0 when provided.");
            });

            RuleFor(x => x.AssignmentQuestionType)
                .IsInEnum()
                .WithMessage("AssignmentQuestionType must be a valid enum value.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required.");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("OrderIndex must be 0 or greater.");

            RuleFor(x => x.Points)
                .GreaterThan(0m)
                .WithMessage("Points must be greater than 0.");
        }
    }
}