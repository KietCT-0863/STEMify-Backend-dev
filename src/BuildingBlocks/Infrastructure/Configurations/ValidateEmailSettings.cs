using Contracts.Configurations;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configurations;

/// <summary>
/// Validation for email settings configuration
/// </summary>
public class ValidateEmailSettings : IValidateOptions<IEmailSettings>
{
    public ValidateOptionsResult Validate(string? name, IEmailSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SMTPServer))
            failures.Add("SMTP Server is required");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add("Username is required");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add("Password is required");

        if (string.IsNullOrWhiteSpace(options.From))
            failures.Add("From address is required");

        if (options.Port <= 0 || options.Port > 65535)
            failures.Add("Port must be between 1 and 65535");

        if (options.TimeoutMs < 1000)
            failures.Add("Timeout must be at least 1000ms");

        if (options.MaxRetryAttempts < 0)
            failures.Add("Max retry attempts cannot be negative");

        // Validate email addresses
        if (!string.IsNullOrWhiteSpace(options.From) && !IsValidEmail(options.From))
            failures.Add("From address is not a valid email");

        if (!string.IsNullOrWhiteSpace(options.Username) && !IsValidEmail(options.Username))
            failures.Add("Username is not a valid email");

        if (!string.IsNullOrWhiteSpace(options.SupportEmail) && !IsValidEmail(options.SupportEmail))
            failures.Add("Support email is not a valid email");

        if (failures.Any())
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
