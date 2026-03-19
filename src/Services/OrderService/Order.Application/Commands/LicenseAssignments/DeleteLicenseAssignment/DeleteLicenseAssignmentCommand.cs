using FluentValidation;
using MediatR;

namespace Order.Application.Commands.LicenseAssignments.DeleteLicenseAssignment
{
    public class DeleteLicenseAssignmentCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteLicenseAssignmentCommandValidator : AbstractValidator<DeleteLicenseAssignmentCommand>
    {
        public DeleteLicenseAssignmentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("LicenseAssignment ID must be greater than 0.");
        }
    }
}