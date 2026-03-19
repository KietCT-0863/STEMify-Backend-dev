using FluentValidation;
using MediatR;
using Product.Application.Models;
using Product.Domain.Enums;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Commands.UpdatePlan
{
    public class UpdatePlanCommand : IRequest<GrpcPlanModel>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public PlanStatus? Status { get; set; }
        public string? Description { get; set; }
        public string? AccessSupportDetail { get; set; }
        public int? MaxTeacherSeats { get; set; }
        public int? MaxStudentSeats { get; set; }
        public List<int>? CurriculumIds { get; set; }
        public List<BillingCycleDto>? BillingCycles { get; set; }
        public int? PlanBillingCycleId { get; set; }
        public bool? IsAddOn { get; set; }
        public int? CurriculumCount { get; set; } 
    }

    public class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
    {
        public UpdatePlanCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Plan ID must be greater than 0.");

            RuleFor(x => x.Name)
                .MaximumLength(255)
                .WithMessage("Plan name must not exceed 255 characters.")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.MaxStudentSeats)
                .GreaterThan(0)
                .WithMessage("MaxStudentSeats must be greater than 0.")
                .When(x => x.MaxStudentSeats.HasValue);

            RuleFor(x => x.MaxTeacherSeats)
                .GreaterThan(0)
                .WithMessage("MaxTeacherSeats must be greater than 0.")
                .When(x => x.MaxTeacherSeats.HasValue);

            RuleFor(x => x.CurriculumIds)
                .Must(x => x == null || x.All(id => id > 0))
                .WithMessage("All CurriculumIds must be positive integers.")
                .When(x => x.CurriculumIds != null);

            RuleForEach(x => x.BillingCycles)
                .ChildRules(b =>
                {
                    b.RuleFor(i => i.Price)
                        .GreaterThan(0)
                        .WithMessage("Each billing cycle Price must be greater than 0.");

                    b.RuleFor(i => i.BillingCycle)
                        .IsInEnum()
                        .WithMessage("BillingCycle must be valid.");
                })
                .When(x => x.BillingCycles != null);

            When(x => x.IsAddOn == true, () =>
            {
                RuleFor(x => x.PlanBillingCycleId)
                    .NotNull()
                    .WithMessage("PlanBillingCycleId is required when IsAddOn is true.");

                RuleFor(x => x.BillingCycles)
                    .Must(list => list == null || list.Count == 1)
                    .WithMessage("When IsAddOn is true, provide exactly one BillingCycles entry or none.");
            });

            When(x => x.IsAddOn == false, () =>
            {
                RuleFor(x => x.BillingCycles)
                    .Must(list =>
                        list == null || (
                            list.Count == 2
                            && list.Any(b => b.BillingCycle == Product.Domain.Enums.BillingCycle.Annual)
                            && list.Any(b => b.BillingCycle == Product.Domain.Enums.BillingCycle.Semiannual)
                        )
                    )
                    .WithMessage("When IsAddOn is false, provide exactly two BillingCycles entries (Annual and Semiannual) or none.");
            });
        }
    }
}