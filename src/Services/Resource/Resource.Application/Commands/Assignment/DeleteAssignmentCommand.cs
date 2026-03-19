using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Assignment
{
    public class DeleteAssignmentsCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteAssignmentsCommandValidator : AbstractValidator<DeleteAssignmentsCommand>
    {
        public DeleteAssignmentsCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");
        }
    }
}
