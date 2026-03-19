using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Order.Domain.Enums;

namespace Order.Application.Commands.LicenseAssignments.CreateLicenseAssignment
{
    public class CreateLicenseAssignmentCommand : IRequest<Shared.Protos.Order.GrpcLicenseAssignmentListModel>
    {
        public List<CreateLicenseAssignmentModel> LicenseAssignments { get; set; } = new();
    }

    public class CreateLicenseAssignmentModel
    {
        public int OrganizationSubscriptionOrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public LicenseType Type { get; set; }
    }

    public class CreateLicenseAssignmentModelValidator : AbstractValidator<CreateLicenseAssignmentModel>
    {
        public CreateLicenseAssignmentModelValidator()
        {
            RuleFor(x => x.OrganizationSubscriptionOrderId)
                .GreaterThan(0)
                .WithMessage("OrganizationSubscriptionOrderId must be greater than 0.");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Type must be a valid license type (STUDENT, TEACHER, ORGANIZATIONADMIN).");
        }
    }

    public class CreateLicenseAssignmentCommandValidator : AbstractValidator<CreateLicenseAssignmentCommand>
    {
        public CreateLicenseAssignmentCommandValidator(
            IGrpcUserClient grpcUserClient,
            ILogger<CreateLicenseAssignmentCommandValidator> logger)
        {

            RuleFor(x => x.LicenseAssignments)
                .NotEmpty()
                .WithMessage("At least one license assignment must be provided.")
                .Must(x => x.Count <= 1000)
                .WithMessage("Cannot process more than 1000 license assignments in a single request.");

            // NEW: Check for duplicate (same subscription + email + type)
            RuleFor(x => x.LicenseAssignments)
                .Must(CheckNoDuplicatesInRequest)
                .WithMessage("Duplicate license assignment detected: same subscription, UserId, and type cannot be assigned twice in the same request.");
        }

        private bool CheckNoDuplicatesInRequest(List<CreateLicenseAssignmentModel> assignments)
        {
            var duplicates = assignments
                .GroupBy(a => new
                {
                    a.OrganizationSubscriptionOrderId,
                    UserId = a.UserId.Trim().ToLowerInvariant(),
                    a.Type
                })
                .Where(g => g.Count() > 1)
                .ToList();

            return duplicates.Count == 0;
        }
    }
}