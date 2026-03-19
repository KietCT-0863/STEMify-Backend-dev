namespace Contracts.Configurations
{
    /// <summary>
    /// Email service configuration settings
    /// </summary>
    public interface IEmailSettings
    {
        /// <summary>
        /// SMTP server hostname
        /// </summary>
        string SMTPServer { get; set; }

        /// <summary>
        /// SMTP server port
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// SMTP username/email
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// SMTP password or app-specific password
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Enable SSL/TLS encryption
        /// </summary>
        string UseSSL { get; set; }

        /// <summary>
        /// Display name for the sender
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Enable email verification functionality
        /// </summary>
        bool EnableVerification { get; set; }

        /// <summary>
        /// Default sender email address
        /// </summary>
        string From { get; set; }

        /// <summary>
        /// Email sending timeout in milliseconds
        /// </summary>
        int TimeoutMs { get; set; }

        /// <summary>
        /// Maximum number of retry attempts for failed emails
        /// </summary>
        int MaxRetryAttempts { get; set; }

        /// <summary>
        /// Enable email service (set to false to disable sending)
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Log email content for debugging (be careful in production)
        /// </summary>
        bool LogEmailContent { get; set; }

        /// <summary>
        /// Default template directory path
        /// </summary>
        string? TemplateDirectory { get; set; }

        /// <summary>
        /// Application name for email templates
        /// </summary>
        string? ApplicationName { get; set; }

        /// <summary>
        /// Support email address for email templates
        /// </summary>
        string? SupportEmail { get; set; }
    }
}
