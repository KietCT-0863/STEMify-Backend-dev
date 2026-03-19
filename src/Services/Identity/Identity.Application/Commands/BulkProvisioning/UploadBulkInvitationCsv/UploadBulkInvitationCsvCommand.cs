using Identity.Application.Dtos.BulkProvisioning;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Identity.Application.Commands.BulkProvisioning.UploadBulkInvitationCsv;

public class UploadBulkInvitationCsvCommand : IRequest<BulkImportJobDto>
{
    public int OrganizationId { get; set; }
    public IFormFile CsvFile { get; set; } = null!;
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Optional: Subscription Order ID to use for license assignment
    /// If not provided, will use the active subscription
    /// </summary>
    public int? SubscriptionOrderId { get; set; }
}
