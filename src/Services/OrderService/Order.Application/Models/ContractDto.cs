namespace Order.Application.Models
{
    public class ContractDto
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationType { get; set; }
        public string? OrganizationImageUrl { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? FileUrl { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
    }
}
