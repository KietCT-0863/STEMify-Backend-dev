using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Notification.Application.Commands;
using Notification.Application.Common.Hubs;
using Shared.Enums;

namespace Notification.API.Consumers
{
    public class CourseUpdatedConsumer : IConsumer<CourseUpdatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseCreatedConsumer> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CourseUpdatedConsumer(
            IMediator mediator,
            ILogger<CourseCreatedConsumer> logger,
            IHubContext<NotificationHub> hubContext
        )
        {
            _mediator = mediator;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<CourseUpdatedEvent> context)
        {
            _logger.LogInformation(
                "CourseUpdatedConsumer: Consuming course updated with ID {Id}",
                context.Message.Id
            );

            var request = context.Message;

            string title;
            string message;

            switch (request.Status)
            {
                case "Published":
                    title = "Your course has been approved";
                    message = "Your course is now published and visible to others.";
                    break;
                case "Rejected":
                    title = "Your course has been rejected";
                    message = "Your course did not meet our criteria. Please review and update it.";
                    break;
                default:
                    title = "Your course status has been updated";
                    message = $"Your course status is now: {request.Status}";
                    break;
            }

            var command = new CreateNotificationCommand
            {
                Title = title,
                UserId = request.CreatedByUserId,
                Message = message,
                ClickUrl = $"/resource/course/{request.Id}",
            };
            await _mediator.Send(command);

            // Send notification to SignalR clients
            await _hubContext
                .Clients.User(request.CreatedByUserId)
                .SendAsync("ReceiveNotification", command);
        }
    }
}
