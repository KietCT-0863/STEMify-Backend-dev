using Common.Logging.Metrics;
using Contracts.Abstractions.Services;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Identity.Application.Services;

public class InvitationEmailService : IInvitationEmailService
{
    private readonly Contracts.Abstractions.Services.IEmailService _emailService;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvitationEmailService> _logger;

    public InvitationEmailService(
        Contracts.Abstractions.Services.IEmailService emailService,
        IOrderLicenseService orderLicenseService,
        IConfiguration configuration,
        ILogger<InvitationEmailService> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _orderLicenseService = orderLicenseService ?? throw new ArgumentNullException(nameof(orderLicenseService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private const string InvitationEmailTemplate = @"
<!doctype html>
<html lang=""vi"">
  <head>
    <meta charset=""UTF-8"" />
    <title>Lời mời tham gia</title>
  </head>
  <body style=""margin: 0; padding: 0; background-color: #f5f7fb; font-family: Arial, Helvetica, sans-serif"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding: 40px 0"">
      <tr>
        <td align=""center"">
          <!-- Card -->
          <table
            width=""600""
            cellpadding=""0""
            cellspacing=""0""
            style=""background: #ffffff; border-radius: 8px; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); padding: 40px""
          >
            <!-- Header -->
            <tr>
              <td align=""center"" style=""padding-bottom: 24px"">
                <h1 style=""margin: 0; font-size: 24px; color: #1f2937"">
                  Bạn được mời tham gia <br />
                  <span style=""color: #2563eb"">{OrganizationName}</span>
                </h1>
              </td>
            </tr>

            <!-- Body -->
            <tr>
              <td style=""font-size: 14px; color: #4b5563; line-height: 1.6; padding-bottom: 16px"">
                <p style=""margin: 0 0 12px 0"">Xin chào <strong>{FirstName}</strong>,</p>

                <p style=""margin: 0 0 12px 0"">
                  Bạn đã được mời tham gia <strong>{OrganizationName}</strong> với vai trò
                  <strong>{TargetRole}</strong>.
                </p>

                <p style=""margin: 0"">
                  Vui lòng nhấn vào nút bên dưới để chấp nhận lời mời và thiết lập tài khoản của bạn.
                </p>
              </td>
            </tr>

            <!-- CTA Button -->
            <tr>
              <td align=""center"" style=""padding: 24px 0"">
                <a
                  href=""{InviteUrl}""
                  style=""
                    background: #3b82f6;
                    color: #ffffff;
                    text-decoration: none;
                    padding: 14px 28px;
                    border-radius: 6px;
                    font-size: 14px;
                    font-weight: bold;
                    display: inline-block;
                  ""
                >
                  Chấp nhận lời mời
                </a>
              </td>
            </tr>

            <!-- Expiration -->
            <tr>
              <td style=""font-size: 13px; color: #6b7280; line-height: 1.6; padding-bottom: 24px"">
                <p style=""margin: 0"">
                  Lời mời này sẽ hết hạn vào ngày
                  <strong>{ExpiresAt}</strong>.
                </p>
              </td>
            </tr>

            <!-- Footer Note -->
            <tr>
              <td
                style=""
                  font-size: 12px;
                  color: #9ca3af;
                  line-height: 1.6;
                  border-top: 1px solid #e5e7eb;
                  padding-top: 16px;
                ""
              >
                <p style=""margin: 0 0 8px 0"">
                  Nếu bạn không mong đợi lời mời này hoặc không nhận ra nội dung trên, bạn có thể bỏ qua email này. Địa
                  chỉ email của bạn sẽ được tự động xóa khỏi hệ thống của chúng tôi sau một vài ngày.
                </p>

                <p style=""margin: 0"">
                  Trân trọng,<br />
                  <strong>Đội ngũ STEMify</strong>
                </p>
              </td>
            </tr>
          </table>

          <!-- Footer -->
          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 16px"">
            <tr>
              <td align=""center"" style=""font-size: 11px; color: #9ca3af"">© {CurrentYear} STEMify.</td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

    public async Task SendInvitationEmailAsync(
        Invitation invitation,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        if (invitation == null)
            throw new ArgumentNullException(nameof(invitation));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Get organization info
            var organization = await _orderLicenseService.GetOrganizationAsync(
                organizationId,
                cancellationToken);

            var organizationName = string.IsNullOrWhiteSpace(organization.Name)
                ? $"Organization {organizationId}"
                : organization.Name;

            // Read Backend URL from configuration (for OAuth flow)
            var backendUrl = _configuration["BackendUrl"]
                ?? throw new InvalidOperationException("BackendUrl is not configured in appsettings.json");
            var inviteUrl = $"{backendUrl.TrimEnd('/')}/api/invitations/accept-oauth?token={invitation.Token.Value}";

            // Prepare template placeholders
            var firstName = string.IsNullOrWhiteSpace(invitation.FirstName)
                ? "bạn"
                : invitation.FirstName;

            var emailBody = InvitationEmailTemplate
                .Replace("{OrganizationName}", organizationName, StringComparison.Ordinal)
                .Replace("{FirstName}", firstName, StringComparison.Ordinal)
                .Replace("{TargetRole}", invitation.TargetRole.ToString(), StringComparison.Ordinal)
                .Replace("{InviteUrl}", inviteUrl, StringComparison.Ordinal)
                .Replace("{ExpiresAt}", invitation.ExpiresAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{CurrentYear}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            
            await _emailService.SendEmailAsync(
                to: invitation.InviteeEmail.Value,
                subject: $"Invitation to join {organizationName}",
                body: emailBody,
                isHtml: true);

            stopwatch.Stop();
            IdentityMetrics.RecordEmailVerification("sent");

            _logger.LogDebug(
                "Invitation email sent successfully to {Email} for invitation {InvitationId}",
                invitation.InviteeEmail.Value,
                invitation.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            IdentityMetrics.RecordEmailVerification("failed");

            _logger.LogError(ex,
                "Failed to send invitation email to {Email} for invitation {InvitationId}",
                invitation.InviteeEmail.Value,
                invitation.Id);
            throw; 
        }
    }
}

