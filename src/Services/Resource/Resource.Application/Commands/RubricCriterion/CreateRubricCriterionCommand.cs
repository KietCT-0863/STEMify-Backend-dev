using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.RubricCriterion
{
    public class CreateRubricCriterionCommand : IRequest<RubricCriterionResponse>
    {
        public string CriterionName { get; set; }
        public string? Description { get; set; }
        public int AssignmentQuestionId { get; set; }
        public decimal MaxPoints { get; set; }
    }

    public class CreateRubricCriterionCommandValidator : AbstractValidator<CreateRubricCriterionCommand>
    {
        public CreateRubricCriterionCommandValidator()
        {
            RuleFor(x => x.CriterionName)
                .NotEmpty().WithMessage("CriterionName is required.")
                .MaximumLength(255).WithMessage("CriterionName must be 200 characters or fewer.");

            RuleFor(x => x.AssignmentQuestionId)
                .GreaterThan(0).WithMessage("AssignmentQuestionId must be greater than zero.");

            RuleFor(x => x.MaxPoints)
                .GreaterThanOrEqualTo(0m).WithMessage("MaxPoints must be greater than or equal to 0.");

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(1000).WithMessage("Description must be 1000 characters or fewer.");
            });
        }
    }
}