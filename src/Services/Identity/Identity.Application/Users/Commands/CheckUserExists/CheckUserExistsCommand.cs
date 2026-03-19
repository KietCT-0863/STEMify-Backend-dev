using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CheckUserExists
{
    public class CheckUserExistsCommand : IRequest<CheckUserExistsResponse>
    {
        public List<string> Email { get; set; } = new();
    }
}