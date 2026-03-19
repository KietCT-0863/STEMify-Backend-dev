namespace Identity.Application.Dtos.Grpc;

/// <summary>
/// DTO representing organization with bulk provisioning specific information
/// Includes email domain and license seat counts
/// </summary>
public class OrganizationBulkProvisioningDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailDomain { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<SubscriptionLicenseInfoDto> Subscriptions { get; set; } = new();

    /// <summary>
    /// Get total available seats across all subscriptions for a license type
    /// </summary>
    public int GetTotalAvailableSeats(string licenseType)
    {
        return licenseType.ToLower() switch
        {
            "student" => Subscriptions.Sum(s => s.AvailableStudentSeats),
            "teacher" => Subscriptions.Sum(s => s.AvailableTeacherSeats),
            "organizationadmin" => Subscriptions.Sum(s => s.AvailableOrganizationAdminSeats),
            _ => 0
        };
    }

    /// <summary>
    /// Check if email matches organization's email domain
    /// </summary>
    public bool EmailMatchesDomain(string email)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(EmailDomain))
            return false;

        var emailDomain = email.Split('@').LastOrDefault();
        return emailDomain?.Equals(EmailDomain, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}

/// <summary>
/// DTO representing subscription with license seat information
/// </summary>
public class SubscriptionLicenseInfoDto
{
    public int SubscriptionOrderId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    // Max seats
    public int MaxStudentSeats { get; set; }
    public int MaxTeacherSeats { get; set; }
    public int MaxOrganizationAdminSeats { get; set; }

    // Current usage
    public int CurrentStudentSeats { get; set; }
    public int CurrentTeacherSeats { get; set; }
    public int CurrentOrganizationAdminSeats { get; set; }

    // Available seats (calculated)
    public int AvailableStudentSeats { get; set; }
    public int AvailableTeacherSeats { get; set; }
    public int AvailableOrganizationAdminSeats { get; set; }

    public bool IsActive => Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
}
