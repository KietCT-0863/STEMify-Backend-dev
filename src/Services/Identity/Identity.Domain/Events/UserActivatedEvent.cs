using Contracts.Domains;

namespace Identity.Domain.Events;

public record UserActivatedEvent(
    Guid UserId,
    string Email,
    DateTime ActivatedAt,
    string? ActivatedBy
) : DomainEvent;
