using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Users.Queries.GetUserInfoFromClaims;

/// <summary>
/// Handler for GetUserInfoFromClaimsQuery using new Application repository interfaces
/// </summary>
public class GetUserInfoFromClaimsQueryHandler
    : IRequestHandler<GetUserInfoFromClaimsQuery, UserInfoResponseDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserInfoFromClaimsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserInfoResponseDto> Handle(
        GetUserInfoFromClaimsQuery request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(request.SubjectId))
            throw new ArgumentException("Subject claim not found in access token");

        if (!Guid.TryParse(request.SubjectId, out var userId))
            throw new ArgumentException("Invalid subject claim format");

        // Use repository to get user with TPT inheritance
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        var response = new UserInfoResponseDto { Sub = user.Id.ToString() };

        // Include claims based on scopes
        if (request.Scopes.Contains("email"))
        {
            response.Email = user.Email ?? string.Empty;
        }

        if (request.Scopes.Contains("profile"))
        {
            // Use TPT inheritance properties
            response.Name = user.FullName;
            response.GivenName = user.FirstName;
            response.FamilyName = user.LastName;
            response.UserType = user.Role.ToString().ToLowerInvariant();

        }

        if (request.Scopes.Contains("phone"))
        {
            response.PhoneNumber = user.PhoneNumber;
        }

        if (request.Scopes.Contains("roles"))
        {
            response.Roles = new[] { user.Role.ToString() };
        }

        return response;
    }
}
