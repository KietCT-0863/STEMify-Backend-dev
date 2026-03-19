using MediatR;

namespace Identity.Application.Users.Commands.DeleteUser;

/// <summary>
/// Command to delete a user (soft delete)
/// </summary>
public class DeleteUserCommand : IRequest<bool>
{
    public Guid UserId { get; init; }

    public DeleteUserCommand(Guid userId)
    {
        UserId = userId;
    }
}
