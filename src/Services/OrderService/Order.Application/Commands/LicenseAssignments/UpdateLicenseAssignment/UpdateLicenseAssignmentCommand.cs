using FluentValidation;
using MediatR;
using Order.Domain.Enums;

namespace Order.Application.Commands.LicenseAssignments.UpdateLicenseAssignment
{
    public class UpdateLicenseAssignmentCommand : IRequest<Shared.Protos.Order.GrpcLicenseAssignmentModel>
    {
        public int Id { get; set; }
        public LicenseAssignmentStatus? Status { get; set; }
    }

    public class UpdateLicenseAssignmentCommandValidator : AbstractValidator<UpdateLicenseAssignmentCommand>
    {
        public UpdateLicenseAssignmentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status.Value)
                    .IsInEnum()
                    .WithMessage("Invalid LicenseAssignmentStatus value.");
            });
        }
    }
}