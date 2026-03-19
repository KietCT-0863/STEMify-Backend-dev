using FluentValidation;

namespace Identity.Application.Commands.BulkProvisioning.RevokeInvitation;

public class RevokeInvitationCommandValidator : AbstractValidator<RevokeInvitationCommand>
{
    public RevokeInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty()
            .WithMessage("Invitation ID is required");
    }
}
