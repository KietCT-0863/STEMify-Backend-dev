using FluentValidation;
using MediatR;
using Product.Application.Models;
using Product.Domain.Enums;

namespace Product.Application.Features.Plans.Commands.CreatePlan
{
    public class CreatePlanCommand : IRequest<Shared.Protos.Product.GrpcPlanModel>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AccessSupportDetail { get; set; }
        public int MaxTeacherSeats { get; set; }
        public int MaxStudentSeats { get; set; }
        public List<int> CurriculumIds { get; set; } = new();
        public List<BillingCycleDto> BillingCycles { get; set; } = new();
        public int? PlanBillingCycleId { get; set; }
        public bool IsAddOn { get; set; } = false;
        public int CurriculumCount { get; set; } = 1;
    }

    public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
    {
        public CreatePlanCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Plan name is required.")
                .MaximumLength(255)
                .WithMessage("Plan name must not exceed 255 characters.");

            RuleFor(x => x.MaxStudentSeats)
                .GreaterThan(0)
                .WithMessage("MaxStudentSeats must be greater than 0.");

            RuleFor(x => x.MaxTeacherSeats)
                .GreaterThan(0)
                .WithMessage("MaxTeacherSeats must be greater than 0.");

            RuleForEach(x => x.BillingCycles)
                .ChildRules(b =>
                {
                    b.RuleFor(i => i.Price)
                        .GreaterThan(0)
                        .WithMessage("Each billing cycle Price must be greater than 0.");

                    b.RuleFor(i => i.BillingCycle)
                        .IsInEnum()
                        .WithMessage("BillingCycle must be valid.");
                });

            RuleFor(x => x.CurriculumIds)
                .Must(x => x == null || x.All(id => id > 0))
                .WithMessage("All CurriculumIds must be positive integers.");

            RuleFor(x => x.IsAddOn)
                .Must((cmd, isAddOn) => !isAddOn || cmd.PlanBillingCycleId.HasValue)
                .WithMessage("PlanBillingCycleId is required when IsAddOn is true.");

            When(x => x.IsAddOn == true, () =>
            {
                RuleFor(x => x.BillingCycles)
                    .NotNull()
                    .WithMessage("BillingCycles must be provided for addon plans.")
                    .Must(list => list != null && list.Count == 1)
                    .WithMessage("When IsAddOn is true, provide exactly one BillingCycles entry.");
            });

            When(x => x.IsAddOn == false, () =>
            {
                RuleFor(x => x.BillingCycles)
                    .NotNull()
                    .WithMessage("BillingCycles must be provided for non-addon plans.")
                    .Must(list =>
                        list != null
                        && list.Count == 2
                        && list.Any(b => b.BillingCycle == BillingCycle.Annual)
                        && list.Any(b => b.BillingCycle == BillingCycle.Semiannual)
                    )
                    .WithMessage("When IsAddOn is false, provide exactly two BillingCycles entries: Annual and Semiannual (one of each).");
            });
        }
    }
}