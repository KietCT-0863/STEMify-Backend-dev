using FluentValidation;
using Emulator.Repository.Models;

namespace Emulator.Service.Validators;

/// <summary>
/// Validator for CreateEmulationRequest
/// </summary>
public class CreateEmulationRequestValidator : AbstractValidator<CreateEmulationRequest>
{
    public CreateEmulationRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(3, 100).WithMessage("Name must be between 3 and 100 characters");

        RuleFor(r => r.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(r => r.Description != null);

        RuleFor(r => r.Visibility)
            .NotEmpty().WithMessage("Visibility is required")
            .Must(v => new[] { "private", "organization", "public" }.Contains(v))
            .WithMessage("Visibility must be private, organization, or public");

        RuleFor(r => r.Definition)
            .NotNull().WithMessage("Definition is required")
            .SetValidator(new EmulationDefinitionValidator());
    }
}
