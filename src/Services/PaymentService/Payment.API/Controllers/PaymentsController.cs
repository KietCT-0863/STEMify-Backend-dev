using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands.CreatePayment;
using Payment.Application.Commands.ConfirmPayment;
using Payment.Application.Common.Interfaces;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IMediator mediator,
            IPaymentRepository paymentRepository,
            ILogger<PaymentsController> logger)
        {
            _mediator = mediator;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        /// <summary>
        /// Create a new payment
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CreatePaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreatePaymentResult>> CreatePayment(
            [FromBody] CreatePaymentCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating payment for OrderId: {OrderId}", command.OrderId);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Domain.Entities.Payment), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Domain.Entities.Payment>> GetPayment(
            Guid id,
            CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(payment);
        }

        /// <summary>
        /// Get payment by order ID
        /// </summary>
        [HttpGet("order/{orderId:int}")]
        [ProducesResponseType(typeof(Domain.Entities.Payment), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Domain.Entities.Payment>> GetPaymentByOrderId(
            int orderId,
            CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

            if (payment == null)
            {
                return NotFound(new { message = "Payment not found for this order" });
            }

            return Ok(payment);
        }

        /// <summary>
        /// Get all payments for a buyer
        /// </summary>
        [HttpGet("buyer/{buyerId:guid}")]
        [ProducesResponseType(typeof(List<Domain.Entities.Payment>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Domain.Entities.Payment>>> GetPaymentsByBuyer(
            Guid buyerId,
            CancellationToken cancellationToken)
        {
            var payments = await _paymentRepository.GetByBuyerIdAsync(buyerId, cancellationToken);
            return Ok(payments);
        }

        /// <summary>
        /// Confirm payment (internal use - called by webhook handler)
        /// </summary>
        [HttpPost("{id:guid}/confirm")]
        [ProducesResponseType(typeof(ConfirmPaymentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ConfirmPaymentResult>> ConfirmPayment(
            Guid id,
            [FromBody] ConfirmPaymentRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ConfirmPaymentCommand
            {
                PaymentId = id,
                ProviderTransactionId = request.ProviderTransactionId,
                GatewayResponseCode = request.GatewayResponseCode,
                RawResponse = request.RawResponse
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }

    public record ConfirmPaymentRequest(
        string ProviderTransactionId,
        string? GatewayResponseCode = null,
        string? RawResponse = null
    );
}
