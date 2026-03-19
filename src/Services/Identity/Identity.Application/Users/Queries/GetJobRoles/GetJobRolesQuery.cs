using MediatR;
using Shared.Protos.User;


namespace Identity.Application.Users.Queries.GetJobRoles
{
    public class GetJobRolesQuery : IRequest<PagedJobRoleList>
    {
        
    }
}
