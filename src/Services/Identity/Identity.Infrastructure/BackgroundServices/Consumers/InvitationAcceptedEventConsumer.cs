using Contracts.Abstractions.Services;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Consumer that handles invitation accepted events
/// Sends welcome emails and notifications
/// </summary>
public class InvitationAcceptedEventConsumer : IConsumer<InvitationAcceptedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly ILogger<InvitationAcceptedEventConsumer> _logger;

    private const string WelcomeEmailTemplate = @"
<!doctype html>
<html lang=""vi"">
  <head>
    <meta charset=""UTF-8"" />
    <title>Chào mừng bạn tham gia tổ chức</title>
  </head>
  <body style=""margin: 0; padding: 0; background-color: #f5f7fb; font-family: Arial, Helvetica, sans-serif"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding: 40px 0"">
      <tr>
        <td align=""center"">
          <table
            width=""600""
            cellpadding=""0""
            cellspacing=""0""
            style=""background: #ffffff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); padding: 40px""
          >
            <tr>
              <td style=""font-size: 14px; color: #4b5563; line-height: 1.6"">
                <h1 style=""margin: 0 0 16px 0; font-size: 24px; color: #1f2937"">
                  Chào mừng bạn đến với STEMify!
                </h1>

                <p style=""margin: 0 0 12px 0"">
                  Xin chào <strong>{UserEmail}</strong>,
                </p>

                <p style=""margin: 0 0 12px 0"">
                  Lời mời của bạn đã được chấp nhận thành công. Bạn hiện là thành viên của tổ chức với vai trò:
                  <strong>{TargetRole}</strong>.
                </p>

                <p style=""margin: 0 0 12px 0"">
                  Bạn có thể truy cập vào bảng điều khiển của mình và bắt đầu sử dụng nền tảng ngay bây giờ.
                </p>

                <p style=""margin: 0"">
                  Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với quản trị viên của tổ chức.
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

    private const string AdminNotificationEmailTemplate = @"
<!doctype html>
<html lang=""vi"">
  <head>
    <meta charset=""UTF-8"" />
    <title>Thành viên mới tham gia tổ chức</title>
  </head>
  <body style=""margin: 0; padding: 0; background-color: #f5f7fb; font-family: Arial, Helvetica, sans-serif"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding: 40px 0"">
      <tr>
        <td align=""center"">
          <table
            width=""600""
            cellpadding=""0""
            cellspacing=""0""
            style=""background: #ffffff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); padding: 40px""
          >
            <tr>
              <td style=""font-size: 14px; color: #4b5563; line-height: 1.6"">
                <h1 style=""margin: 0 0 16px 0; font-size: 22px; color: #1f2937"">
                  Thành viên mới đã tham gia tổ chức
                </h1>

                <p style=""margin: 0 0 12px 0"">
                  Người dùng <strong>{UserEmail}</strong> đã tham gia tổ chức của bạn.
                </p>

                <table cellpadding=""0"" cellspacing=""0"" style=""font-size: 13px; color: #4b5563; margin: 8px 0 16px 0"">
                  <tr>
                    <td style=""padding: 2px 0; width: 140px; font-weight: bold"">ID tổ chức:</td>
                    <td style=""padding: 2px 0"">{OrganizationId}</td>
                  </tr>
                  <tr>
                    <td style=""padding: 2px 0; width: 140px; font-weight: bold"">Vai trò:</td>
                    <td style=""padding: 2px 0"">{TargetRole}</td>
                  </tr>
                  <tr>
                    <td style=""padding: 2px 0; width: 140px; font-weight: bold"">ID lời mời:</td>
                    <td style=""padding: 2px 0"">{InvitationId}</td>
                  </tr>
                </table>

                <p style=""margin: 0"">
                  Bạn có thể quản lý thành viên trong trang quản trị tổ chức.
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

    public InvitationAcceptedEventConsumer(
        IEmailService emailService,
        IOrganizationUserRepository organizationUserRepository,
        ILogger<InvitationAcceptedEventConsumer> logger)
    {
        _emailService = emailService;
        _organizationUserRepository = organizationUserRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvitationAcceptedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing InvitationAcceptedEvent: Invitation {InvitationId} accepted by user {UserId}",
            @event.InvitationId,
            @event.UserId);

        try
        {
            await SendWelcomeEmailAsync(@event);

           _logger.LogInformation(
                "User {UserId} successfully joined organization {OrganizationId} with role {Role}",
                @event.UserId,
                @event.OrganizationId,
                @event.TargetRole);

            await NotifyOrganizationAdminsAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while processing InvitationAcceptedEvent for Invitation {InvitationId}, User {UserId}",
                @event.InvitationId,
                @event.UserId);
        }
    }

    private async Task NotifyOrganizationAdminsAsync(InvitationAcceptedEvent @event)
    {
        try
        {
            var admins = await _organizationUserRepository.GetOrganizationAdminsAsync(@event.OrganizationId);
            if (admins == null || admins.Count == 0)
            {
                _logger.LogInformation("No organization admins found for organization {OrganizationId}", @event.OrganizationId);
                return;
            }

            var subject = $"New member joined organization {@event.OrganizationId}";
            var body = AdminNotificationEmailTemplate
                .Replace("{UserEmail}", @event.UserEmail, StringComparison.Ordinal)
                .Replace("{OrganizationId}", @event.OrganizationId.ToString(), StringComparison.Ordinal)
                .Replace("{TargetRole}", @event.TargetRole.ToString(), StringComparison.Ordinal)
                .Replace("{InvitationId}", @event.InvitationId.ToString(), StringComparison.Ordinal);

            foreach (var admin in admins)
            {
                if (string.IsNullOrWhiteSpace(admin.User?.Email)) continue;
                await _emailService.SendEmailAsync(admin.User.Email, subject, body, isHtml: true);
            }

            _logger.LogInformation(
                "Notified {Count} admins about new member {UserEmail} in organization {OrganizationId}",
                admins.Count, @event.UserEmail, @event.OrganizationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to notify admins for organization {OrganizationId} about new member {UserEmail}",
                @event.OrganizationId, @event.UserEmail);
        }
    }

    private async Task SendWelcomeEmailAsync(InvitationAcceptedEvent @event)
    {
        try
        {
            var emailBody = WelcomeEmailTemplate
                .Replace("{UserEmail}", @event.UserEmail, StringComparison.Ordinal)
                .Replace("{TargetRole}", @event.TargetRole.ToString(), StringComparison.Ordinal);

            await _emailService.SendEmailAsync(
                to: @event.UserEmail,
                subject: "Welcome! Your invitation has been accepted",
                body: emailBody,
                isHtml: true);

            _logger.LogInformation(
                "Welcome email sent to {Email} for invitation {InvitationId}",
                @event.UserEmail,
                @event.InvitationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send welcome email to {Email}",
                @event.UserEmail);
        }
    }
}
