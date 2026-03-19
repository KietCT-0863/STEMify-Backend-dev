namespace Order.Application.Models
{
    public class ExpiringSubscriptionDto
    {
        public int SubscriptionOrderId { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public int MaxStudentSeats { get; set; }
        public int MaxTeacherSeats { get; set; }
        public int CurrentStudentSeats { get; set; }
        public int CurrentTeacherSeats { get; set; }
        public List<string> AdminUserIds { get; set; } = new();
        public List<string> AdminEmails { get; set; } = new();
    }
}
