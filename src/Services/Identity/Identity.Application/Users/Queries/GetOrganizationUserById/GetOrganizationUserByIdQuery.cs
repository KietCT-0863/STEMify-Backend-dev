using Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;
using MediatR;

namespace Identity.Application.Users.Queries.GetOrganizationUserById;

public sealed record GetOrganizationUserByIdQuery(Guid OrganizationUserId) 
    : IRequest<OrganizationUserGroupedDto?>;


