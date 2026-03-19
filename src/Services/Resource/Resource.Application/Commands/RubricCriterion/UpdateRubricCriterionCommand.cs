using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.RubricCriterion
{
    public class UpdateRubricCriterionCommand : IRequest<RubricCriterionResponse>
    {
        public int Id { get; set; }
        public string? CriterionName { get; set; }
        public string? Description { get; set; }
        public decimal? MaxPoints { get; set; }
    }

    public class UpdateRubricCriterionCommandValidator : AbstractValidator<UpdateRubricCriterionCommand>
    {
        public UpdateRubricCriterionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than zero.");

            // If CriterionName is provided, it must not be empty and have a reasonable max length
            When(x => x.CriterionName != null, () =>
            {
                RuleFor(x => x.CriterionName)
                    .NotEmpty().WithMessage("CriterionName, when provided, cannot be empty.")
                    .MaximumLength(200).WithMessage("CriterionName must be 200 characters or fewer.");
            });

            // If Description is provided, enforce a max length
            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(1000).WithMessage("Description must be 1000 characters or fewer.");
            });

            // If MaxPoints is provided, it must be non-negative
            When(x => x.MaxPoints.HasValue, () =>
            {
                RuleFor(x => x.MaxPoints!.Value)
                    .GreaterThanOrEqualTo(0).WithMessage("MaxPoints must be greater than or equal to 0.");
            });
        }
    }
}