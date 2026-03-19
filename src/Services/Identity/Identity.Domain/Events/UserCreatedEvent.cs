using Contracts.Domains;

namespace Identity.Domain.Events;

public record UserCreatedEvent(
    Guid UserId,
    string Email,
    string UserName,
    string UserType,
    DateTime CreatedAt
) : DomainEvent;
