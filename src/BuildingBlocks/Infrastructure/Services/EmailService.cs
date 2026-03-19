using Contracts.Abstractions.Messages;
using Contracts.Abstractions.Services;
using Contracts.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services;

/// <summary>
/// Email service implementation for sending various types of emails
/// </summary>
public class EmailService : IEmailService, IDisposable
{
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailSettings _emailSettings;
    private readonly IEmailTemplateService _templateService;
    private readonly SmtpClient _smtpClient;
    private bool _disposed = false;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<IEmailSettings> emailSettings,
        IEmailTemplateService templateService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

        // Configure SMTP client
        _smtpClient = ConfigureSmtpClient();

        // Load templates from directory if specified
        if (!string.IsNullOrWhiteSpace(_emailSettings.TemplateDirectory))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _templateService.LoadTemplatesAsync(_emailSettings.TemplateDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load email templates from directory");
                }
            });
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email is required", nameof(to));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));

        var mailRequest = new MailRequest
        {
            From = _emailSettings.From,
            ToAddress = to,
            Subject = subject,
            Body = body ?? string.Empty
        };

        await SendEmailAsync(mailRequest);
    }

    public async Task SendEmailAsync(MailRequest mailRequest)
    {
        if (mailRequest == null)
            throw new ArgumentNullException(nameof(mailRequest));

        try
        {
            using var mailMessage = CreateMailMessage(mailRequest);
            await _smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Recipients} with subject: {Subject}",
                GetRecipientsString(mailRequest), mailRequest.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients} with subject: {Subject}",
                GetRecipientsString(mailRequest), mailRequest.Subject);
            throw;
        }
    }

    public async Task SendBulkEmailAsync(IEnumerable<string> toAddresses, string subject, string body, bool isHtml = false)
    {
        if (toAddresses == null || !toAddresses.Any())
            throw new ArgumentException("At least one recipient is required", nameof(toAddresses));

        var tasks = toAddresses.Select(async email =>
        {
            try
            {
                await SendEmailAsync(email, subject, body, isHtml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk email to {Email}", email);
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Bulk email sent to {Count} recipients with subject: {Subject}",
            toAddresses.Count(), subject);
    }

    public async Task SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, object> templateData)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email is required", nameof(to));

        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        if (!_templateService.TemplateExists(templateName))
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));

        // Add default template data
        var enrichedTemplateData = EnrichTemplateData(templateData ?? new Dictionary<string, object>());

        var processedTemplate = await _templateService.ProcessTemplateAsync(templateName, enrichedTemplateData);

        // Extract subject from template (first line should be subject)
        var lines = processedTemplate.Split('\n', 2);
        var subject = lines.Length > 0 ? lines[0].Trim() : $"Email from {_emailSettings.DisplayName}";
        var body = lines.Length > 1 ? lines[1].Trim() : processedTemplate;

        await SendEmailAsync(to, subject, body, true);

        _logger.LogInformation("Templated email '{TemplateName}' sent to {Email}", templateName, to);
    }

    private SmtpClient ConfigureSmtpClient()
    {
        var smtpClient = new SmtpClient
        {
            Host = _emailSettings.SMTPServer,
            Port = _emailSettings.Port,
            EnableSsl = bool.Parse(_emailSettings.UseSSL ?? "true"),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 seconds timeout
        };

        return smtpClient;
    }

    private MailMessage CreateMailMessage(MailRequest mailRequest)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(
                string.IsNullOrWhiteSpace(mailRequest.From) ? _emailSettings.From : mailRequest.From,
                _emailSettings.DisplayName
            ),
            Subject = mailRequest.Subject,
            Body = mailRequest.Body,
            IsBodyHtml = mailRequest.IsHtml,
            Priority = mailRequest.Priority switch
            {
                EmailPriority.Low => MailPriority.Low,
                EmailPriority.High => MailPriority.High,
                _ => MailPriority.Normal
            }
        };

        // Add primary recipient
        if (!string.IsNullOrWhiteSpace(mailRequest.ToAddress))
        {
            mailMessage.To.Add(mailRequest.ToAddress);
        }

        // Add additional recipients
        if (mailRequest.ToAddresses != null && mailRequest.ToAddresses.Any())
        {
            foreach (var email in mailRequest.ToAddresses.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                mailMessage.To.Add(email);
            }
        }

        // Add attachments if any
        if (mailRequest.Attachments != null && mailRequest.Attachments.Any())
        {
            foreach (var file in mailRequest.Attachments)
            {
                if (file.Length > 0)
                {
                    var attachment = new Attachment(file.OpenReadStream(), file.FileName, file.ContentType);
                    mailMessage.Attachments.Add(attachment);
                }
            }
        }

        return mailMessage;
    }

    private Dictionary<string, object> EnrichTemplateData(Dictionary<string, object> templateData)
    {
        var enrichedData = new Dictionary<string, object>(templateData);

        // Add default values if not provided
        if (!enrichedData.ContainsKey("AppName"))
            enrichedData["AppName"] = _emailSettings.ApplicationName ?? "Application";

        if (!enrichedData.ContainsKey("SupportEmail"))
            enrichedData["SupportEmail"] = _emailSettings.SupportEmail ?? _emailSettings.From;

        if (!enrichedData.ContainsKey("FromEmail"))
            enrichedData["FromEmail"] = _emailSettings.From;

        if (!enrichedData.ContainsKey("DisplayName"))
            enrichedData["DisplayName"] = _emailSettings.DisplayName;

        if (!enrichedData.ContainsKey("CurrentYear"))
            enrichedData["CurrentYear"] = DateTime.UtcNow.Year.ToString();

        if (!enrichedData.ContainsKey("CurrentDate"))
            enrichedData["CurrentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (!enrichedData.ContainsKey("ExpirationHours"))
            enrichedData["ExpirationHours"] = "24";

        return enrichedData;
    }

    private string GetRecipientsString(MailRequest mailRequest)
    {
        var recipients = new List<string>();

        if (!string.IsNullOrWhiteSpace(mailRequest.ToAddress))
            recipients.Add(mailRequest.ToAddress);

        if (mailRequest.ToAddresses != null)
            recipients.AddRange(mailRequest.ToAddresses.Where(e => !string.IsNullOrWhiteSpace(e)));

        return string.Join(", ", recipients);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _smtpClient?.Dispose();
            _disposed = true;
        }
    }
}
