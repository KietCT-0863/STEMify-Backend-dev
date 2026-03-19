using Identity.Domain.Enums;

namespace Identity.Application.Dtos.BulkProvisioning;

public class CsvInvitationRow
{
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? GroupName { get; set; }
    public string? ExternalId { get; set; }
    public GroupGrade? Grade { get; set; }
    public int RowNumber { get; set; }
    public string? GroupCode { get; set; }

    public string GetLicenseType()
    {
        return Role switch
        {
            OrganizationRole.Student => "Student",
            OrganizationRole.Teacher => "Teacher",
            OrganizationRole.OrganizationAdmin => "OrganizationAdmin",
            _ => throw new InvalidOperationException($"Invalid role for organization user: {Role}")
        };
    }
}


