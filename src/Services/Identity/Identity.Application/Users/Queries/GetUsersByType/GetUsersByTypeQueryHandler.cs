using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Users.Queries.GetUsersByType;

/// <summary>
/// Handler for GetUsersByTypeQuery using TPT inheritance
/// </summary>
public class GetUsersByTypeQueryHandler
    : IRequestHandler<GetUsersByTypeQuery, PagedResult<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByTypeQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserSummaryDto>> Handle(
        GetUsersByTypeQuery request,
        CancellationToken cancellationToken
    )
    {
        // Parse user type (validation handled by FluentValidation)
        Enum.TryParse<UserRole>(request.UserType, true, out var userRole);

        // Get all users by role
        var allUsers = await _userRepository.GetUsersByRoleAsync(userRole);

        var totalCount = allUsers.Count();

        // Apply pagination manually
        var users = allUsers.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);

        var userDtos = users.Select(user => new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserType = user.Role.ToString(),
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Bio = null,
            Specialization = null,
            Major = null,
            Age = null,
        });

        return new PagedResult<UserSummaryDto>(
            userDtos,
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
