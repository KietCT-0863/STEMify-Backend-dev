using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;
using MediatR;

namespace Identity.Application.Users.Queries.GetOrganizationUsersByUserId;

public sealed class GetOrganizationUsersByUserIdQueryHandler
    : IRequestHandler<GetOrganizationUsersByUserIdQuery, PagedResult<OrganizationUserGroupedDto>>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public GetOrganizationUsersByUserIdQueryHandler(
        IOrganizationUserRepository organizationUserRepository)
    {
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<PagedResult<OrganizationUserGroupedDto>> Handle(
        GetOrganizationUsersByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var organizationUsers = await _organizationUserRepository.GetByUserIdAsync(
            request.UserId,
            request.ActiveOnly,
            cancellationToken);

        if (organizationUsers.Count == 0)
        {
            return new PagedResult<OrganizationUserGroupedDto>(
                Enumerable.Empty<OrganizationUserGroupedDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        var firstOrgUser = organizationUsers[0];
        var user = firstOrgUser.User;
        if (user == null)
        {
            return new PagedResult<OrganizationUserGroupedDto>(
                Enumerable.Empty<OrganizationUserGroupedDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        var fullName = !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName
            : $"{user.FirstName} {user.LastName}".Trim();

        var subscriptions = organizationUsers
            .Select(ou => new SubscriptionInfoDto
            {
                OrganizationUserId = ou.Id,
                OrganizationId = ou.OrganizationId,
                OrganizationRole = ou.OrganizationRole.ToString(),
                LicenseType = ou.OrganizationRole.ToString(),
                LicenseAssignmentId = null,
                SubscriptionOrderId = null,
                IsActive = true,
                JoinedAt = ou.JoinedAt,
                GroupName = ou.Group?.Name,
                GroupCode = ou.Group?.Code,
                Bio = ou.Bio,
                StudentDateOfBirth = ou.StudentDateOfBirth,
                StudentMajor = ou.StudentMajor,
                TeacherSpecialization = ou.TeacherSpecialization
            })
            .OrderByDescending(s => s.JoinedAt)
            .ToList();

        var dto = new OrganizationUserGroupedDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FullName = fullName,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            LastLoginAt = user.LastLoginAt,
            Subscriptions = subscriptions
        };

        return new PagedResult<OrganizationUserGroupedDto>(
            new[] { dto },
            1,
            request.PageNumber,
            request.PageSize);
    }
}


