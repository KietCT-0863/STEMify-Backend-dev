using System.Security.Claims;

namespace Identity.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<bool> AuthorizeAsync(string userId, string policyName);
    Task<(bool Result, string UserId)> CreateUserAsync(
        string email,
        string userName,
        string password
    );
    Task<bool> DeleteUserAsync(string userId);
    Task<ClaimsPrincipal> CreateUserPrincipalAsync(string userId);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<string?> FindUserByEmailAsync(string email);
}
