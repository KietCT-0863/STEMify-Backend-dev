namespace Identity.Application.Dtos.BulkProvisioning;

public class InvitationSummaryDto
{
    public Guid InvitationId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public DateTime InvitedAt { get; set; }
}

public class InvitationsPageDto
{
    public List<InvitationSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}


