using Contracts.Domains;

namespace EventBus.Messages.Subscription;

public record SubscriptionCancelledEvent : DomainEvent
{
    public List<int> LicenseAssignmentIds { get; set; } = new();
}

