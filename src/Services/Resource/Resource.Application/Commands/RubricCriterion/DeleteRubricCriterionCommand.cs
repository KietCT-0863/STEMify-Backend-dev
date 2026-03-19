using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.RubricCriterion
{
    public class DeleteRubricCriterionCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteRubricCriterionCommandValidator : AbstractValidator<DeleteRubricCriterionCommand>
    {
        public DeleteRubricCriterionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than zero.");
        }
    }
}
