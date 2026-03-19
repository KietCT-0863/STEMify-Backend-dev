namespace Contracts.Common.Domain
{
    public interface IBusinessRule
    {
        string Message { get; }
        int Status { get; }
        bool IsBroken();
    }
}
