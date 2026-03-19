using EventBus.Messages.Payment;
using MassTransit;
using MediatR;
using Payment.Application.Commands.CreatePayment;
using Payment.Domain.Enums;

namespace Payment.API.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(
            IMediator mediator,
            ILogger<OrderCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            try
            {
                _logger.LogInformation(
                    "Received OrderCreatedEvent for Order: {OrderId}, OrderNumber: {OrderNumber}",
                    context.Message.OrderId,
                    context.Message.OrderNumber
                );

                var createPaymentCommand = new CreatePaymentCommand
                {
                    OrderId = context.Message.OrderId,
                    BuyerId = context.Message.BuyerId,
                    OrderNumber = context.Message.OrderNumber,
                    Amount = context.Message.TotalAmount,
                    Currency = context.Message.Currency,
                    Provider = PaymentProvider.PayOS, // Default to PayOS for now
                    ReturnUrl = context.Message.ReturnUrl,
                    CancelUrl = context.Message.CancelUrl
                };

                var result = await _mediator.Send(createPaymentCommand);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Payment created successfully for Order: {OrderNumber}, PaymentId: {PaymentId}",
                        context.Message.OrderNumber,
                        result.PaymentId
                    );

                    // TODO: Publish PaymentCreatedEvent back to notify OrderService
                    // await context.Publish(new PaymentCreatedEvent { ... });
                }
                else
                {
                    _logger.LogError(
                        "Failed to create payment for Order: {OrderNumber}, Error: {Error}",
                        context.Message.OrderNumber,
                        result.ErrorMessage
                    );

                    // Publish PaymentFailedEvent to trigger compensating transaction
                    await context.Publish(new PaymentFailedEvent
                    {
                        OrderId = context.Message.OrderId,
                        OrderNumber = context.Message.OrderNumber,
                        BuyerId = context.Message.BuyerId,
                        FailureReason = result.ErrorMessage ?? "Unknown error",
                        PaymentProvider = PaymentProvider.PayOS.ToString(),
                        FailedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreatedEvent for Order: {OrderNumber}", context.Message.OrderNumber);

                // Publish PaymentFailedEvent to trigger compensating transaction
                await context.Publish(new PaymentFailedEvent
                {
                    OrderId = context.Message.OrderId,
                    OrderNumber = context.Message.OrderNumber,
                    BuyerId = context.Message.BuyerId,
                    FailureReason = ex.Message,
                    PaymentProvider = PaymentProvider.PayOS.ToString(),
                    FailedAt = DateTime.UtcNow
                });

                throw; // Rethrow to let MassTransit handle retry/error queue
            }
        }
    }
}
