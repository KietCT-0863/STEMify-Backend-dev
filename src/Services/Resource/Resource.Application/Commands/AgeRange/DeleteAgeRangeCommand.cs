using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.AgeRange
{
    public class DeleteAgeRangeCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteAgeRangeCommandValidator : AbstractValidator<DeleteAgeRangeCommand>
    {
        public DeleteAgeRangeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be greater than zero.");
        }
    }
}
