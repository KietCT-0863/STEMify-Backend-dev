namespace Payment.Application.Common.Interfaces
{
    public interface IPaymentEventPublisher
    {
        Task PublishPaymentCompletedAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            decimal amount,
            string currency,
            string providerTransactionId,
            string paymentProvider,
            DateTime completedAt,
            CancellationToken cancellationToken = default);

        Task PublishPaymentFailedAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            string failureReason,
            string paymentProvider,
            DateTime failedAt,
            CancellationToken cancellationToken = default);

        Task PublishPaymentCancelledAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            string cancellationReason,
            string paymentProvider,
            DateTime cancelledAt,
            CancellationToken cancellationToken = default);
    }
}
