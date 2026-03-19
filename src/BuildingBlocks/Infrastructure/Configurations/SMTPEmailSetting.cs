using Contracts.Configurations;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configurations
{
    /// <summary>
    /// SMTP email service configuration implementation
    /// </summary>
    public class SMTPEmailSetting : IEmailSettings
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "EmailSettings";

        [Required]
        public string SMTPServer { get; set; } = "smtp.gmail.com";

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        [Required]
        [EmailAddress]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string UseSSL { get; set; } = "true";

        [Required]
        public string DisplayName { get; set; } = "System Notification";

        public bool EnableVerification { get; set; } = true;

        [Required]
        [EmailAddress]
        public string From { get; set; } = string.Empty;

        [Range(1000, 300000)] // 1 second to 5 minutes
        public int TimeoutMs { get; set; } = 30000; // 30 seconds

        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        public bool Enabled { get; set; } = true;

        public bool LogEmailContent { get; set; } = false;

        public string? TemplateDirectory { get; set; }

        public string? ApplicationName { get; set; } = "Application";

        [EmailAddress]
        public string? SupportEmail { get; set; }

        /// <summary>
        /// Validate the configuration settings
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SMTPServer))
                throw new InvalidOperationException("SMTP Server is required");

            if (string.IsNullOrWhiteSpace(Username))
                throw new InvalidOperationException("Username is required");

            if (string.IsNullOrWhiteSpace(Password))
                throw new InvalidOperationException("Password is required");

            if (string.IsNullOrWhiteSpace(From))
                throw new InvalidOperationException("From address is required");

            if (Port <= 0 || Port > 65535)
                throw new InvalidOperationException("Port must be between 1 and 65535");

            if (TimeoutMs < 1000)
                throw new InvalidOperationException("Timeout must be at least 1000ms");
        }
    }
}
