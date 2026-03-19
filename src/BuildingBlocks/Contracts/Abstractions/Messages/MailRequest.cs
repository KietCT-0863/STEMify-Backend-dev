using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Contracts.Abstractions.Messages
{
    /// <summary>
    /// Request model for sending emails
    /// </summary>
    public class MailRequest
    {
        /// <summary>
        /// Sender email address (optional, uses configured default if not provided)
        /// </summary>
        [EmailAddress]
        public string? From { get; set; }

        /// <summary>
        /// Primary recipient email address
        /// </summary>
        [EmailAddress]
        public string ToAddress { get; set; } = string.Empty;

        /// <summary>
        /// Additional recipient email addresses
        /// </summary>
        public IEnumerable<string> ToAddresses { get; set; } = new List<string>();

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email body content
        /// </summary>
        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// File attachments (optional)
        /// </summary>
        public IFormFileCollection? Attachments { get; set; }

        /// <summary>
        /// Whether the body contains HTML
        /// </summary>
        public bool IsHtml { get; set; } = true;

        /// <summary>
        /// Email priority level
        /// </summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    }

    /// <summary>
    /// Email priority levels
    /// </summary>
    public enum EmailPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }
}
