using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Users.Queries.GetUserInfo;

/// <summary>
/// Handler for GetUserInfoQuery using new Application repository interfaces
/// </summary>
public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, UserInfoDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public GetUserInfoQueryHandler(IUserRepository userRepository, IOrganizationUserRepository organizationUserRepository)
    {
        _userRepository = userRepository;
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<UserInfoDto> Handle(
        GetUserInfoQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new NotFoundException($"User with ID {request.UserId} not found");

        var userInfo = new UserInfoDto
        {
            Sub = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            EmailVerified = user.IsEmailConfirmed(),
            Name = user.FullName,
            GivenName = user.FirstName,
            FamilyName = user.LastName,
            UserType = user.Role.ToString(),
            Status = user.Status,
            UserName = user.UserName
        };

        var memberships = await _organizationUserRepository.GetByUserIdAsync(user.Id, true, cancellationToken);
        var orgId = memberships.FirstOrDefault()?.OrganizationId;
        if (orgId.HasValue)
        {
            userInfo.OrganizationId = orgId.Value;
        }

        return userInfo;
    }
}
