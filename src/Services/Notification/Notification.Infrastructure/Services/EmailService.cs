using System.Net;
using System.Net.Mail;
using Contracts.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Common.Configurations;
using Notification.Application.Common.Interfaces.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notification.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly SendGridSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SendGridSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var toEmail = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(
                from,
                toEmail,
                subject,
                plainTextContent: null,
                htmlContent: htmlBody
            );

            try
            {
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Sent email to {Email} with subject {Subject}",
                        to,
                        subject
                    );
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to send email to {Email} with subject {Subject}. Status: {StatusCode}, Body: {ErrorBody}",
                        to,
                        subject,
                        response.StatusCode,
                        errorBody
                    );
                    throw new Exception($"SendGrid failed: {response.StatusCode}, {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception while sending email to {Email} with subject {Subject}",
                    to,
                    subject
                );
                throw;
            }
        }
    }
}
