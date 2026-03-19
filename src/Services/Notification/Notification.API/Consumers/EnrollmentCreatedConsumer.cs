using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Notification.Application.Commands;
using Notification.Application.Common.Configurations;
using Notification.Application.Common.Hubs;
using Notification.Application.Common.Interfaces.Services;
using Notification.Infrastructure.Helpers;

namespace Notification.API.Consumers
{
    public class EnrollmentCreatedConsumer : IConsumer<CourseEnrollmentCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<EnrollmentCreatedConsumer> _logger;
        private readonly IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly IHubContext<NotificationHub> _hubContext;

        public EnrollmentCreatedConsumer(
            IMediator mediator,
            ILogger<EnrollmentCreatedConsumer> logger,
            IEmailService emailService,
            IOptions<ClientAppSettings> clientAppSettings,
            IHubContext<NotificationHub> hubContext
        )
        {
            _mediator = mediator;
            _logger = logger;
            _emailService = emailService;
            _clientAppSettings = clientAppSettings.Value;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<CourseEnrollmentCreatedEvent> context)
        {
            _logger.LogInformation(
                "EnrollemtCreatedConsumer: Consuming enrollment created with ID {Id}",
                context.Message.Id
            );

            var request = context.Message;
            var command = new CreateNotificationCommand
            {
                Title = "Bạn đã đăng ký khóa học mới",
                UserId = request.StudentId,
                Message =
                    $"Chào mừng bạn đến với khóa học **{request.CourseTitle}**. \n"
                    + $"Hãy bắt đầu hành trình học tập của bạn ngay bây giờ!",
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
                var subject = EmailTemplateHelper.GetEnrollmentSubject(request.CourseTitle);

                var courseLink = $"{_clientAppSettings.BaseUrl}/resource/course/{request.CourseId}";

                // HtmlBody
                var htmlBody = EmailTemplateHelper.GetEnrollmentHtmlBody(
                    request.StudentName,
                    request.CourseTitle,
                    courseLink
                );

                try
                {
                    await _emailService.SendEmailAsync(request.StudentEmail, subject, htmlBody);
                    _logger.LogInformation(
                        "EnrollmentCreatedConsumer: Sent welcome email to {Email}",
                        request.StudentEmail
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "EnrollmentCreatedConsumer: Failed to send email to {Email}",
                        request.StudentEmail
                    );
                }
            }
            else
            {
                _logger.LogWarning(
                    "EnrollmentCreatedConsumer: Student email is missing for enrollment ID {Id}",
                    request.Id
                );
            }
        }
    }
}
