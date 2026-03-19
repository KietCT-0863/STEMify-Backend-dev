using Order.Domain.Enums;

namespace Order.Application.Models
{
    public class OrganizationDto
    {
        public int Id { get; set; }
        public string OrganizationType { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
        public List<SubscriptionDto> Subscriptions { get; set; } = new();
    }
}
