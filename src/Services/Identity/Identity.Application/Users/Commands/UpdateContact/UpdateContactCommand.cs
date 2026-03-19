using Identity.Domain.Enums;
using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.UpdateContact
{
    public class UpdateContactCommand : IRequest<ContactResponse>
    {
        public int Id { get; set; }
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public string? Email { get; set; } 
        public string? PhoneNumber { get; set; } 
        public string? OrganizationName { get; set; }
        public int? JobRoleId { get; set; }
        public ContactStatus? Status { get; set; }
    }
}
