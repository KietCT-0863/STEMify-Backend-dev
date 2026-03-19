using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;
using System.Text.Json;

namespace Payment.Application.Commands.CreatePayment
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IEnumerable<IPaymentGateway> _paymentGateways;
        private readonly ILogger<CreatePaymentCommandHandler> _logger;

        public CreatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            IEnumerable<IPaymentGateway> paymentGateways,
            ILogger<CreatePaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _paymentGateways = paymentGateways;
            _logger = logger;
        }

        public async Task<CreatePaymentResult> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if payment already exists for this order
                var existingPayment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
                if (existingPayment != null && existingPayment.Status == PaymentStatus.Completed)
                {
                    return new CreatePaymentResult(false, null, null, "Payment already completed for this order");
                }

                // Select payment gateway
                var gateway = _paymentGateways.FirstOrDefault(g => g.Provider == request.Provider);
                if (gateway == null)
                {
                    return new CreatePaymentResult(false, null, null, $"Payment provider {request.Provider} not supported");
                }

                // Create payment entity
                var payment = new Domain.Entities.Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    BuyerId = request.BuyerId,
                    OrderNumber = request.OrderNumber,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Provider = request.Provider,
                    Method = request.Method,
                    ReturnUrl = request.ReturnUrl,
                    CancelUrl = request.CancelUrl,
                    Status = PaymentStatus.Pending,
                    Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
                };

                // Call payment gateway
                var gatewayRequest = new CreatePaymentRequest(
                    request.OrderNumber,
                    request.Amount,
                    request.Currency,
                    request.ReturnUrl,
                    request.CancelUrl,
                    request.Metadata
                );

                var gatewayResult = await gateway.CreatePaymentAsync(gatewayRequest, cancellationToken);

                // Update payment with gateway response
                payment.PaymentUrl = gatewayResult.PaymentUrl;
                payment.ExpiresAt = gatewayResult.ExpiresAt;
                payment.Status = gatewayResult.Status;
                payment.ErrorMessage = gatewayResult.ErrorMessage;

                // Save payment
                await _paymentRepository.AddAsync(payment, cancellationToken);

                // Create transaction record
                if (gatewayResult.TransactionId != null)
                {
                    var transaction = new Domain.Entities.PaymentTransaction
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        ProviderTransactionId = gatewayResult.TransactionId,
                        Amount = request.Amount,
                        Currency = request.Currency,
                        Status = gatewayResult.Status
                    };
                    await _paymentRepository.AddTransactionAsync(transaction, cancellationToken);
                }

                _logger.LogInformation("Payment created successfully. PaymentId: {PaymentId}, OrderId: {OrderId}",
                    payment.Id, request.OrderId);

                return new CreatePaymentResult(
                    gatewayResult.Success,
                    payment.Id,
                    payment.PaymentUrl,
                    gatewayResult.ErrorMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for OrderId: {OrderId}", request.OrderId);
                return new CreatePaymentResult(false, null, null, $"Internal error: {ex.Message}");
            }
        }
    }
}
