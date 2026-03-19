using Contracts.Domains;

namespace Identity.Domain.Events;

public record UserProfileUpdatedEvent(
    Guid UserId,
    string ProfileType, // "Teacher" or "Student"
    string UpdatedFields,
    DateTime UpdatedAt,
    string? UpdatedBy = null
) : DomainEvent;
