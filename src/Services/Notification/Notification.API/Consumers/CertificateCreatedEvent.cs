using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Notification.Application.Commands;
using Notification.Application.Common.Hubs;

namespace Notification.API.Consumers
{
    public class CertificateCreatedConsumer : IConsumer<CertificateCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CertificateCreatedConsumer> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CertificateCreatedConsumer(
            IMediator mediator,
            ILogger<CertificateCreatedConsumer> logger,
            IHubContext<NotificationHub> hubContext
        )
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<CertificateCreatedEvent> context)
        {
            _logger.LogInformation(
                "CertificateCreatedConsumer: Received CertificateCreatedEvent for StudentId: {StudentId}, Title: {Title}",
                context.Message.StudentId,
                context.Message.CertificateTitile
            );

            var request = context.Message;
            var command = new CreateNotificationCommand
            {
                Title = "Chứng chỉ mới đã sẵn sàng",
                UserId = request.StudentId,
                Message = $"Chúc mừng {request.Name}! Chứng chỉ '{request.CertificateTitile}' của bạn đã sẵn sàng.",
                ClickUrl = $"vi/certificate",
            };

            await _mediator.Send(command);

            // Send notification to SignalR clients
            await _hubContext
                .Clients.User(request.StudentId)
                .SendAsync("ReceiveNotification", command);
        }
    }
}
