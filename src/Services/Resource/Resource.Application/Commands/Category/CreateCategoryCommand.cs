using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Category
{
    public class CreateTopicCommand : IRequest<CategoryResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
    {
        public CreateTopicCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters.");
        }
    }
}
