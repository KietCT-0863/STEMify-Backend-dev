using FluentValidation;
using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.UpdateOrganizationSubscriptionOrder
{
    public class UpdateOrganizationSubscriptionOrderCommand : IRequest<GrpcOrganizationSubscriptionOrderDetail>
    {
        public int Id { get; set; }
        //public int? OrganizationId { get; set; }
        //public int? PlanBillingCycleId { get; set; }
        //public int? ContractId { get; set; }
        //public int? ParentSubscriptionId { get; set; }
        //public string? PlanName { get; set; }
        //public decimal? GrossAmount { get; set; }
        //public decimal? NetAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public Domain.Enums.OrganizationSubscriptionOrderStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        //public DateTime? EndDate { get; set; }
        public int? MaxStudentSeats { get; set; }
        public int? MaxTeacherSeats { get; set; }
        public List<int>? CurriculumIds { get; set; } = new();
    }

    public class UpdateOrganizationSubscriptionOrderCommandValidator : AbstractValidator<UpdateOrganizationSubscriptionOrderCommand>
    {
        private const int MaxPlanNameLength = 255;

        public UpdateOrganizationSubscriptionOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Id must be greater than 0.");

            // Require at least one updatable field
            RuleFor(x => x)
                .Must(cmd =>
                    //cmd.OrganizationId.HasValue ||
                    //cmd.PlanBillingCycleId.HasValue ||
                    //cmd.ContractId.HasValue ||
                    //cmd.ParentSubscriptionId.HasValue ||
                    //cmd.PlanName != null ||
                    //cmd.GrossAmount.HasValue ||
                    //cmd.NetAmount.HasValue ||
                    cmd.DiscountPercent.HasValue ||
                    cmd.Status.HasValue ||
                    cmd.StartDate.HasValue ||
                    //cmd.EndDate.HasValue ||
                    cmd.MaxStudentSeats.HasValue ||
                    cmd.MaxTeacherSeats.HasValue ||
                    (cmd.CurriculumIds != null && cmd.CurriculumIds.Any()))
                .WithMessage("At least one field must be provided to update.");

            //When(x => x.OrganizationId.HasValue, () =>
            //{
            //    RuleFor(x => x.OrganizationId!.Value)
            //        .GreaterThan(0)
            //        .WithMessage("OrganizationId must be greater than 0.");
            //});

            //When(x => x.PlanBillingCycleId.HasValue, () =>
            //{
            //    RuleFor(x => x.PlanBillingCycleId!.Value)
            //        .GreaterThan(0)
            //        .WithMessage("PlanBillingCycleId must be greater than 0.");
            //});

            //When(x => x.ContractId.HasValue, () =>
            //{
            //    RuleFor(x => x.ContractId!.Value)
            //        .GreaterThan(0)
            //        .WithMessage("ContractId must be greater than 0.");
            //});

            //When(x => x.ParentSubscriptionId.HasValue, () =>
            //{
            //    RuleFor(x => x.ParentSubscriptionId!.Value)
            //        .GreaterThan(0)
            //        .WithMessage("ParentSubscriptionId must be greater than 0 when provided.");
            //});

            //When(x => x.PlanName != null, () =>
            //{
            //    RuleFor(x => x.PlanName)
            //        .NotEmpty()
            //        .WithMessage("PlanName must not be empty.")
            //        .MaximumLength(MaxPlanNameLength)
            //        .WithMessage($"PlanName must not exceed {MaxPlanNameLength} characters.");
            //});

            //When(x => x.GrossAmount.HasValue, () =>
            //{
            //    RuleFor(x => x.GrossAmount!.Value)
            //        .GreaterThan(0)
            //        .WithMessage("GrossAmount must be greater than 0.");
            //});

            //When(x => x.NetAmount.HasValue, () =>
            //{
            //    RuleFor(x => x.NetAmount!.Value)
            //        .GreaterThanOrEqualTo(0)
            //        .WithMessage("NetAmount must be greater than or equal to 0.")
            //        .Must((cmd, net) => !cmd.GrossAmount.HasValue || net <= cmd.GrossAmount.Value)
            //        .WithMessage("NetAmount must be less than or equal to GrossAmount when GrossAmount is provided.");
            //});

            When(x => x.DiscountPercent.HasValue, () =>
            {
                RuleFor(x => x.DiscountPercent!.Value)
                    .InclusiveBetween(0m, 100m)
                    .WithMessage("DiscountPercent must be between 0 and 100.");
            });

            //When(x => x.StartDate.HasValue || x.EndDate.HasValue, () =>
            //{
            //    RuleFor(x => x)
            //        .Must(cmd =>
            //        {
            //            if (cmd.StartDate.HasValue && cmd.EndDate.HasValue)
            //                return cmd.StartDate.Value < cmd.EndDate.Value;
            //            return true;
            //        })
            //        .WithMessage("StartDate must be earlier than EndDate when both are provided.");
            //});

            When(x => x.MaxStudentSeats.HasValue, () =>
            {
                RuleFor(x => x.MaxStudentSeats!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MaxStudentSeats must be greater than or equal to 0.");
            });

            When(x => x.MaxTeacherSeats.HasValue, () =>
            {
                RuleFor(x => x.MaxTeacherSeats!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MaxTeacherSeats must be greater than or equal to 0.");
            });

            When(x => x.CurriculumIds != null, () =>
            {
                RuleFor(x => x.CurriculumIds!)
                    .Must(list => list.All(id => id > 0))
                    .WithMessage("All CurriculumIds must be positive integers.");
            });

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .Must(s => Enum.IsDefined(typeof(Domain.Enums.OrganizationSubscriptionOrderStatus), s))
                    .WithMessage("Invalid Status value.");
            });
        }
    }
}