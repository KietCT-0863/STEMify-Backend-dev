namespace Identity.Application.Users.Commands.CreateUser;

public record CreateUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string Role { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}
