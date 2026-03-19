using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Commands.ConfirmPayment
{
    public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, ConfirmPaymentResult>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentEventPublisher _eventPublisher;
        private readonly ILogger<ConfirmPaymentCommandHandler> _logger;

        public ConfirmPaymentCommandHandler(
            IPaymentRepository paymentRepository,
            IPaymentEventPublisher eventPublisher,
            ILogger<ConfirmPaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<ConfirmPaymentResult> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
                if (payment == null)
                {
                    return new ConfirmPaymentResult(false, "Payment not found");
                }

                if (payment.Status == PaymentStatus.Completed)
                {
                    _logger.LogWarning("Payment {PaymentId} already completed", request.PaymentId);
                    return new ConfirmPaymentResult(true);
                }

                var completedAt = DateTime.UtcNow;

                // Update payment status
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = completedAt;
                await _paymentRepository.UpdateAsync(payment, cancellationToken);

                // Add transaction record
                var transaction = new Domain.Entities.PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    ProviderTransactionId = request.ProviderTransactionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = PaymentStatus.Completed,
                    GatewayResponseCode = request.GatewayResponseCode,
                    RawResponse = request.RawResponse,
                    ProcessedAt = completedAt
                };
                await _paymentRepository.AddTransactionAsync(transaction, cancellationToken);

                // Publish PaymentCompletedEvent via Outbox pattern
                await _eventPublisher.PublishPaymentCompletedAsync(
                     paymentId: payment.Id,
                     orderId: payment.OrderId,
                     orderNumber: payment.OrderNumber,
                     buyerId: payment.BuyerId,
                     amount: payment.Amount,
                     currency: payment.Currency,
                     providerTransactionId: request.ProviderTransactionId,
                     paymentProvider: payment.Provider.ToString(),
                     completedAt: completedAt,
                     cancellationToken: cancellationToken
                 );

                _logger.LogInformation(
                    "Payment {PaymentId} confirmed successfully for Order {OrderNumber}",
                    request.PaymentId,
                    payment.OrderNumber
                );

                return new ConfirmPaymentResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentId}", request.PaymentId);
                return new ConfirmPaymentResult(false, $"Internal error: {ex.Message}");
            }
        }
    }
}
