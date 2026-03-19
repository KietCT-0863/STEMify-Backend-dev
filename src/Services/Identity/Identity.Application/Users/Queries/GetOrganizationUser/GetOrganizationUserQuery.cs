using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Queries.GetOrganizationUserById;

public sealed record GetOrganizationUserQuery(Guid UserId, int OrganizationId)
    : IRequest<OrganizationUserModel>;


