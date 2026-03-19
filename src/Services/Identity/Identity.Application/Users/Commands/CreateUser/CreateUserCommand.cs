using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Users.Commands.CreateUser;

public record CreateUserCommand : IRequest<CreateUserResponse>
{
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public UserRole Role { get; init; }

    // Computed property for validation
    public string FullName => $"{FirstName} {LastName}";

    // Role-specific properties
    public string? Bio { get; init; }
    public string? Specialization { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Major { get; init; }
}
