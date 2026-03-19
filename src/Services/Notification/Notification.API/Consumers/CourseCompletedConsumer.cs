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
    public class CourseCompletedConsumer : IConsumer<CourseCompletedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CourseCompletedConsumer> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;

        public CourseCompletedConsumer(
            IMediator mediator,
            ILogger<CourseCompletedConsumer> logger,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext
        )
        {
            _mediator = mediator;
            _logger = logger;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<CourseCompletedEvent> context)
        {
            _logger.LogInformation(
                "Course Completed Consumer: Consuming course completed with enrollment ID {Id}",
                context.Message.Id
            );

            var request = context.Message;
            var command = new CreateNotificationCommand
            {
                Title = "Hoàn thành khóa học!",
                UserId = request.StudentId,
                Message =
                    $"Chúc mừng bạn! Bạn đã hoàn thành khóa học **{request.CourseTitle}** thành công. \n"
                    + $"Bạn có thể xem chứng chỉ và khám phá những nội dung tiếp theo trong lộ trình học tập của bạn.",
                ClickUrl = $"/resource/course/{request.CourseId}",
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
                var subject = EmailTemplateHelper.GetCompletionSubject(request.CourseTitle);

                // HtmlBody
                var htmlBody = EmailTemplateHelper.GetCompletionHtmlBody(
                    request.StudentName,
                    request.CourseTitle
                );

                try
                {
                    await _emailService.SendEmailAsync(request.StudentEmail, subject, htmlBody);
                    _logger.LogInformation(
                        "CourseCompletedConsumer: Sent congratulation email to {Email}",
                        request.StudentEmail
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "CourseCompletedConsumer: Failed to send email to {Email}",
                        request.StudentEmail
                    );
                }
            }
        }
    }
}
