using Payment.Domain.Enums;

namespace Payment.Application.Common.Interfaces
{
    public interface IPaymentGateway
    {
        PaymentProvider Provider { get; }

        Task<PaymentGatewayResult> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

        Task<PaymentGatewayResult> GetPaymentStatusAsync(string providerTransactionId, CancellationToken cancellationToken = default);

        Task<RefundResult> RefundPaymentAsync(RefundRequest request, CancellationToken cancellationToken = default);

        Task<bool> VerifyWebhookSignatureAsync(string signature, string payload);
    }

    public record CreatePaymentRequest(
        string OrderNumber,
        decimal Amount,
        string Currency,
        string ReturnUrl,
        string CancelUrl,
        Dictionary<string, string>? Metadata = null
    );

    public record PaymentGatewayResult(
        bool Success,
        string? TransactionId,
        string? PaymentUrl,
        PaymentStatus Status,
        string? ErrorMessage = null,
        DateTime? ExpiresAt = null
    );

    public record RefundRequest(
        string ProviderTransactionId,
        decimal Amount,
        string Currency,
        string Reason
    );

    public record RefundResult(
        bool Success,
        string? RefundId,
        RefundStatus Status,
        string? ErrorMessage = null
    );
}
