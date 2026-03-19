using MediatR;
using Payment.Domain.Enums;

namespace Payment.Application.Commands.CreatePayment
{
    public record CreatePaymentCommand : IRequest<CreatePaymentResult>
    {
        public int OrderId { get; init; }
        public Guid BuyerId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "VND";
        public PaymentProvider Provider { get; init; }
        public PaymentMethod Method { get; init; }
        public string ReturnUrl { get; init; } = string.Empty;
        public string CancelUrl { get; init; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; init; }
    }

    public record CreatePaymentResult(
        bool Success,
        Guid? PaymentId,
        string? PaymentUrl,
        string? ErrorMessage = null
    );
}
