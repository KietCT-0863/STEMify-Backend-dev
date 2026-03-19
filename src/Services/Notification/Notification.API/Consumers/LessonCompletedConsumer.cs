using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Notification.Application.Commands;
using Notification.Application.Common.Hubs;
using Notification.Application.Common.Interfaces.Services;
using Notification.Infrastructure.Helpers;

namespace Notification.API.Consumers
{
    public class LessonCompletedConsumer : IConsumer<LessonCompletedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LessonCompletedConsumer> _logger;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public LessonCompletedConsumer(
            IMediator mediator,
            ILogger<LessonCompletedConsumer> logger,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext
        )
        {
            _mediator = mediator;
            _logger = logger;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<LessonCompletedEvent> context)
        {
            _logger.LogInformation(
                "Lesson Completed Consumer: Consuming lesson completed with lesson progress ID {Id}",
                context.Message.Id
            );

            var request = context.Message;
            var command = new CreateNotificationCommand
            {
                Title = "Lesson Completed!",
                UserId = request.StudentId,
                Message =
                    $"You've just completed the lesson **{request.LessonName}**! \n"
                    + $"Keep up the great work and continue your learning journey.",
                ClickUrl = $"/resource/lesson/{request.LessonId}",
            };

            await _mediator.Send(command);

            // Send notification to SignalR clients
            await _hubContext
                .Clients.User(request.StudentId)
                .SendAsync("ReceiveNotification", command);

            // Send email
            if (!string.IsNullOrEmpty(request.StudentEmail))
            {
                // Subject
                var subject = EmailTemplateHelper.GetCompletionSubject(request.LessonName);

                // HtmlBody
                var htmlBody = EmailTemplateHelper.GetCompletionHtmlBody(
                    request.StudentName,
                    request.LessonName
                );

                try
                {
                    await _emailService.SendEmailAsync(request.StudentEmail, subject, htmlBody);
                    _logger.LogInformation(
                        "LessonCompletedConsumer: Sent congratulation email to {Email}",
                        request.StudentEmail
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "LessonCompletedConsumer: Failed to send email to {Email}",
                        request.StudentEmail
                    );
                }
            }
        }
    }
}
