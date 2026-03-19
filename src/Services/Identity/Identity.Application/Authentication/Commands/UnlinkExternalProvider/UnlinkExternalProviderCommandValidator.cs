using FluentValidation;

namespace Identity.Application.Authentication.Commands.UnlinkExternalProvider;

/// <summary>
/// Validator for UnlinkExternalProviderCommand
/// </summary>
public class UnlinkExternalProviderCommandValidator
    : AbstractValidator<UnlinkExternalProviderCommand>
{
    public UnlinkExternalProviderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .WithMessage("Provider name is required")
            .MaximumLength(50)
            .WithMessage("Provider name must not exceed 50 characters");
    }
}
