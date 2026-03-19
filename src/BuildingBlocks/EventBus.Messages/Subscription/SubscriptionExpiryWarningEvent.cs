using Contracts.Domains;

namespace EventBus.Messages.Subscription;

public record SubscriptionExpiryWarningEvent : DomainEvent
{
    public int SubscriptionOrderId { get; init; }

    public int OrganizationId { get; init; }

    public string OrganizationName { get; init; }

    public string PlanName { get; init; }

    public DateTime ExpiryDate { get; init; }

    public int DaysUntilExpiry { get; init; }

    public List<string> AdminUserIds { get; init; }

    public List<string> AdminEmails { get; init; }

    public int MaxStudentSeats { get; init; }

    public int MaxTeacherSeats { get; init; }

    public SubscriptionExpiryWarningEvent(
        int subscriptionOrderId,
        int organizationId,
        string organizationName,
        string planName,
        DateTime expiryDate,
        int daysUntilExpiry,
        List<string> adminUserIds,
        List<string> adminEmails,
        int maxStudentSeats,
        int maxTeacherSeats)
    {
        SubscriptionOrderId = subscriptionOrderId;
        OrganizationId = organizationId;
        OrganizationName = organizationName;
        PlanName = planName;
        ExpiryDate = expiryDate;
        DaysUntilExpiry = daysUntilExpiry;
        AdminUserIds = adminUserIds ?? new List<string>();
        AdminEmails = adminEmails ?? new List<string>();
        MaxStudentSeats = maxStudentSeats;
        MaxTeacherSeats = maxTeacherSeats;
    }
}
