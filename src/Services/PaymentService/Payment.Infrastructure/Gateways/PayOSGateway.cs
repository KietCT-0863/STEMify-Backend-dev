using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Payment.Application.Common.Interfaces;
using Payment.Domain.Enums;
using Payment.Infrastructure.Gateways.Settings;
using System.Text.Json;

namespace Payment.Infrastructure.Gateways
{
    public class PayOSGateway : IPaymentGateway
    {
        private readonly PayOS _payOS;
        private readonly PayOSSettings _settings;
        private readonly ILogger<PayOSGateway> _logger;

        public PaymentProvider Provider => PaymentProvider.PayOS;

        public PayOSGateway(
            IOptions<PayOSSettings> settings,
            ILogger<PayOSGateway> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Initialize PayOS client
            _payOS = new PayOS(
                _settings.ClientId,
                _settings.ApiKey,
                _settings.ChecksumKey
            );
        }

        public async Task<PaymentGatewayResult> CreatePaymentAsync(
            CreatePaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating PayOS payment for order: {OrderNumber}", request.OrderNumber);

                // Calculate expiration time (15 minutes from now)
                var expiresAt = DateTime.UtcNow.AddMinutes(15);
                var expiredAtUnix = ((DateTimeOffset)expiresAt).ToUnixTimeSeconds();

                // Create PayOS payment data
                var paymentData = new PaymentData(
                    orderCode: GenerateOrderCode(),
                    amount: (int)request.Amount, // PayOS requires integer amount
                    description: $"{request.OrderNumber}",
                    items: new List<ItemData>
                    {
                        new ItemData(request.OrderNumber, 1, (int)request.Amount)
                    },
                    cancelUrl: request.CancelUrl,
                    returnUrl: request.ReturnUrl,
                    expiredAt: (int)expiredAtUnix
                );

                // Call PayOS API to create payment link
                var createPaymentResult = await _payOS.createPaymentLink(paymentData);

                _logger.LogInformation(
                    "PayOS payment created successfully. CheckoutUrl: {CheckoutUrl}",
                    createPaymentResult.checkoutUrl
                );

                return new PaymentGatewayResult(
                    Success: true,
                    TransactionId: createPaymentResult.orderCode.ToString(),
                    PaymentUrl: createPaymentResult.checkoutUrl,
                    Status: PaymentStatus.Pending,
                    ErrorMessage: null,
                    ExpiresAt: expiresAt
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment for order: {OrderNumber}", request.OrderNumber);
                return new PaymentGatewayResult(
                    Success: false,
                    TransactionId: null,
                    PaymentUrl: null,
                    Status: PaymentStatus.Failed,
                    ErrorMessage: ex.Message
                );
            }
        }

        public async Task<PaymentGatewayResult> GetPaymentStatusAsync(
            string providerTransactionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting PayOS payment status for transaction: {TransactionId}", providerTransactionId);

                if (!long.TryParse(providerTransactionId, out var orderCode))
                {
                    return new PaymentGatewayResult(
                        Success: false,
                        TransactionId: providerTransactionId,
                        PaymentUrl: null,
                        Status: PaymentStatus.Failed,
                        ErrorMessage: "Invalid transaction ID format"
                    );
                }

                var paymentInfo = await _payOS.getPaymentLinkInformation(orderCode);

                var status = MapPayOSStatusToPaymentStatus(paymentInfo.status);

                return new PaymentGatewayResult(
                    Success: true,
                    TransactionId: providerTransactionId,
                    PaymentUrl: null,
                    Status: status,
                    ErrorMessage: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayOS payment status for transaction: {TransactionId}", providerTransactionId);
                return new PaymentGatewayResult(
                    Success: false,
                    TransactionId: providerTransactionId,
                    PaymentUrl: null,
                    Status: PaymentStatus.Failed,
                    ErrorMessage: ex.Message
                );
            }
        }

        public async Task<RefundResult> RefundPaymentAsync(
            RefundRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing PayOS refund for transaction: {TransactionId}", request.ProviderTransactionId);

                // PayOS refund API (if supported in SDK version)
                // Note: Check PayOS SDK documentation for refund support
                // For now, return not implemented

                _logger.LogWarning("PayOS refund not yet implemented");

                return new RefundResult(
                    Success: false,
                    RefundId: null,
                    Status: RefundStatus.Failed,
                    ErrorMessage: "Refund functionality not yet implemented for PayOS"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS refund for transaction: {TransactionId}", request.ProviderTransactionId);
                return new RefundResult(
                    Success: false,
                    RefundId: null,
                    Status: RefundStatus.Failed,
                    ErrorMessage: ex.Message
                );
            }
        }

        public Task<bool> VerifyWebhookSignatureAsync(string signature, string payload)
        {
            try
            {
                // Parse payload and extract the `data` object which contains the transaction fields
                using var webhookJson = JsonDocument.Parse(payload);

                if (!webhookJson.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("PayOS webhook missing or invalid 'data' object");
                    return Task.FromResult(false);
                }

                if (string.IsNullOrWhiteSpace(signature))
                {
                    _logger.LogWarning("PayOS webhook missing signature header");
                    return Task.FromResult(false);
                }

                // Build the canonical string: sorted key=value pairs joined by '&'
                var keyValuePairs = new List<(string Key, string Value)>();

                foreach (var property in dataElement.EnumerateObject())
                {
                    var key = property.Name;
                    var value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => string.Empty,
                        _ => property.Value.GetRawText()
                    };

                    keyValuePairs.Add((key, value));
                }

                keyValuePairs.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < keyValuePairs.Count; i++)
                {
                    builder.Append(keyValuePairs[i].Key);
                    builder.Append('=');
                    builder.Append(keyValuePairs[i].Value);
                    if (i < keyValuePairs.Count - 1)
                    {
                        builder.Append('&');
                    }
                }

                var canonical = builder.ToString();

                // Compute HMAC-SHA256 hex using configured ChecksumKey
                var expectedSignature = ComputeHmacSha256Hex(_settings.ChecksumKey, canonical);

                var matches = string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);

                if (!matches)
                {
                    _logger.LogWarning("PayOS webhook signature mismatch. Expected: {Expected}, Provided: {Provided}", expectedSignature, signature);
                }

                return Task.FromResult(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS webhook signature verification failed: {Message}", ex.Message);
                return Task.FromResult(false);
            }
        }

        private static string ComputeHmacSha256Hex(string secret, string data)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Helper methods

        private static long GenerateOrderCode()
        {
            // Generate a unique order code (timestamp-based)
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static PaymentStatus MapPayOSStatusToPaymentStatus(string payOSStatus)
        {
            return payOSStatus?.ToUpper() switch
            {
                "PENDING" => PaymentStatus.Pending,
                "PROCESSING" => PaymentStatus.Processing,
                "PAID" => PaymentStatus.Completed,
                "CANCELLED" => PaymentStatus.Cancelled,
                "EXPIRED" => PaymentStatus.Expired,
                _ => PaymentStatus.Failed
            };
        }
    }
}
