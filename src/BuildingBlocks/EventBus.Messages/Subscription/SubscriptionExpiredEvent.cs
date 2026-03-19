using System;
using Contracts.Domains;

namespace EventBus.Messages.Subscription;

public record SubscriptionExpiredEvent : DomainEvent
{
    public int SubscriptionOrderId { get; init; }

    public int OrganizationId { get; init; }

    public string OrganizationName { get; init; }

    public string PlanName { get; init; }

    public DateTime EndDate { get; init; }

    public SubscriptionExpiredEvent(
        int subscriptionOrderId,
        int organizationId,
        string organizationName,
        string planName,
        DateTime endDate)
    {
        SubscriptionOrderId = subscriptionOrderId;
        OrganizationId = organizationId;
        OrganizationName = organizationName;
        PlanName = planName;
        EndDate = endDate;
    }
}

