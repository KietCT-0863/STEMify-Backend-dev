using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CreateContact
{
    public class CreateContactCommand : IRequest<ContactResponse>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public int JobRoleId { get; set; } 

    }
}
