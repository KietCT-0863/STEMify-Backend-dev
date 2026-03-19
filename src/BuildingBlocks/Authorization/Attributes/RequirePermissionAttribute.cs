using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
   public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));

        Permission = permission;
        Policy = $"RequirePermission:{permission}";
    }

   public string Permission { get; }
}
