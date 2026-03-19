using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Identity.Application.Authentication.Commands.ProcessExternalLogin;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Common.Models.Auth;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using DomainUser = Identity.Domain.Entities.User;
using Identity.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IAntiforgery _antiforgery;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailService emailService,
        ILogger<AccountController> logger,
        IMediator mediator,
        IConfiguration configuration,
        IGoogleOAuthService googleOAuthService,
        IAntiforgery antiforgery,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager
    )
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _logger = logger;
        _mediator = mediator;
        _configuration = configuration;
        _googleOAuthService = googleOAuthService;
        _antiforgery = antiforgery;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Clear existing external cookie
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["GoogleClientId"] = _configuration["Authentication:Google:ClientId"];
        
        if (TempData.ContainsKey("ErrorMessage"))
        {
            ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            TempData.Remove("ErrorMessage");
        }
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại thông tin đăng nhập.";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        if (user.Status == Domain.Enums.UserStatus.Disabled)
        {
            TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        if (user.Status == Domain.Enums.UserStatus.Deleted)
        {
            TempData["ErrorMessage"] = "Tài khoản của bạn đã bị xóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        if (user.Status == Domain.Enums.UserStatus.Locked)
        {
            TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        // Check if email is confirmed
        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            _logger.LogWarning("Login attempt with unconfirmed email: {Email}", model.Email);
            TempData["ErrorMessage"] = "Bạn cần xác nhận email trước khi đăng nhập. Vui lòng kiểm tra hộp thư và nhấn vào liên kết xác nhận.";
            
            // Provide option to resend confirmation email
            TempData["UnconfirmedEmail"] = model.Email;
            TempData["ShowResendOption"] = true;
            
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", model.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa tạm thời do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau.";
        }
        else if (result.IsNotAllowed)
        {
            TempData["ErrorMessage"] = "Bạn không được phép đăng nhập. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
        }
        else
        {
            TempData["ErrorMessage"] = "Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại thông tin đăng nhập.";
        }

        return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string? returnUrl = null, string? mode = null)
    {
        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

       
        var isSecureRequest = Request.IsHttps ||
                              string.Equals(
                                  Request.Headers["X-Forwarded-Proto"],
                                  "https",
                                  StringComparison.OrdinalIgnoreCase);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddYears(-1),
            Domain = null
        };

        Response.Cookies.Delete(".AspNetCore.Identity.Application", cookieOptions);
        var allCookies = Request.Cookies.Keys.ToList();
        var antiforgeryCookies = allCookies
            .Where(key => key.StartsWith(".AspNetCore.Antiforgery.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var cookieName in antiforgeryCookies)
        {
            Response.Cookies.Delete(cookieName, cookieOptions);
            _logger.LogDebug("Deleted Antiforgery cookie: {CookieName}", cookieName);
        }

        var aspNetCoreCookies = allCookies
            .Where(key => key.StartsWith(".AspNetCore.", StringComparison.OrdinalIgnoreCase) 
                       && !key.Equals(".AspNetCore.Identity.Application", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Aspire Dashboard cookies (need to delete on logout)
        var aspireCookies = allCookies
            .Where(key => key.StartsWith(".Aspire.Dashboard.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var cookieName in aspNetCoreCookies)
        {
            Response.Cookies.Delete(cookieName, cookieOptions);
            _logger.LogDebug("Deleted AspNetCore cookie: {CookieName}", cookieName);
        }

        foreach (var cookieName in aspireCookies)
        {
            Response.Cookies.Delete(cookieName, cookieOptions);
            _logger.LogDebug("Deleted Aspire cookie: {CookieName}", cookieName);
        }


        if (!string.IsNullOrWhiteSpace(Request.Host.Host))
        {
            var fallbackOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(-1),
                Domain = Request.Host.Host
            };

            Response.Cookies.Delete(".AspNetCore.Identity.Application", fallbackOptions);

            foreach (var cookieName in antiforgeryCookies)
            {
                Response.Cookies.Delete(cookieName, fallbackOptions);
                _logger.LogDebug("Fallback deleted Antiforgery cookie: {CookieName}", cookieName);
            }

            foreach (var cookieName in aspNetCoreCookies)
            {
                Response.Cookies.Delete(cookieName, fallbackOptions);
                _logger.LogDebug("Fallback deleted AspNetCore cookie: {CookieName}", cookieName);
            }

            foreach (var cookieName in aspireCookies)
            {
                Response.Cookies.Delete(cookieName, fallbackOptions);
                _logger.LogDebug("Fallback deleted Aspire cookie: {CookieName}", cookieName);
            }
        }

        if (mode == "redirect")
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        return Ok(new { message = "Logged out" });
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Create platform member user
        var userId = Guid.NewGuid();

        try
        {
            var platformUser = DomainUser.Create(
                userId,
                model.Email,
                model.Email, // Use email as username
                model.FirstName,
                model.LastName,
                UserRole.Member
            );

            var result = await _userManager.CreateAsync(platformUser, model.Password);

            if (result.Succeeded)
            {
                // Normalize role name from user type
                var roleName = UserRole.Member.ToString();

                // Verify role exists 
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogError(
                        "Role {Role} does not exist! This indicates seeding failed during app startup. User registration cannot proceed.",
                        roleName
                    );
                    await _userManager.DeleteAsync(platformUser);
                    ModelState.AddModelError(
                        string.Empty,
                        "System error: User role not available. Please contact administrator."
                    );
                    return View(model);
                }

                // Add user to role (creates AspNetUserRoles row)
                var roleResult = await _userManager.AddToRoleAsync(platformUser, roleName);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to add user {Email} to role {Role}: {Errors}",
                        model.Email,
                        roleName,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description))
                    );
                    await _userManager.DeleteAsync(platformUser);
                    ModelState.AddModelError(
                        string.Empty,
                        "Failed to assign role. Please try again."
                    );
                    return View(model);
                }

                _logger.LogInformation(
                    "User {Email} created with role {Role}.",
                    model.Email,
                    roleName
                );

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(platformUser);
                
                // Create confirmation callback URL
                var callbackUrl = Url.Action(
                    "ConfirmEmail", 
                    "Account",
                    new { userId = platformUser.Id, code = emailToken },
                    protocol: Request.Scheme
                ) ?? throw new InvalidOperationException("Failed to generate confirmation URL");

                // Send email confirmation
                await _emailService.SendEmailConfirmationAsync(
                    platformUser.Email!, 
                    emailToken, 
                    callbackUrl
                );

                _logger.LogInformation(
                    "Email confirmation sent to {Email}",
                    model.Email
                );

                // Redirect to email confirmation page 
                TempData["SuccessMessage"] = $"Tài khoản đã được tạo thành công! Vui lòng kiểm tra email {model.Email} để xác nhận tài khoản.";
                TempData["UserEmail"] = model.Email;
                
                return RedirectToAction("EmailConfirmationSent", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Consent(string? returnUrl = null)
    {
        var clientId = HttpContext.Request.Query["client_id"].FirstOrDefault();
        var scope = HttpContext.Request.Query["scope"].FirstOrDefault();
        var redirectUri = HttpContext.Request.Query["redirect_uri"].FirstOrDefault();

        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogWarning("Consent page accessed without client_id parameter");
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            var currentUrl = $"{Request.Path}{Request.QueryString}";
            return RedirectToAction("Login", new { returnUrl = currentUrl });
        }

        var model = new ConsentViewModel
        {
            ApplicationName = clientId,
            Scope = scope ?? string.Empty,
            ReturnUrl = $"/connect/authorize{Request.QueryString}",
            RequestedScopes = (scope ?? string.Empty).Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            ),
        };

        _logger.LogInformation(
            "Displaying consent page for client: {ClientId}, user: {UserId}",
            clientId,
            user.Id
        );
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consent(ConsentViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        if (model.Accept)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                var separator = model.ReturnUrl.Contains("?") ? "&" : "?";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var consentGrantedUrl = $"{model.ReturnUrl}{separator}consent_granted=true&_t={timestamp}";

                _logger.LogInformation(
                    "User {UserId} granted consent, redirecting back to: {ReturnUrl}",
                    user.Id,
                    consentGrantedUrl
                );
                return Redirect(consentGrantedUrl);
            }
        }
        else
        {
            var clientId = HttpContext.Request.Form["client_id"].FirstOrDefault();
            var redirectUri = HttpContext.Request.Form["redirect_uri"].FirstOrDefault();

            if (!string.IsNullOrEmpty(redirectUri))
            {
                var errorUrl =
                    $"{redirectUri}?error=access_denied&error_description=The+user+denied+the+request";
                _logger.LogInformation(
                    "User {UserId} denied consent, redirecting to: {ErrorUrl}",
                    user.Id,
                    errorUrl
                );
                return Redirect(errorUrl);
            }
        }

        _logger.LogWarning("Consent flow completed but no valid redirect URL found");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult EmailConfirmationSent()
    {
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.UserEmail = TempData["UserEmail"];
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            ViewBag.ErrorMessage = "Liên kết xác nhận email không hợp lệ.";
            return View("EmailConfirmationResult");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            ViewBag.ErrorMessage = "Không tìm thấy người dùng.";
            return View("EmailConfirmationResult");
        }

        if (user.EmailConfirmed)
        {
            ViewBag.SuccessMessage = "Email đã được xác nhận trước đó.";
            ViewBag.IsAlreadyConfirmed = true;
            return View("EmailConfirmationResult");
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed for user {Email}", user.Email);

            await _emailService.SendWelcomeEmailAsync(
                user.Email!,
                user.FirstName,
                user.Role.ToString()
            );

            ViewBag.SuccessMessage = "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay bây giờ.";
            ViewBag.UserEmail = user.Email;
            return View("EmailConfirmationResult");
        }

        ViewBag.ErrorMessage = "Có lỗi xảy ra khi xác nhận email. Liên kết có thể đã hết hạn.";
        ViewBag.UserId = userId;
        return View("EmailConfirmationResult");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendEmailConfirmation(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Vui lòng cung cấp địa chỉ email.";
            return RedirectToAction("EmailConfirmationSent");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            TempData["SuccessMessage"] = "Nếu email tồn tại, email xác nhận đã được gửi lại.";
            return RedirectToAction("EmailConfirmationSent");
        }

        if (user.EmailConfirmed)
        {
            TempData["InfoMessage"] = "Email đã được xác nhận trước đó.";
            return RedirectToAction("Login");
        }

        try
        {
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, code = emailToken },
                protocol: Request.Scheme
            ) ?? throw new InvalidOperationException("Failed to generate confirmation URL");

            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                emailToken,
                callbackUrl
            );

            _logger.LogInformation("Email confirmation resent to {Email}", user.Email);
            TempData["SuccessMessage"] = $"Email xác nhận đã được gửi lại đến {email}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend email confirmation to {Email}", email);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
        }

        TempData["UserEmail"] = email;
        return RedirectToAction("EmailConfirmationSent");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            TempData.Remove("SuccessMessage");
            TempData["ErrorMessage"] = "Email không tồn tại trong hệ thống. Vui lòng kiểm tra lại địa chỉ email.";
            return View(model);
        }

        if (!user.CanLogin())
        {
            TempData.Remove("SuccessMessage");
            TempData["ErrorMessage"] = "Tài khoản của bạn không thể đăng nhập. Vui lòng liên hệ quản trị viên để được hỗ trợ.";
            return View(model);
        }

        try
        {
           var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, code = resetToken },
                protocol: Request.Scheme
            ) ?? throw new InvalidOperationException("Failed to generate reset password URL");

            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                resetToken,
                callbackUrl
            );

            TempData.Remove("ErrorMessage"); 
            TempData["SuccessMessage"] = "Nếu email tồn tại, email đặt lại mật khẩu đã được gửi.";
        }
        catch (Exception)
        {
            TempData.Remove("SuccessMessage"); 
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
        }

        return RedirectToAction("ForgotPasswordConfirmation");
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        var successMsg = TempData.Peek("SuccessMessage")?.ToString() ?? TempData["SuccessMessage"]?.ToString();
        var errorMsg = TempData.Peek("ErrorMessage")?.ToString() ?? TempData["ErrorMessage"]?.ToString();
        ViewBag.SuccessMessage = successMsg;
        ViewBag.ErrorMessage = errorMsg;
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string? userId = null, string? code = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            ViewBag.ErrorMessage = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return View("ResetPasswordResult");
        }

        var model = new ResetPasswordViewModel
        {
            UserId = userId,
            Code = code
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Code))
        {
            ViewBag.ErrorMessage = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return View("ResetPasswordResult");
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            ViewBag.ErrorMessage = "Không tìm thấy người dùng.";
            return View("ResetPasswordResult");
        }

        // if (!user.CanLogin())
        // {
        //     _logger.LogWarning("Password reset attempted for user {Email} with status {Status}", user.Email, user.Status);
        //     ViewBag.ErrorMessage = $"Tài khoản của bạn đang ở trạng thái {user.Status}. Vui lòng liên hệ quản trị viên.";
        //     return View("ResetPasswordResult");
        // }

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successful for user {Email}", user.Email);
            ViewBag.SuccessMessage = "Mật khẩu đã được đặt lại thành công! Bạn có thể đăng nhập ngay bây giờ.";
            ViewBag.UserEmail = user.Email;
            return View("ResetPasswordResult");
        }

       foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(
            nameof(ExternalLoginCallback),
            "Account",
            new { ReturnUrl = returnUrl }
        );

        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            provider,
            redirectUrl
        );

        _logger.LogInformation("Initiating external login with provider: {Provider}", provider);

        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handle callback from external authentication provider
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            _logger.LogWarning("External login error: {Error}", remoteError);
            TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        // Get information from the external login provider
        var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalLoginInfo == null)
        {
            _logger.LogWarning("External login info is null");
            TempData["ErrorMessage"] = "Error loading external login information.";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        try
        {
            // Extract user information from external provider
            var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email)
                ?? throw new InvalidOperationException("Email not provided by external provider");

            var givenName = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
            var surname = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

            // If no separate first/last names, try to split the full name
            if (string.IsNullOrEmpty(givenName) && string.IsNullOrEmpty(surname))
            {
                var name = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    givenName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                    surname = nameParts.Length > 1 ? nameParts[1] : string.Empty;
                }
            }

            var picture = externalLoginInfo.Principal.FindFirstValue("picture")
                ?? externalLoginInfo.Principal.FindFirstValue("urn:google:picture");

            var externalLoginInfoDto = new ExternalLoginInfoDto
            {
                Provider = externalLoginInfo.LoginProvider,
                ProviderType = MapProviderNameToEnum(externalLoginInfo.LoginProvider),
                ProviderKey = externalLoginInfo.ProviderKey,
                Email = email,
                FirstName = givenName,
                LastName = surname,
                ProfilePictureUrl = picture
            };

            if (!string.IsNullOrEmpty(returnUrl) && !Url.IsLocalUrl(returnUrl))
            {
                _logger.LogWarning("Invalid returnUrl in external login callback: {ReturnUrl}", returnUrl);
                returnUrl = Url.Content("~/");
            }

            var processResult = await ProcessExternalLoginAsync(
                externalLoginInfoDto,
                returnUrl,
                externalLoginInfo.LoginProvider,
                generateOAuthToken: false 
            );

            if (processResult.IsSuccess)
            {
                if (!string.IsNullOrEmpty(processResult.RedirectUrl) && Url.IsLocalUrl(processResult.RedirectUrl))
                {
                    return Redirect(processResult.RedirectUrl);
                }
                else
                {
                    _logger.LogWarning("Invalid redirect URL after external login: {RedirectUrl}", processResult.RedirectUrl);
                    return RedirectToAction("Index", "Home");
                }
            }

            // Login failed 
            TempData["ErrorMessage"] = processResult.ErrorMessage ?? "External login failed";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during external login callback");
            TempData["ErrorMessage"] = "An error occurred during external login";
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GoogleOneTap([FromBody] GoogleOneTapRequest request, string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google One Tap: Anti-forgery token validation failed");
            return Json(new { success = false, error = "Invalid request token" });
        }

        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            _logger.LogWarning("Google One Tap: Missing credential");
            return Json(new { success = false, error = "Missing credential" });
        }

        try
        {
            // Verify the credential token
            var claims = await _googleOAuthService.VerifyIdTokenAsync(request.Credential);
            
            // Extract user information
            var (googleId, email, firstName, lastName, profilePictureUrl) = _googleOAuthService.ExtractUserInfo(claims);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Google One Tap: Email not found in credential");
                return Json(new { success = false, error = "Email not provided by Google" });
            }

            // Create ExternalLoginInfoDto
            var externalLoginInfoDto = new ExternalLoginInfoDto
            {
                Provider = "Google",
                ProviderType = ExternalAuthProvider.Google,
                ProviderKey = googleId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ProfilePictureUrl = profilePictureUrl
            };

            // Process external login using common method (SOLID - DRY principle)
            // Generate OAuth token by redirecting to authorization endpoint
            var processResult = await ProcessExternalLoginAsync(
                externalLoginInfoDto,
                returnUrl,
                "Google",
                generateOAuthToken: true
            );

            if (processResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Google One Tap login successful, returning OAuth authorization URL: {Url}",
                    processResult.RedirectUrl
                );
                return Json(new
                {
                    success = true,
                    redirectUrl = processResult.RedirectUrl,
                    message = "Login successful. Redirect to OAuth authorization to get token."
                });
            }

            // Login failed - return error
            return Json(new { success = false, error = processResult.ErrorMessage ?? "Login failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Google One Tap login");
            return Json(new { success = false, error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Common method to process external login (Google One Tap and External Login)
    /// </summary>
    private async Task<ExternalLoginProcessResult> ProcessExternalLoginAsync(
        ExternalLoginInfoDto externalLoginInfoDto,
        string? returnUrl = null,
        string providerName = "Google",
        bool generateOAuthToken = true)
    {
        returnUrl ??= Url.Content("~/");

        // Process external login using CQRS command
        var command = new ProcessExternalLoginCommand
        {
            ExternalLoginInfo = externalLoginInfoDto,
            DefaultUserRole = UserRole.Member,
            ReturnUrl = returnUrl
        };

        var result = await _mediator.Send(command);

        // Check if login failed
        if (!result.Succeeded || !result.UserId.HasValue)
        {
            _logger.LogError("External login failed: {Error}", result.ErrorMessage);
            return ExternalLoginProcessResult.Failure(result.ErrorMessage ?? "Login failed");
        }

        // Get user to check status
        var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());
        if (user == null)
        {
            _logger.LogError("User {UserId} not found after successful login", result.UserId.Value);
            return ExternalLoginProcessResult.Failure("User not found");
        }

        // Check if user can login (must be Active and email confirmed)
        if (!user.CanLogin())
        {
            if (result.IsNewUser)
            {
                _logger.LogWarning(
                    "Login attempt by new user {Email} from {Provider} - user created but status is {Status}, requires activation",
                    result.Email,
                    providerName,
                    user.Status
                );
                return ExternalLoginProcessResult.Failure(
                    "Tài khoản của bạn đã được tạo nhưng chưa được kích hoạt. Vui lòng liên hệ quản trị viên để được kích hoạt tài khoản."
                );
            }
            else
            {
                _logger.LogWarning(
                    "Login attempt by user {Email} with status {Status} - rejected",
                    result.Email,
                    user.Status
                );
                return ExternalLoginProcessResult.Failure(
                    $"Tài khoản của bạn đang ở trạng thái {user.Status}. Vui lòng liên hệ quản trị viên."
                );
            }
        }

        // User can login - sign in
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation(
            "User {Email} logged in with {Provider} provider",
            result.Email,
            providerName
        );

        // If generateOAuthToken is true, redirect to OAuth authorization endpoint to get token
        if (generateOAuthToken)
        {
            var oauthRedirectUrl = BuildOAuthAuthorizationUrl(returnUrl);
            return ExternalLoginProcessResult.Success(oauthRedirectUrl, result.Email ?? string.Empty);
        }

        // Otherwise, use the original returnUrl
        string redirectUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            redirectUrl = returnUrl;
        }
        else
        {
            redirectUrl = Url.Action("Index", "Home") ?? "/";
        }

        return ExternalLoginProcessResult.Success(redirectUrl, result.Email ?? string.Empty);
    }

    /// <summary>
    /// Build OAuth authorization URL to get authorization code/token after external login
    /// </summary>
    private string BuildOAuthAuthorizationUrl(string? returnUrl = null)
    {
        // Default client ID and scopes
        var clientId = _configuration["OAuth:ClientId"] ?? "stemify-web";
        var defaultScopes = "stemify_api openid profile email roles";
        var defaultRedirectUri = _configuration["OAuth:RedirectUri"] ?? "https://localhost:3000/api/auth/callback/oidc";

        // Build authorization URL
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var authorizeUrl = $"{baseUrl}/connect/authorize";

        // Build query parameters
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "response_type", "code" },
            { "scope", defaultScopes },
            { "redirect_uri", defaultRedirectUri },
            { "prompt", "none" } // Skip consent since user just logged in
        };

        // Add state if returnUrl is provided
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            queryParams["state"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(returnUrl));
        }

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var fullUrl = $"{authorizeUrl}?{queryString}";

        _logger.LogInformation(
            "Built OAuth authorization URL for external login: {Url}",
            fullUrl
        );

        return fullUrl;
    }

    /// <summary>
    /// Maps provider name string to ExternalAuthProvider enum
    /// </summary>
    private static ExternalAuthProvider MapProviderNameToEnum(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "google" => ExternalAuthProvider.Google,
            _ => ExternalAuthProvider.None
        };
    }
}

/// <summary>
/// Result class for external login processing
/// </summary>
internal class ExternalLoginProcessResult
{
    public bool IsSuccess { get; private set; }
    public string? RedirectUrl { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Email { get; private set; }

    private ExternalLoginProcessResult(bool isSuccess, string? redirectUrl = null, string? errorMessage = null, string? email = null)
    {
        IsSuccess = isSuccess;
        RedirectUrl = redirectUrl;
        ErrorMessage = errorMessage;
        Email = email;
    }

    public static ExternalLoginProcessResult Success(string redirectUrl, string email)
    {
        return new ExternalLoginProcessResult(true, redirectUrl, null, email);
    }

    public static ExternalLoginProcessResult Failure(string errorMessage)
    {
        return new ExternalLoginProcessResult(false, null, errorMessage, null);
    }
}

// ViewModels
public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public required string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên")]
    [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ")]
    [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
    public required string LastName { get; set; }

    // Optional profile fields
    [StringLength(500, ErrorMessage = "Bio cannot be more than 500 characters")]
    public string? Bio { get; set; } = "Tell us a bit about yourself...";
}

public class ConsentViewModel
{
    public required string ApplicationName { get; set; }
    public required string Scope { get; set; }
    public string? ReturnUrl { get; set; }
    public string[] RequestedScopes { get; set; } = Array.Empty<string>();
    public bool Accept { get; set; }
}

public class GoogleOneTapRequest
{
    [Required]
    public required string Credential { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public required string Email { get; set; }
}

public class ResetPasswordViewModel
{
    [Required]
    public string? UserId { get; set; }

    [Required]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string? ConfirmPassword { get; set; }
}
