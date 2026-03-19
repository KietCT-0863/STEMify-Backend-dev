using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Standard
{
    public class CreateStandardCommand : IRequest<StandardResponse>
    {
        public string? Description { get; set; }
        public string StandardName { get; set; } = string.Empty;
    }

    public class CreateStandardCommandValidator : AbstractValidator<CreateStandardCommand>
    {
        public CreateStandardCommandValidator()
        {
            RuleFor(x => x.StandardName)
                .NotEmpty()
                .WithMessage("Standard name is required.")
                .MaximumLength(255)
                .WithMessage("Standard name must not exceed 255 characters.");
        }
    }
}
