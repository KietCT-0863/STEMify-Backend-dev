namespace Identity.Application.Common.Interfaces.Services;

/// <summary>
/// Interface for email service to send verification emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email confirmation email to user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="confirmationToken">Email confirmation token</param>
    /// <param name="callbackUrl">Callback URL for email confirmation</param>
    /// <returns>Task representing the operation</returns>
    Task SendEmailConfirmationAsync(string email, string confirmationToken, string callbackUrl);

    /// <summary>
    /// Send password reset email to user
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="callbackUrl">Callback URL for password reset</param>
    /// <returns>Task representing the operation</returns>
    Task SendPasswordResetEmailAsync(string email, string resetToken, string callbackUrl);

    /// <summary>
    /// Send welcome email after email confirmation
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="userType">User type (Teacher/Student)</param>
    /// <returns>Task representing the operation</returns>
    Task SendWelcomeEmailAsync(string email, string firstName, string userType);
}
