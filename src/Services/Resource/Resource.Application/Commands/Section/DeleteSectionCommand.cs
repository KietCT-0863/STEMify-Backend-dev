using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Section
{
    public class DeleteSectionCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteSectionCommandValidator : AbstractValidator<DeleteSectionCommand>
    {
        public DeleteSectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Section ID must be greater than 0.");
        }
    }
}
