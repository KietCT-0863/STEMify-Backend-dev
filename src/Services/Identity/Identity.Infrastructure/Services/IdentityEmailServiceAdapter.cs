using Identity.Application.Common.Interfaces.Services;
using Contracts.Abstractions.Services;
using Microsoft.Extensions.Logging;
using IdentityIEmailService = Identity.Application.Common.Interfaces.Services.IEmailService;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Adapter that bridges Identity-specific email interface with shared email service
/// </summary>
public class IdentityEmailServiceAdapter(
    Contracts.Abstractions.Services.IEmailService sharedEmailService,
    IEmailTemplateService templateService,
    ILogger<IdentityEmailServiceAdapter> logger) : IdentityIEmailService
{
    private readonly Contracts.Abstractions.Services.IEmailService _sharedEmailService = sharedEmailService ?? throw new ArgumentNullException(nameof(sharedEmailService));
    private readonly IEmailTemplateService _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
    private readonly ILogger<IdentityEmailServiceAdapter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl)
    {
        try
        {
            var templateData = new Dictionary<string, object>
            {
                ["ConfirmationUrl"] = callbackUrl,
                ["AppName"] = "STEMify Platform",
                ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
            };

            await _sharedEmailService.SendTemplatedEmailAsync(
                email, 
                "email-confirmation-vi", 
                templateData
            );
            
            _logger.LogInformation("Email confirmation sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email confirmation to {Email}. Error: {ErrorMessage}", email, ex.Message);
            
            
            // if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            // {
            //     _logger.LogWarning("EMAIL SENDING FAILED IN DEVELOPMENT MODE - Registration will continue without email verification");
            //     _logger.LogWarning("To fix email issues, check Gmail App Password configuration in appsettings.Development.json");
            //     return; 
            // }
            
            throw; // In production, throw to notify user of email issues
        }
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string callbackUrl)
    {
        try
        {
            var templateData = new Dictionary<string, object>
            {
                ["ResetUrl"] = callbackUrl,
                ["AppName"] = "STEMify Platform",
                ["ExpirationHours"] = "1",
                ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
            };

            await _sharedEmailService.SendTemplatedEmailAsync(
                email, 
                "password-reset-vi", 
                templateData
            );
            
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}. Error: {ErrorMessage}", email, ex.Message);
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                _logger.LogWarning("PASSWORD RESET EMAIL FAILED IN DEVELOPMENT MODE");
                return;
            }
            
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName, string userType)
    {
        try
        {
            var roleIcon = userType.ToLowerInvariant() == "teacher" ? "👩‍🏫" : "👨‍🎓";
            var roleText = userType.ToLowerInvariant() == "teacher" ? "Giáo viên" : "Học sinh";
            
            var templateData = new Dictionary<string, object>
            {
                ["FirstName"] = firstName,
                ["RoleIcon"] = roleIcon,
                ["RoleText"] = roleText,
                ["AppName"] = "STEMify Platform",
                ["DashboardUrl"] = "https://app.stemify.com/dashboard",
                ["SupportEmail"] = "support@stemify.com",
                ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
            };

            await _sharedEmailService.SendTemplatedEmailAsync(
                email, 
                "welcome-vi", 
                templateData
            );
            
            _logger.LogInformation("Welcome email sent to {Email} for {UserType}", email, userType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}. Error: {ErrorMessage}", email, ex.Message);
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                _logger.LogWarning("WELCOME EMAIL FAILED IN DEVELOPMENT MODE");
                return;
            }
            
            throw;
        }
    }

}