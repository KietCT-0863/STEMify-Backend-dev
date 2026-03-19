namespace Payment.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6,
        Refunded = 7,
        PartiallyRefunded = 8
    }
}
