using Identity.Application.Common.Models;
using Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;
using MediatR;

namespace Identity.Application.Users.Queries.GetOrganizationUsersByUserId;

public sealed class GetOrganizationUsersByUserIdQuery
    : IRequest<PagedResult<OrganizationUserGroupedDto>>
{
    public GetOrganizationUsersByUserIdQuery(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; }

    public bool ActiveOnly { get; init; } = true;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 100;
}


