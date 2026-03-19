using FluentValidation;

namespace Identity.Application.Commands.BulkProvisioning.ResendInvitation;

public class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEmpty()
            .WithMessage("Invitation ID is required");
    }
}
