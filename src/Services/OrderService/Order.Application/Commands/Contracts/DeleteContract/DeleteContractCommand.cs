using FluentValidation;
using MediatR;

namespace Order.Application.Commands.Contracts.DeleteContract
{
    public class DeleteContractCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteContractCommandValidator : AbstractValidator<DeleteContractCommand>
    {
        public DeleteContractCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Contract ID must be greater than 0.");
        }
    }
}