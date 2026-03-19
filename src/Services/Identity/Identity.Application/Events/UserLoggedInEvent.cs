using MediatR;

namespace Identity.Application.Events;

public record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime LoggedInAt,
    string? IpAddress = null,
    string? UserAgent = null
) : INotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
