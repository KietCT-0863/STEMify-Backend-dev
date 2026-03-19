using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.EnsureUsersExist;

public class EnsureUsersExistCommand : IRequest<CheckUserExistsResponse>
{
    public List<string> Emails { get; set; } = new();
}




