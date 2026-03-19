using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Queries.GetContactById
{
    public class GetContactByIdQuery : IRequest<ContactResponse>
    {
        public int Id { get; set; }
    }
}
