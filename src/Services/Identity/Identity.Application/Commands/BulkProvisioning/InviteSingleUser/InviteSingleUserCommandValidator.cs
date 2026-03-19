using FluentValidation;
using Identity.Domain.Enums;

namespace Identity.Application.Commands.BulkProvisioning.InviteSingleUser;

public class InviteSingleUserCommandValidator : AbstractValidator<InviteSingleUserCommand>
{
    public InviteSingleUserCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .GreaterThan(0)
            .WithMessage("Organization ID must be greater than 0");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Role must be a valid organization role")
            .Must(role =>
                role == OrganizationRole.Student
                || role == OrganizationRole.Teacher
                || role == OrganizationRole.OrganizationAdmin)
            .WithMessage("Role must be Student, Teacher, or OrganizationAdmin");

        RuleFor(x => x.LicenseType)
            .MaximumLength(100)
            .WithMessage("License type must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.LicenseType));

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.FullName)
            .MaximumLength(200)
            .WithMessage("Full name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.GroupName)
            .MaximumLength(100)
            .WithMessage("GroupName must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.GroupName));

        RuleFor(x => x.ExternalId)
            .MaximumLength(100)
            .WithMessage("External ID must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalId));

        RuleFor(x => x.InvitedBy)
            .NotEmpty()
            .WithMessage("InvitedBy is required");

        RuleFor(x => x.ExpirationDays)
            .GreaterThan(0)
            .WithMessage("Expiration days must be greater than 0")
            .LessThanOrEqualTo(365)
            .WithMessage("Expiration days must not exceed 365");
    }
}

