using System;
using Contracts.Domains;

namespace EventBus.Messages.Subscription;

public record SubscriptionActivatedEvent : DomainEvent
{
    public int SubscriptionOrderId { get; init; }

    public int OrganizationId { get; init; }

    public string OrganizationName { get; init; }

    public string PlanName { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public int MaxStudentSeats { get; init; }

    public int MaxTeacherSeats { get; init; }

    public SubscriptionActivatedEvent(
        int subscriptionOrderId,
        int organizationId,
        string organizationName,
        string planName,
        DateTime startDate,
        DateTime endDate,
        int maxStudentSeats,
        int maxTeacherSeats)
    {
        SubscriptionOrderId = subscriptionOrderId;
        OrganizationId = organizationId;
        OrganizationName = organizationName;
        PlanName = planName;
        StartDate = startDate;
        EndDate = endDate;
        MaxStudentSeats = maxStudentSeats;
        MaxTeacherSeats = maxTeacherSeats;
    }
}

