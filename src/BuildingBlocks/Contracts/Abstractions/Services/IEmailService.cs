using Contracts.Abstractions.Messages;

namespace Contracts.Abstractions.Services
{
    /// <summary>
    /// Interface for email service to send various types of emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send a simple text or HTML email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (can be HTML)</param>
        /// <param name="isHtml">Whether the body contains HTML</param>
        /// <returns>Task representing the operation</returns>
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);

        /// <summary>
        /// Send email using MailRequest model
        /// </summary>
        /// <param name="mailRequest">Mail request containing all email details</param>
        /// <returns>Task representing the operation</returns>
        Task SendEmailAsync(MailRequest mailRequest);

        /// <summary>
        /// Send email to multiple recipients
        /// </summary>
        /// <param name="toAddresses">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="isHtml">Whether the body contains HTML</param>
        /// <returns>Task representing the operation</returns>
        Task SendBulkEmailAsync(IEnumerable<string> toAddresses, string subject, string body, bool isHtml = false);

        /// <summary>
        /// Send templated email with data
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="templateName">Template name</param>
        /// <param name="templateData">Data to populate template</param>
        /// <returns>Task representing the operation</returns>
        Task SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, object> templateData);
    }
}
