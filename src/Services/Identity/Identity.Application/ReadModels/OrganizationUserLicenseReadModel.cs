using Shared.Enums;

namespace Identity.Application.ReadModels;


public class OrganizationUserLicenseReadModel
{

    public int LicenseAssignmentId { get; set; }

    public Guid OrganizationUserId { get; set; }

    public Guid UserId { get; set; }


    public int OrganizationId { get; set; }


    public int SubscriptionOrderId { get; set; }

   
    public string LicenseType { get; set; } = string.Empty;


    public LicenseAssignmentStatus Status { get; set; } = LicenseAssignmentStatus.Pending;


    public DateTime AssignedAt { get; set; }


    public DateTime LastUpdatedAt { get; set; }
}


