using Contracts.Domains;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities
{
    public class Contact : EntityAuditBase<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public int JobRoleId { get; set; }
        public Guid? HandledByUserId { get; set; }
        public ContactStatus Status { get; set; }

        // Navigation Properties
        public virtual JobRole JobRole { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
