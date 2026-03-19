using MediatR;

namespace Payment.Application.Commands.ConfirmPayment
{
    public record ConfirmPaymentCommand : IRequest<ConfirmPaymentResult>
    {
        public Guid PaymentId { get; init; }
        public string ProviderTransactionId { get; init; } = string.Empty;
        public string? GatewayResponseCode { get; init; }
        public string? RawResponse { get; init; }
    }

    public record ConfirmPaymentResult(
        bool Success,
        string? ErrorMessage = null
    );
}
