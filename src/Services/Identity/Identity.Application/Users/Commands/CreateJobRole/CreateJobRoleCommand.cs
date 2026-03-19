using Google.Protobuf.WellKnownTypes;
using MediatR;

namespace Identity.Application.Users.Commands.CreateJobRole
{
    public class CreateJobRoleCommand : IRequest
    {
        public List<string> Names { get; set; } = [];
    }
}
