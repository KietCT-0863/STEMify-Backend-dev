using FluentValidation;
using MediatR;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.CreateOrganizationSubscriptionOrder
{
    public class CreateOrganizationSubscriptionOrderCommand : IRequest<Shared.Protos.Order.GrpcOrganizationSubscriptionOrderDetail>
    {
        public int OrganizationId { get; set; }
        public int PlanBillingCycleId { get; set; }
        public int? ContractId { get; set; }
        public int? ParentSubscriptionId { get; set; }
        public CreateContractDto? Contract { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public int MaxStudentSeats { get; set; }
        public int MaxTeacherSeats { get; set; }
        public List<int> CurriculumIds { get; set; } = [];
    }

    public class CreateContractDto
    {
        public string Name { get; set; } = string.Empty;
        public int OrganizationId { get; set; }
        public string? Description { get; set; }
        public byte[]? FileBytes { get; set; }
    }

    public class CreateOrganizationSubscriptionOrderCommandValidator : AbstractValidator<CreateOrganizationSubscriptionOrderCommand>
    {
        private const int MaxPlanNameLength = 255;

        public CreateOrganizationSubscriptionOrderCommandValidator()
        {
            RuleFor(x => x.OrganizationId)
                .GreaterThan(0)
                .WithMessage("OrganizationId must be greater than 0.");

            RuleFor(x => x.PlanBillingCycleId)
                .GreaterThan(0)
                .WithMessage("PlanBillingCycleId must be greater than 0.");

            RuleFor(x => x.ContractId)
                .GreaterThan(0)
                .WithMessage("ContractId must be greater than 0.");

            When(x => x.ParentSubscriptionId.HasValue, () =>
            {
                RuleFor(x => x.ParentSubscriptionId.Value)
                    .GreaterThan(0)
                    .WithMessage("ParentSubscriptionId must be greater than 0 when provided.");
            });

            //RuleFor(x => x.PlanName)
            //    .NotEmpty()
            //    .WithMessage("PlanName is required.")
            //    .MaximumLength(MaxPlanNameLength)
            //    .WithMessage($"PlanName must not exceed {MaxPlanNameLength} characters.");

            //RuleFor(x => x.GrossAmount)
            //    .GreaterThan(0)
            //    .WithMessage("GrossAmount must be greater than 0.");

            //RuleFor(x => x.NetAmount)
            //    .GreaterThanOrEqualTo(0)
            //    .WithMessage("NetAmount must be greater than or equal to 0.")
            //    .LessThanOrEqualTo(x => x.GrossAmount)
            //    .WithMessage("NetAmount must be less than or equal to GrossAmount.");

            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(0m, 100m)
                .WithMessage("DiscountPercent must be between 0 and 100.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.")
                .Must(date => date.Date >= DateTime.Now.Date)
                .WithMessage("Start date must be today or in the future.");

            //RuleFor(x => x.EndDate)
            //    .GreaterThan(x => x.StartDate)
            //    .WithMessage("EndDate must be later than StartDate.");

            RuleFor(x => x.MaxStudentSeats)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MaxStudentSeats must be greater than or equal to 0.");

            RuleFor(x => x.MaxTeacherSeats)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MaxTeacherSeats must be greater than or equal to 0.");

            RuleFor(x => x.CurriculumIds)
                .NotNull()
                .WithMessage("CurriculumIds must be provided (empty list is allowed).");

            RuleForEach(x => x.CurriculumIds)
                .GreaterThan(0)
                .WithMessage("Each CurriculumId must be a positive integer.");
        }
    }
}