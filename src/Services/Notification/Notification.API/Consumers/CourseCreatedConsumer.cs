using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Notification.Application.Commands;

namespace Notification.API.Consumers
{
    public class CourseCreatedConsumer : IConsumer<CourseCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseCreatedConsumer> _logger;

        public CourseCreatedConsumer(IMediator mediator, ILogger<CourseCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CourseCreatedEvent> context)
        {
            _logger.LogInformation(
                "StudentCreatedConsumer: Consuming CourseCreatedEvent for student with ID {StudentId}",
                context.Message.Id
            );

            var request = context.Message;
            var command = new CreateNotificationCommand
            {
                Title = request.Title,
                UserId = request.CreatedByUserId,
                Message = $"A new course '{request.Title}' has been created.",
            };

            await _mediator.Send(command);
        }
    }
}
