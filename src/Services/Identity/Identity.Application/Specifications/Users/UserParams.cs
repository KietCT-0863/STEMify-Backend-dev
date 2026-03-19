using Identity.Domain.Enums;
using Shared.SeedWork;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Parameters for User specifications with filtering and paging support
/// </summary>
public class UserParams : PagingRequestParam
{
    private string? _search;

    public string? Search
    {
        get => _search;
        set => _search = value?.ToLower().Trim();
    }

    public string? Email { get; set; }
    public string? UserName { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public bool? IsEmailConfirmed { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
    public DateTime? LastLoginBefore { get; set; }
}
