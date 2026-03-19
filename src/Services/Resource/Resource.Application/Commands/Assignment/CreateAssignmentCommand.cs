using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Assignment
{
    public class CreateAssignmentCommand : IRequest<GrpcAssignmentModel>
    {
        public int SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal PassingScore { get; set; } = 80;
        public int? DurationDays { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public List<CreateAssignmentQuestionModel>? AssignmentQuestions { get; set; }
    }

    public class CreateAssignmentQuestionModel
    {
        public AssignmentQuestionType AssignmentQuestionType { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<CreateRubricCriterionModel> RubricCriterion { get; set; } = new();
    }

    public class CreateRubricCriterionModel
    {
        public string CriterionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxPoints { get; set; }
    }

    public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
    {
        private const decimal TotalPoints = 100m;
        private const decimal Epsilon = 0.0001m;

        public CreateAssignmentCommandValidator()
        {
            RuleFor(x => x.SectionId)
                .GreaterThan(0)
                .WithMessage("SectionId must be greater than 0.");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required.")
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.PassingScore)
                .GreaterThan(0m)
                .WithMessage("PassingScore must be greater than 0.")
                .LessThanOrEqualTo(100m)
                .WithMessage("PassingScore must be less than or equal to 100.");

            When(x => x.AssignmentQuestions?.Any() == true, () =>
            {
                RuleForEach(x => x.AssignmentQuestions)
                    .SetValidator(new CreateAssignmentQuestModelValidator());
            });

            When(x => x.CooldownHours.HasValue, () =>
            {
                RuleFor(x => x.CooldownHours.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("CooldownHours must be 0 or greater.");
            });

            //RuleFor(x => x)
            //    .Must(cmd => cmd.AssignmentQuestions != null
            //                 && Math.Abs(cmd.AssignmentQuestions.Sum(q => q.Points) - TotalPoints) <= Epsilon)
            //    .WithMessage($"Sum of all question points must equal {TotalPoints}.");


            When(x => x.MaxAttemptAllowed.HasValue, () =>
            {
                RuleFor(x => x.MaxAttemptAllowed.Value)
                    .GreaterThan(0)
                    .WithMessage("MaxAttemptAllowed must be greater than 0.");
            });
        }
    }

    public class CreateAssignmentQuestModelValidator : AbstractValidator<CreateAssignmentQuestionModel>
    {
        public CreateAssignmentQuestModelValidator()
        {
            RuleFor(x => x.AssignmentQuestionType)
                .IsInEnum()
                .WithMessage("AssignmentType must be a valid enum value.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required.");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("OrderIndex must be 0 or greater.");
        }
    }
}