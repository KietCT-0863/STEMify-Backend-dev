namespace Identity.Application.Dtos.BulkProvisioning;

public class BulkImportJobsPageDto
{
    public List<BulkImportJobDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}


