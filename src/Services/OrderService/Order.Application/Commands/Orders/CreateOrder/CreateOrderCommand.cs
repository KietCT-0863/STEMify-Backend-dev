using MediatR;
using Order.Domain.Enums;

namespace Order.Application.Commands.Orders.CreateOrder
{
    public record CreateOrderCommand(
        Guid BuyerId,
        decimal SubTotal,
        decimal DeliveryFee,
        decimal DiscountAmount,
        string? Notes,
        ICollection<CreateOrderItemDto> OrderItems
    ) : IRequest<CreateOrderResponse>;

    public record CreateOrderItemDto(
        int ProductId,
        string ProductName,
        string Sku,
        string? ProductDescription,
        string? ProductImageUrl,
        decimal UnitPrice,
        decimal DiscountAmount,
        int Quantity,
        decimal Amount
    );

    public record CreateOrderResponse(
        int OrderId,
        string OrderNumber,
        Guid? PaymentId,
        string? PaymentUrl,
        OrderStatus Status
    );
}
