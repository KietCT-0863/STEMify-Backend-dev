using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Users.Commands.UpdateUser;

/// <summary>
/// Command to update user information
/// </summary>
public class UpdateUserCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public UserStatus? Status { get; init; }
    public string? Password { get; init; }
    public UserRole? UserRole { get; init; }
}
