using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CheckUserExists
{
    public class CheckUserIdExistsCommand : IRequest<CheckUserExistsResponse>
    {
        public List<string> UserIds { get; set; } = new();
    }
}