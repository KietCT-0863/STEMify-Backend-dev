using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Domain.Entities;
using Order.Domain.Enums;

namespace Order.Application.Commands.Orders.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderUnitOfWork unitOfWork,

            ILogger<CreateOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;

            _logger = logger;
        }

        public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating order for buyer {BuyerId}", request.BuyerId);

            // Calculate total amount
            var totalAmount = request.SubTotal + request.DeliveryFee - request.DiscountAmount;

            // Generate order number
            var orderNumber = GenerateOrderNumber();

            // Create order entity
            var order = new Domain.Entities.Order
            {
                BuyerId = request.BuyerId,
                OrderNumber = orderNumber,
                Status = OrderStatus.PaymentPending,
                SubTotal = request.SubTotal,
                DeliveryFee = request.DeliveryFee,
                DiscountAmount = request.DiscountAmount,
                Notes = request.Notes,
                Amount = totalAmount,
                OrderItems = request.OrderItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Sku = item.Sku,
                    ProductDescription = item.ProductDescription,
                    ProductImageUrl = item.ProductImageUrl,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    Quantity = item.Quantity,
                    Amount = item.Amount
                }).ToList(),
                OrderHistories = new List<OrderHistory>
                {
                    new OrderHistory
                    {
                        OldStatus = OrderStatus.Pending,
                        NewStatus = OrderStatus.PaymentPending,
                        ChangedById = request.BuyerId.ToString(),
                        ChangedByRole = "Buyer",
                        ChangedAt = DateTime.UtcNow,
                        Notes = "Order created, waiting for payment"
                    }
                }
            };

            // Save order to database
            var createdOrder = await _unitOfWork.Orders.AddAsync(order);
            _logger.LogInformation("Order {OrderId} created with number {OrderNumber}", createdOrder.Id, createdOrder.OrderNumber);

            try
            {
                //TODO
                // Create payment in Payment service
                // var paymentId = await _paymentIntegrationService.CreatePaymentForOrderAsync(
                //     createdOrder.Id,
                //     totalAmount,
                //     createdOrder.OrderNumber,
                //     request.BuyerId);

                // // Update order with payment ID
                // createdOrder.PaymentId = paymentId;
                // await _orderRepository.UpdateAsync(createdOrder);

                // // Get payment details to get payment URL
                // var paymentDetails = await _paymentIntegrationService.GetPaymentDetailsAsync(createdOrder.Id);

                // _logger.LogInformation("Payment {PaymentId} created for order {OrderId}", paymentId, createdOrder.Id);
                return new CreateOrderResponse(
                                    createdOrder.Id,
                                    createdOrder.OrderNumber,
                                    null,
                                    null,
                                    createdOrder.Status
                                );
                // return new CreateOrderResponse(
                //     createdOrder.Id,
                //     createdOrder.OrderNumber,
                //     paymentId,
                //     paymentDetails?.PaymentUrl,
                //     createdOrder.Status
                // );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment for order {OrderId}", createdOrder.Id);

                // Update order status to indicate payment creation failed
                createdOrder.Status = OrderStatus.PaymentFailed;
                await _unitOfWork.Orders.UpdateAsync(createdOrder);

                return new CreateOrderResponse(
                    createdOrder.Id,
                    createdOrder.OrderNumber,
                    null,
                    null,
                    createdOrder.Status
                );
            }
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }
}
