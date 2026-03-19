using FluentValidation;

namespace Identity.Application.Authentication.Commands.ProcessExternalLogin;

/// <summary>
/// Validator for ProcessExternalLoginCommand
/// </summary>
public class ProcessExternalLoginCommandValidator
    : AbstractValidator<ProcessExternalLoginCommand>
{
    public ProcessExternalLoginCommandValidator()
    {
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

        RuleFor(x => x.ExternalLoginInfo.Email)
            .NotEmpty()
            .WithMessage("Email from external provider is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .When(x => x.ExternalLoginInfo != null);

        RuleFor(x => x.ExternalLoginInfo.FirstName)
            .MaximumLength(50)
            .WithMessage("First name must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalLoginInfo?.FirstName));

        RuleFor(x => x.ExternalLoginInfo.LastName)
            .MaximumLength(50)
            .WithMessage("Last name must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalLoginInfo?.LastName));

        RuleFor(x => x.DefaultUserRole)
            .IsInEnum()
            .WithMessage("Default user role must be a valid user role");
    }
}
