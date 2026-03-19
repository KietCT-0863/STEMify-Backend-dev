namespace Product.Application.Models
{
    public class PlanDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? Description { get; init; } = string.Empty;
        public string? AccessSupportDetail { get; init; } = string.Empty;
        public int CurriculumCount { get; init; }
        public int? MaxTeacherSeats { get; init; }
        public int? MaxStudentSeats { get; init; }
        public DateTimeOffset CreatedDate { get; init; }
        public DateTimeOffset? LastModifiedDate { get; init; }
        public List<PlanCurriculumDto>? Curriculums { get; init; }
        public List<PlanBillingCycleDto>? PlanBillingCycles { get; init; }
    }

    public class PlanCurriculumDto
    {
        public int Id { get; init; }
    }

    public class PlanBillingCycleDto
    {
        public int Id { get; init; }
        public int PlanId { get; init; }
        public string? BillingCycle { get; init; }
        public decimal Price { get; init; }
        public int? MaxTeacherSeats { get; init; }
        public int? MaxStudentSeats { get; init; }
        public bool IsAddOn { get; init; }
        public int? ParentPlanBillingCycleId { get; init; }
    }
}
