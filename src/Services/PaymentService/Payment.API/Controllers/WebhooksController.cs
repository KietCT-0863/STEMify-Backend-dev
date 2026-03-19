using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands.ConfirmPayment;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;
using System.Text.Json;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly ILogger<WebhooksController> _logger;
        private readonly IEnumerable<IPaymentGateway> _paymentGateways;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMediator _mediator;
        private readonly IPaymentEventPublisher _eventPublisher;

        public WebhooksController(
            ILogger<WebhooksController> logger,
            IEnumerable<IPaymentGateway> paymentGateways,
            IPaymentRepository paymentRepository,
            IMediator mediator,
            IPaymentEventPublisher eventPublisher)
        {
            _logger = logger;
            _paymentGateways = paymentGateways;
            _paymentRepository = paymentRepository;
            _mediator = mediator;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// PayOS webhook endpoint
        /// </summary>
        [HttpPost("payos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayOSWebhook()
        {
            try
            {
                _logger.LogInformation("Received PayOS webhook");

                // Read raw body
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();

                _logger.LogDebug("PayOS webhook payload: {Payload}", payload);

                // Get PayOS gateway
                var payOSGateway = _paymentGateways.FirstOrDefault(g => g.Provider == PaymentProvider.PayOS);
                if (payOSGateway == null)
                {
                    _logger.LogError("PayOS gateway not found");
                    return BadRequest(new { error = "PayOS gateway not configured" });
                }

                // Parse webhook data first to get signature from payload
                var webhookData = JsonSerializer.Deserialize<PayOSWebhookData>(payload);
                if (webhookData == null || webhookData.data == null)
                {
                    return BadRequest(new { error = "Invalid payload" });
                }

                var orderCode = webhookData.data.orderCode?.ToString() ?? string.Empty;

                // Verify webhook signature (signature is in payload, not header)
                var signature = webhookData.signature;
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("PayOS webhook signature missing in payload");
                    return BadRequest(new { error = "Signature missing" });
                }

                var isValid = await payOSGateway.VerifyWebhookSignatureAsync(signature, payload);
                if (!isValid)
                {
                    _logger.LogWarning("PayOS webhook signature verification failed");
                    return BadRequest(new { error = "Invalid signature" });
                }

                _logger.LogInformation(
                    "PayOS webhook verified. OrderCode: {OrderCode}, Status: {Status}",
                    orderCode,
                    webhookData.data.status
                );

                // Find payment by provider transaction ID
                var payment = await _paymentRepository.GetByProviderTransactionIdAsync(orderCode, cancellationToken: default);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for OrderCode: {OrderCode}", orderCode);
                    return BadRequest(new { error = "Payment not found" });
                }

                // Process payment based on status
                var statusCode = webhookData.data.code;

                switch (statusCode)
                {
                    case "00": // PayOS success code
                        if (payment.Status != Domain.Enums.PaymentStatus.Completed)
                        {
                            _logger.LogInformation("Confirming payment {PaymentId} for OrderCode: {OrderCode}", payment.Id, orderCode);

                            var confirmResult = await _mediator.Send(new ConfirmPaymentCommand
                            {
                                PaymentId = payment.Id,
                                ProviderTransactionId = orderCode,
                                GatewayResponseCode = webhookData.code,
                                RawResponse = payload
                            });

                            if (!confirmResult.Success)
                            {
                                _logger.LogError("Failed to confirm payment {PaymentId}: {Error}", payment.Id, confirmResult.ErrorMessage);
                                return BadRequest(new { error = confirmResult.ErrorMessage });
                            }
                        }
                        break;

                    case "CANCELLED":
                        if (payment.Status != Domain.Enums.PaymentStatus.Cancelled)
                        {
                            _logger.LogInformation("Payment {PaymentId} was cancelled", payment.Id);

                            // Update payment status
                            payment.Status = Domain.Enums.PaymentStatus.Cancelled;
                            await _paymentRepository.UpdateAsync(payment, cancellationToken: default);

                            // Publish PaymentCancelledEvent for compensating transaction
                            await _eventPublisher.PublishPaymentCancelledAsync(
                                paymentId: payment.Id,
                                orderId: payment.OrderId,
                                orderNumber: payment.OrderNumber,
                                buyerId: payment.BuyerId,
                                cancellationReason: "User cancelled payment",
                                paymentProvider: payment.Provider.ToString(),
                                cancelledAt: DateTime.UtcNow,
                                cancellationToken: default
                            );
                        }
                        break;

                    case "EXPIRED":
                        if (payment.Status != Domain.Enums.PaymentStatus.Expired)
                        {
                            _logger.LogInformation("Payment {PaymentId} has expired", payment.Id);

                            // Update payment status
                            payment.Status = Domain.Enums.PaymentStatus.Expired;
                            await _paymentRepository.UpdateAsync(payment, cancellationToken: default);

                            // Publish PaymentFailedEvent (expired = failed)
                            await _eventPublisher.PublishPaymentFailedAsync(
                                paymentId: payment.Id,
                                orderId: payment.OrderId,
                                orderNumber: payment.OrderNumber,
                                buyerId: payment.BuyerId,
                                failureReason: "Payment link expired",
                                paymentProvider: payment.Provider.ToString(),
                                failedAt: DateTime.UtcNow,
                                cancellationToken: default
                            );
                        }
                        break;

                    default:
                        _logger.LogWarning("Unhandled payment status: {Status} for payment {PaymentId}", statusCode, payment.Id);
                        break;
                }

                return Ok(new { success = true, message = "Webhook processed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Stripe webhook endpoint
        /// </summary>
        [HttpPost("stripe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StripeWebhook([FromBody] object payload)
        {
            _logger.LogInformation("Received Stripe webhook");

            // TODO: Implement Stripe webhook handling
            // 1. Verify webhook signature (use Stripe-Signature header)
            // 2. Parse payload
            // 3. Handle different event types
            // 4. Update payment status
            // 5. Publish events

            await Task.CompletedTask;
            return Ok(new { message = "Webhook received" });
        }

        ///// <summary>
        ///// VNPay webhook endpoint 
        ///// </summary>
        //[HttpPost("vnpay")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<IActionResult> VNPayWebhook([FromQuery] string vnp_TxnRef)
        //{
        //    _logger.LogInformation("Received VNPay webhook for transaction: {TxnRef}", vnp_TxnRef);

        //    // TODO: Implement VNPay webhook handling

        //    await Task.CompletedTask;
        //    return Ok(new { message = "Webhook received" });
        //}
    }
}
