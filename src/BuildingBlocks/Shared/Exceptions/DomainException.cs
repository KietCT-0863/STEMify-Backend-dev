namespace Shared.Exceptions
{
    public class DomainException : Exception
    {
        public string ErrorCode { get; }

        public DomainException(string message)
            : base(message)
        {
            ErrorCode = "DOMAIN_RULE_VIOLATION";
        }

        public DomainException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "DOMAIN_RULE_VIOLATION";
        }

        public DomainException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
