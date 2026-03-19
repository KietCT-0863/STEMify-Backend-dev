using Microsoft.AspNetCore.Http;

namespace Shared.Exceptions
{
    public class UnAuthorizedException(string message, System.Exception? innerException = null)
        : IdentityException(message, StatusCodes.Status401Unauthorized, innerException);
}
