using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.ReadModels;
using Identity.Domain.Enums;
using Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;
using MediatR;
using Shared.Enums;

namespace Identity.Application.Users.Queries.GetOrganizationUserById;

public sealed class GetOrganizationUserByIdQueryHandler
    : IRequestHandler<GetOrganizationUserByIdQuery, OrganizationUserGroupedDto?>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;

    public GetOrganizationUserByIdQueryHandler(
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository)
    {
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
    }

    public async Task<OrganizationUserGroupedDto?> Handle(
        GetOrganizationUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var orgUsers = await _organizationUserRepository.GetByIdsAsync(
            new List<Guid> { request.OrganizationUserId },
            cancellationToken);

        var orgUser = orgUsers.FirstOrDefault();
        if (orgUser == null || orgUser.User == null)
        {
            return null;
        }

        var user = orgUser.User;
        var fullName = !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName
            : $"{user.FirstName} {user.LastName}".Trim();


        var licenseAssignments = await _licenseReadRepository.GetByOrganizationUserIdAsync(
            orgUser.Id,
            cancellationToken);

        var matchingLicense = licenseAssignments.FirstOrDefault(l => 
            l.Status == LicenseAssignmentStatus.Active && 
            l.LicenseType.Equals(orgUser.OrganizationRole.ToString(), StringComparison.OrdinalIgnoreCase));

        var isActive = matchingLicense?.Status == LicenseAssignmentStatus.Active;

        var dto = new OrganizationUserGroupedDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FullName = fullName,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            LastLoginAt = user.LastLoginAt,
            Subscriptions =
            {
                new SubscriptionInfoDto
                {
                    OrganizationUserId = orgUser.Id,
                    OrganizationId = orgUser.OrganizationId,
                    OrganizationRole = orgUser.OrganizationRole.ToString(),
                    LicenseType = orgUser.OrganizationRole.ToString(),
                    LicenseAssignmentId = matchingLicense?.LicenseAssignmentId.ToString(),
                    SubscriptionOrderId = matchingLicense?.SubscriptionOrderId,
                    IsActive = isActive,
                    JoinedAt = orgUser.JoinedAt,
                    GroupName = orgUser.Group != null ? orgUser.Group.Name : null,
                    GroupCode = orgUser.Group != null ? orgUser.Group.Code : null,
                    Bio = orgUser.Bio,
                    StudentDateOfBirth = orgUser.StudentDateOfBirth,
                    StudentMajor = orgUser.StudentMajor,
                    TeacherSpecialization = orgUser.TeacherSpecialization
                }
            }
        };

        return dto;
    }

}


