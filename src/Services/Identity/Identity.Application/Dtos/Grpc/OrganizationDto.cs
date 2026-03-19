namespace Identity.Application.Dtos.Grpc;

public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Code { get; set; } = string.Empty;

    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
}
