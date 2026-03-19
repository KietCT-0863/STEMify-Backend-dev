using FluentValidation;

namespace Identity.Application.Authentication.Commands.LinkExternalProvider;

/// <summary>
/// Validator for LinkExternalProviderCommand
/// </summary>
public class LinkExternalProviderCommandValidator : AbstractValidator<LinkExternalProviderCommand>
{
    public LinkExternalProviderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.ExternalLoginInfo)
            .NotNull()
            .WithMessage("External login information is required");

        RuleFor(x => x.ExternalLoginInfo.Provider)
            .NotEmpty()
            .WithMessage("Provider name is required")
            .When(x => x.ExternalLoginInfo != null);

        RuleFor(x => x.ExternalLoginInfo.ProviderKey)
            .NotEmpty()
            .WithMessage("Provider key is required")
            .When(x => x.ExternalLoginInfo != null);
    }
}
