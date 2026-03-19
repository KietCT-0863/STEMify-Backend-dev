using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Users.Queries.GetActiveUsersQuery;

public class GetActiveUsersQuery : IRequest<IEnumerable<ActiveUserDto>>
{
    public string? UserType { get; set; }
}

public class ActiveUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // TPT-specific properties
    public string? Bio { get; set; }
    public string? Specialization { get; set; } // For teachers
    public string? Major { get; set; } // For students
    public int? Age { get; set; } // For students
}

/// <summary>
/// Handler for GetActiveUsersQuery using Identity UnitOfWork
/// </summary>
public class GetActiveUsersQueryHandler(IIdentityUnitOfWork unitOfWork)
    : IRequestHandler<GetActiveUsersQuery, IEnumerable<ActiveUserDto>>
{
    public async Task<IEnumerable<ActiveUserDto>> Handle(
        GetActiveUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        // Get active users
        var activeUsers = await unitOfWork.Users.GetActiveUsersAsync();

        // Apply optional type filtering
        if (!string.IsNullOrWhiteSpace(request.UserType))
        {
            activeUsers = activeUsers.Where(u =>
                u.Role.ToString().Equals(request.UserType, StringComparison.OrdinalIgnoreCase)
            );
        }

        return activeUsers.Select(user => new ActiveUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Bio = null,
            Specialization = null,
            Major = null,
            Age = null,
        });
    }
}
