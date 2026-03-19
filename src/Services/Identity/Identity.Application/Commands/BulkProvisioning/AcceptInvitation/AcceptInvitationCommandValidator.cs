using FluentValidation;

namespace Identity.Application.Commands.BulkProvisioning.AcceptInvitation;


public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationToken)
            .NotEmpty()
            .WithMessage("Invitation token is required")
            .MinimumLength(20)
            .WithMessage("Invalid invitation token format");

        RuleFor(x => x.GoogleEmail)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
