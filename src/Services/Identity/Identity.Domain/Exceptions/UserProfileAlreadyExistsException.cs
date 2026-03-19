using Shared.Exceptions;

namespace Identity.Domain.Exceptions
{
    public class UserProfileAlreadyExistsException : DomainException
    {
        public UserProfileAlreadyExistsException(string profileType)
            : base($"{profileType} profile already exists", "PROFILE_ALREADY_EXISTS") { }
    }
}
