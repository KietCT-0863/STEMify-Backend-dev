using Contracts.Abstractions.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Infrastructure.Services;

/// <summary>
/// Email template service implementation
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly Dictionary<string, string> _templates;
    private readonly Regex _placeholderRegex;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _placeholderRegex = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        LoadDefaultTemplates();
    }

    public async Task<string> ProcessTemplateAsync(string templateName, Dictionary<string, object> templateData)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        if (!_templates.ContainsKey(templateName))
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));

        var template = _templates[templateName];
        var processedTemplate = await ProcessTemplateContentAsync(template, templateData ?? new Dictionary<string, object>());

        _logger.LogDebug("Processed template '{TemplateName}' with {DataCount} data items", templateName, templateData?.Count ?? 0);

        return processedTemplate;
    }

    public bool TemplateExists(string templateName)
    {
        return !string.IsNullOrWhiteSpace(templateName) && _templates.ContainsKey(templateName);
    }

    public void RegisterTemplate(string templateName, string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        if (string.IsNullOrWhiteSpace(templateContent))
            throw new ArgumentException("Template content is required", nameof(templateContent));

        _templates[templateName] = templateContent;
        _logger.LogInformation("Registered template '{TemplateName}'", templateName);
    }

    public async Task LoadTemplatesAsync(string templateDirectory)
    {
        if (string.IsNullOrWhiteSpace(templateDirectory))
            throw new ArgumentException("Template directory is required", nameof(templateDirectory));

        if (!Directory.Exists(templateDirectory))
        {
            _logger.LogWarning("Template directory '{TemplateDirectory}' does not exist", templateDirectory);
            return;
        }

        try
        {
            var templateFiles = Directory.GetFiles(templateDirectory, "*.html", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(templateDirectory, "*.txt", SearchOption.AllDirectories));

            foreach (var filePath in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(filePath);
                var templateContent = await File.ReadAllTextAsync(filePath);

                RegisterTemplate(templateName, templateContent);
            }

            _logger.LogInformation("Loaded {Count} templates from '{TemplateDirectory}'", _templates.Count, templateDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load templates from '{TemplateDirectory}'", templateDirectory);
            throw;
        }
    }

    public IEnumerable<string> GetAvailableTemplates()
    {
        return _templates.Keys;
    }

    private async Task<string> ProcessTemplateContentAsync(string template, Dictionary<string, object> data)
    {
        return await Task.Run(() =>
        {
            var result = _placeholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;

                if (data.ContainsKey(key))
                {
                    return data[key]?.ToString() ?? string.Empty;
                }

                // If no data found, keep the placeholder for debugging
                _logger.LogDebug("Template placeholder '{Key}' not found in data", key);
                return match.Value;
            });

            return result;
        });
    }

    private void LoadDefaultTemplates()
    {
        // Welcome email template
        RegisterTemplate("welcome", @"Welcome to {{AppName}}!
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }
        .content { padding: 40px 30px; }
        .welcome-box { background-color: #f8f9ff; padding: 30px; border-radius: 10px; margin: 30px 0; text-align: center; border-left: 4px solid #667eea; }
        .features { background-color: #f9f9f9; padding: 30px; border-radius: 8px; margin: 30px 0; }
        .features ul { list-style: none; padding: 0; }
        .features li { padding: 10px 0; border-bottom: 1px solid #eee; }
        .features li:last-child { border-bottom: none; }
        .button { display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; margin: 20px 0; font-weight: bold; }
        .footer { background-color: #2c3e50; color: white; padding: 30px 20px; text-align: center; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{AppName}}</h1>
        </div>
        <div class='content'>
            <div class='welcome-box'>
                <h2>Welcome {{FirstName}}!</h2>
                <p>Your account has been successfully activated.</p>
            </div>
            
            <p>Thank you for joining {{AppName}}. We're excited to have you on board!</p>
            
            <div class='features'>
                <h3> What you can do now:</h3>
                <ul>
                    <li><strong>Access Resources</strong> - Explore our comprehensive library</li>
                    <li> <strong>Watch Tutorials</strong> - Learn with high-quality videos</li>
                    <li> <strong>Interactive Labs</strong> - Practice with hands-on experiments</li>
                    <li> <strong>Join Community</strong> - Connect with other learners</li>
                    <li><strong>Track Progress</strong> - Monitor your learning journey</li>
                </ul>
            </div>
            
            <div style='text-align: center;'>
                <a href='{{DashboardUrl}}' class='button'>Go to Dashboard</a>
            </div>
            
            <p>If you need any help, don't hesitate to contact our support team at {{SupportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>© 2024 {{AppName}}. All rights reserved.</p>
            <p>Thank you for choosing {{AppName}}!</p>
        </div>
    </div>
</body>
</html>");

        // Password reset template
        RegisterTemplate("password-reset", @"Password Reset Request
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { background: linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%); color: white; padding: 40px 20px; text-align: center; }
        .content { padding: 40px 30px; }
        .button { display: inline-block; background: linear-gradient(135deg, #ff6b6b 0%, #ee5a52 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; margin: 20px 0; font-weight: bold; }
        .warning { background-color: #fff3cd; padding: 20px; border-radius: 8px; margin: 30px 0; border-left: 4px solid #ffc107; }
        .footer { background-color: #2c3e50; color: white; padding: 30px 20px; text-align: center; }
        .code-block { background-color: #f8f9fa; padding: 15px; border-radius: 5px; font-family: monospace; word-break: break-all; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{AppName}}</h1>
        </div>
        <div class='content'>
            <h2>Password Reset Request</h2>
            <p>Hello {{FirstName}},</p>
            <p>We received a request to reset your password for your {{AppName}} account.</p>
            
            <div style='text-align: center;'>
                <a href='{{ResetUrl}}' class='button'>Reset Password</a>
            </div>
            
            <div class='warning'>
                <strong>Security Notice:</strong>
                <ul>
                    <li>This link is valid for {{ExpirationHours}} hours only</li>
                    <li>If you didn't request this reset, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                </ul>
            </div>
            
            <p>Or copy and paste this link into your browser:</p>
            <div class='code-block'>{{ResetUrl}}</div>
            
            <p>If you need help, contact us at {{SupportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>© 2024 {{AppName}}. All rights reserved.</p>
            <p>This is an automated email, please do not reply.</p>
        </div>
    </div>
</body>
</html>");

        // Email confirmation template
        RegisterTemplate("email-confirmation", @"Email Confirmation Required
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Confirm Email</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { background: linear-gradient(135deg, #4ecdc4 0%, #44a08d 100%); color: white; padding: 40px 20px; text-align: center; }
        .content { padding: 40px 30px; }
        .button { display: inline-block; background: linear-gradient(135deg, #4ecdc4 0%, #44a08d 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; margin: 20px 0; font-weight: bold; }
        .footer { background-color: #2c3e50; color: white; padding: 30px 20px; text-align: center; }
        .code-block { background-color: #f8f9fa; padding: 15px; border-radius: 5px; font-family: monospace; word-break: break-all; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{AppName}}</h1>
        </div>
        <div class='content'>
            <h2>Confirm Your Email Address</h2>
            <p>Hello {{FirstName}},</p>
            <p>Thank you for registering with {{AppName}}! To complete your registration, please confirm your email address.</p>
            
            <div style='text-align: center;'>
                <a href='{{ConfirmationUrl}}' class='button'>Confirm Email</a>
            </div>
            
            <p>Or copy and paste this link into your browser:</p>
            <div class='code-block'>{{ConfirmationUrl}}</div>
            
            <p><strong>Note:</strong> This link will expire in 24 hours for security reasons.</p>
            
            <p>If you didn't create an account with us, please ignore this email.</p>
            
            <p>Need help? Contact us at {{SupportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>© 2024 {{AppName}}. All rights reserved.</p>
            <p>This is an automated email, please do not reply.</p>
        </div>
    </div>
</body>
</html>");

        // General notification template
        RegisterTemplate("notification", @"{{Subject}}
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Notification</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }
        .content { padding: 40px 30px; }
        .footer { background-color: #2c3e50; color: white; padding: 30px 20px; text-align: center; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{AppName}}</h1>
        </div>
        <div class='content'>
            <h2>{{Title}}</h2>
            <p>Hello {{FirstName}},</p>
            <div>{{Message}}</div>
            <p>Thank you for using {{AppName}}!</p>
        </div>
        <div class='footer'>
            <p>© 2024 {{AppName}}. All rights reserved.</p>
            <p>If you need help, contact us at {{SupportEmail}}.</p>
        </div>
    </div>
</body>
</html>");

        // Vietnamese email confirmation template (from Identity service)
        RegisterTemplate("email-confirmation-vi", @"Xác nhận email - STEMify Platform
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác nhận Email</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #7dd3fc; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px 20px; }
        .button { 
            display: inline-block; 
            background-color: #7dd3fc; 
            color: white; 
            padding: 12px 30px; 
            text-decoration: none; 
            border-radius: 5px; 
            margin: 20px 0; 
        }
        .footer { background-color: #f4f4f4; padding: 20px; text-align: center; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{AppName}}</h1>
        </div>
        <div class='content'>
            <h2>Chào mừng bạn đến với STEMify!</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất quá trình đăng ký, vui lòng xác nhận địa chỉ email của bạn.</p>
            
            <p>Nhấn vào nút bên dưới để xác nhận email:</p>
            
            <div style='text-align: center;'>
                <a href='{{ConfirmationUrl}}' class='button'>Xác Nhận Email</a>
            </div>
            
            <p>Hoặc sao chép và dán liên kết sau vào trình duyệt:</p>
            <p style='word-break: break-all; background-color: #f9f9f9; padding: 10px; border-radius: 3px;'>
                {{ConfirmationUrl}}
            </p>
            
            <p><strong>Lưu ý:</strong> Liên kết này sẽ hết hạn sau 24 giờ.</p>
            
            <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
        </div>
        <div class='footer'>
            <p>© {{CurrentYear}} {{AppName}}. All rights reserved.</p>
            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>");

        // Vietnamese password reset template (from Identity service)
        RegisterTemplate("password-reset-vi", @"Đặt lại mật khẩu - STEMify Platform
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đặt Lại Mật Khẩu</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #7dd3fc; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px 20px; }
        .button { 
            display: inline-block; 
            background-color: #7dd3fc; 
            color: white; 
            padding: 12px 30px; 
            text-decoration: none; 
            border-radius: 5px; 
            margin: 20px 0; 
        }
        .footer { background-color: #f4f4f4; padding: 20px; text-align: center; font-size: 14px; }
        .warning { background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1> {{AppName}}</h1>
        </div>
        <div class='content'>
            <h2>Yêu cầu đặt lại mật khẩu</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            
            <p>Nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
            
            <div style='text-align: center;'>
                <a href='{{ResetUrl}}' class='button'>Đặt Lại Mật Khẩu</a>
            </div>
            
            <div class='warning'>
                <strong> Lưu ý bảo mật:</strong>
                <ul>
                    <li>Liên kết này chỉ có hiệu lực trong {{ExpirationHours}} giờ</li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này</li>
                    <li>Không chia sẻ liên kết này với bất kỳ ai</li>
                </ul>
            </div>
            
            <p>Hoặc sao chép và dán liên kết sau vào trình duyệt:</p>
            <p style='word-break: break-all; background-color: #f9f9f9; padding: 10px; border-radius: 3px;'>
                {{ResetUrl}}
            </p>
        </div>
        <div class='footer'>
            <p>© {{CurrentYear}} {{AppName}}. All rights reserved.</p>
            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>");

        // Vietnamese welcome email template (from Identity service)
        RegisterTemplate("welcome-vi", @"Chào mừng đến với STEMify Platform!
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Chào Mừng</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #7dd3fc; color: white; padding: 20px; text-align: center; }
        .content { padding: 30px 20px; }
        .welcome-box { background-color: #f0f9ff; padding: 20px; border-radius: 10px; margin: 20px 0; text-align: center; }
        .features { background-color: #f9f9f9; padding: 20px; border-radius: 5px; margin: 20px 0; }
        .footer { background-color: #f4f4f4; padding: 20px; text-align: center; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1> {{AppName}}</h1>
        </div>
        <div class='content'>
            <div class='welcome-box'>
                <h2>{{RoleIcon}} Chào mừng {{FirstName}}!</h2>
                <p>Tài khoản <strong>{{RoleText}}</strong> của bạn đã được kích hoạt thành công.</p>
            </div>
            
            <h3> Bạn có thể bắt đầu khám phá:</h3>
            <div class='features'>
                <ul>
                    <li> <strong>Tài liệu học tập</strong> - Truy cập kho tài liệu phong phú</li>
                    <li> <strong>Video bài giảng</strong> - Học tập qua video chất lượng cao</li>
                    <li> <strong>Thực hành tương tác</strong> - Mô phỏng thí nghiệm STEM</li>
                    <li> <strong>Cộng đồng học tập</strong> - Kết nối với giáo viên và học sinh</li>
                    <li> <strong>Theo dõi tiến độ</strong> - Quản lý quá trình học tập</li>
                </ul>
            </div>
            
            <p>Hãy bắt đầu hành trình học tập STEM tuyệt vời cùng chúng tôi!</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{{DashboardUrl}}' style='display: inline-block; background-color: #7dd3fc; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px;'>
                     Đi đến Dashboard
                </a>
            </div>
            
            <p>Nếu bạn cần hỗ trợ, đừng ngần ngại liên hệ với đội ngũ hỗ trợ của chúng tôi tại {{SupportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>© {{CurrentYear}} {{AppName}}. All rights reserved.</p>
            <p>Cảm ơn bạn đã tin tương và sử dụng {{AppName}}!</p>
        </div>
    </div>
</body>
</html>");

        _logger.LogInformation("Loaded {Count} default email templates", _templates.Count);
    }
}
