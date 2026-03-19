namespace Payment.Domain.Enums
{
    public enum RefundStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }
}
