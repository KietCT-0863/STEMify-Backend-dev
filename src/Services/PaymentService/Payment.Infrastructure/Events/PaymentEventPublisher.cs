using EventBus.Messages.Payment;
using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;

namespace Payment.Infrastructure.Events
{
    public class PaymentEventPublisher : IPaymentEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<PaymentEventPublisher> _logger;

        public PaymentEventPublisher(
            IPublishEndpoint publishEndpoint,
            ILogger<PaymentEventPublisher> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task PublishPaymentCompletedAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            decimal amount,
            string currency,
            string providerTransactionId,
            string paymentProvider,
            DateTime completedAt,
            CancellationToken cancellationToken = default)
        {
            var @event = new PaymentCompletedEvent
            {
                PaymentId = paymentId,
                OrderId = orderId,
                OrderNumber = orderNumber,
                BuyerId = buyerId,
                Amount = amount,
                Currency = currency,
                ProviderTransactionId = providerTransactionId,
                PaymentProvider = paymentProvider,
                CompletedAt = completedAt
            };

            _logger.LogInformation(
                "Publishing PaymentCompletedEvent for Payment: {PaymentId}, Order: {OrderNumber}",
                paymentId,
                orderNumber
            );

            await _publishEndpoint.Publish(@event, cancellationToken);
        }

        public async Task PublishPaymentFailedAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            string failureReason,
            string paymentProvider,
            DateTime failedAt,
            CancellationToken cancellationToken = default)
        {
            var @event = new PaymentFailedEvent
            {
                PaymentId = paymentId,
                OrderId = orderId,
                OrderNumber = orderNumber,
                BuyerId = buyerId,
                FailureReason = failureReason,
                PaymentProvider = paymentProvider,
                FailedAt = failedAt
            };

            _logger.LogWarning(
                "Publishing PaymentFailedEvent for Payment: {PaymentId}, Order: {OrderNumber}, Reason: {Reason}",
                paymentId,
                orderNumber,
                failureReason
            );

            await _publishEndpoint.Publish(@event, cancellationToken);
        }

        public async Task PublishPaymentCancelledAsync(
            Guid paymentId,
            int orderId,
            string orderNumber,
            Guid buyerId,
            string cancellationReason,
            string paymentProvider,
            DateTime cancelledAt,
            CancellationToken cancellationToken = default)
        {
            var @event = new PaymentCancelledEvent
            {
                PaymentId = paymentId,
                OrderId = orderId,
                OrderNumber = orderNumber,
                BuyerId = buyerId,
                CancellationReason = cancellationReason,
                PaymentProvider = paymentProvider,
                CancelledAt = cancelledAt
            };

            _logger.LogInformation(
                "Publishing PaymentCancelledEvent for Payment: {PaymentId}, Order: {OrderNumber}",
                paymentId,
                orderNumber
            );

            await _publishEndpoint.Publish(@event, cancellationToken);
        }
    }
}
