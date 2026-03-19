using Contracts.Domains;
using System.Text.Json;

namespace Order.Domain.Entities
{
    public class SubscriptionOrderCurriculum : EntityBase<int>
    {
        public int OrganizationSubscriptionOrderId { get; set; }
        public int CurriculumId { get; set; }
        public string CurriculumTitle { get; set; } = string.Empty;
        public string CurriculumCode { get; set; } = string.Empty;
        public string? CurriculumImageUrl { get; set; } 
        public string? CurriculumDescription { get; set; } 

        // snapshot resource ID
        public List<CourseSnapshot> CoursesSnapshot { get; set; } = [];
        public List<EmulatorSnapshot> EmulatorsSnapshot { get; set; } = [];

        // Navigation properties
        public OrganizationSubscriptionOrder OrganizationSubscriptionOrder { get; set; }
    }

    public class CourseSnapshot
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string Level { get; set; } = string.Empty;
        public int? KitId { get; set; }
}

    public class EmulatorSnapshot
    {
        public string EmulationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
