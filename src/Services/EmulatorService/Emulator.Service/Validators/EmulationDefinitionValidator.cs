using FluentValidation;
using Emulator.Repository.Entities;

namespace Emulator.Service.Validators;

/// <summary>
/// Validator for EmulationDefinition
/// </summary>
public class EmulationDefinitionValidator : AbstractValidator<EmulationDefinition>
{
    public EmulationDefinitionValidator()
    {
        RuleFor(a => a.Metadata)
            .NotNull().WithMessage("Metadata is required");

        RuleFor(a => a.Metadata.Version)
            .NotEmpty().WithMessage("Metadata version is required");

        RuleFor(a => a.Metadata.Author)
            .NotEmpty().WithMessage("Metadata author is required");

        RuleFor(a => a.Metadata.Difficulty)
            .Must(d => new[] { "beginner", "intermediate", "advanced" }.Contains(d))
            .WithMessage("Difficulty must be beginner, intermediate, or advanced");

        RuleFor(a => a.Templates)
            .NotNull().WithMessage("Templates are required");

        RuleFor(a => a.Instances)
            .NotNull().WithMessage("Instances are required");

        RuleFor(a => a.Connections)
            .NotNull().WithMessage("Connections are required");

        RuleFor(a => a.Actions)
            .NotNull().WithMessage("Actions are required");

        RuleFor(a => a.Activities)
            .NotNull().WithMessage("Activities are required");

        RuleFor(a => a.Scene)
            .NotNull().WithMessage("Scene is required");
    }
}