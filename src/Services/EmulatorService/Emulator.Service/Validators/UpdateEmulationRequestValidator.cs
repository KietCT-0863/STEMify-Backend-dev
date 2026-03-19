using FluentValidation;
using Emulator.Repository.Models;

namespace Emulator.Service.Validators;

/// <summary>
/// Validator for UpdateEmulationRequest
/// </summary>
public class UpdateEmulationRequestValidator : AbstractValidator<UpdateEmulationRequest>
{
    public UpdateEmulationRequestValidator()
    {
        RuleFor(r => r.Name)
            .Length(3, 100).WithMessage("Name must be between 3 and 100 characters")
            .When(r => r.Name != null);

        RuleFor(r => r.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(r => r.Description != null);

        RuleFor(r => r.Status)
            .Must(s => new[] { "draft", "review", "published", "archived" }.Contains(s!))
            .WithMessage("Status must be draft, review, published, or archived")
            .When(r => r.Status != null);

        RuleFor(r => r.Definition)
            .SetValidator(new EmulationDefinitionValidator()!)
            .When(r => r.Definition != null);
    }
}