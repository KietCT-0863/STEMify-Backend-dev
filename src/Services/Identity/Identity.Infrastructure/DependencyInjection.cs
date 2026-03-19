using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Extensions;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Infrastructure.BackgroundServices;
using Identity.Infrastructure.BackgroundServices.Consumers;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Data.Seeders;
using Identity.Infrastructure.Grpc;
using Identity.Infrastructure.Identity;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Infrastructure.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Protos.Order;
using Sieve.Services;
using System.Security.Cryptography.X509Certificates;
using static OpenIddict.Abstractions.OpenIddictConstants;
using IdentityIEmailService = Identity.Application.Common.Interfaces.Services.IEmailService;

namespace Identity.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableConsumers = true
    )
    {
        var isProduction = string.Equals(
            configuration["DOTNET_ENVIRONMENT"]
            ?? configuration["ASPNETCORE_ENVIRONMENT"],
            "Production",
            StringComparison.OrdinalIgnoreCase
        );

        // Ensure DbContext is registered before Infrastructure services
        // This is critical for IdentityUnitOfWork dependency injection
        EnsureDbContextRegistered(services, configuration);

        // Register Application layer services
        services.AddApplicationServices();

        // New clean architecture seeding implementation
        services.AddScoped<IRoleSeeder, RoleSeeder>();
        services.AddScoped<IUserSeeder, UserSeeder>();
        services.AddScoped<IOAuthSeeder, OAuthSeeder>();
        services.AddScoped<IGroupSeeder, GroupSeeder>();
        services.AddScoped<IOrganizationUserSeeder, OrganizationUserSeeder>();

        // Register all seeding strategies
        services.AddScoped<ISeedingStrategy>(provider =>
            provider.GetRequiredService<IRoleSeeder>()
        );
        services.AddScoped<ISeedingStrategy>(provider =>
            provider.GetRequiredService<IUserSeeder>()
        );
        services.AddScoped<ISeedingStrategy>(provider =>
            provider.GetRequiredService<IOAuthSeeder>()
        );
        services.AddScoped<ISeedingStrategy>(provider =>
            provider.GetRequiredService<IGroupSeeder>()
        );
        services.AddScoped<ISeedingStrategy>(provider =>
            provider.GetRequiredService<IOrganizationUserSeeder>()
        );

        // Register the main data seeder service
        services.AddScoped<IDataSeeder, DataSeederService>();

        // Sieve for filtering, sorting, and pagination
        services.AddScoped<ISieveProcessor, SieveProcessor>();

        // Domain Events
        // services.AddScoped<IDomainEventsAccessor, DomainEventsAccessor>();
        // services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        // Unit of Work & Repository Pattern
        // services.AddScoped<IEfUnitOfWork<ApplicationDbContext>, EfUnitOfWork<ApplicationDbContext>>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();


        // OpenIddict Configuration
        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options
                    .UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                var issuer = configuration["OpenIddict:Issuer"] ?? "https://localhost:7131";
                options.SetIssuer(new Uri(issuer));
                
                // Use absolute URLs for endpoints to ensure correct public URLs
                options
                    .SetAuthorizationEndpointUris(new Uri(new Uri(issuer), "/connect/authorize"))
                    .SetTokenEndpointUris(new Uri(new Uri(issuer), "/connect/token"))
                    .SetUserInfoEndpointUris(new Uri(new Uri(issuer), "/connect/userinfo"))
                    .SetEndSessionEndpointUris(new Uri(new Uri(issuer), "/connect/logout"))
                    .SetIntrospectionEndpointUris(new Uri(new Uri(issuer), "/connect/introspect"))
                    .SetConfigurationEndpointUris(new Uri(new Uri(issuer), "/.well-known/openid-configuration"));

                options
                    .AllowAuthorizationCodeFlow()
                    .AllowClientCredentialsFlow()
                    .AllowRefreshTokenFlow()
                    .AllowPasswordFlow();

                options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles, Scopes.OfflineAccess);
                options.IgnoreScopePermissions();
                options.RequireProofKeyForCodeExchange();

                options.SetAccessTokenLifetime(TimeSpan.FromDays(10));

                if (!isProduction)
                {
                    var signingCertificate = LoadCertificateFromKeyVault(configuration);
                    signingCertificate ??= LoadCertificateFromBase64(
                        configuration,
                        "OpenIddict:SigningCertificate:Base64",
                        "OpenIddict:SigningCertificate:Password",
                        isProduction
                    );

                    var encryptionCertificate = LoadCertificateFromBase64(
                        configuration,
                        "OpenIddict:EncryptionCertificate:Base64",
                        "OpenIddict:EncryptionCertificate:Password",
                        isProduction
                    );

                    if (signingCertificate is not null)
                    {
                        options.AddSigningCertificate(signingCertificate);
                    }
                    else
                    {
                        options.AddDevelopmentSigningCertificate();
                    }

                    if (encryptionCertificate is not null)
                    {
                        options.AddEncryptionCertificate(encryptionCertificate);
                    }
                    else
                    {
                        options.AddDevelopmentEncryptionCertificate();
                    }
                }
                else
                {
                    options.AddDevelopmentSigningCertificate();
                    options.AddDevelopmentEncryptionCertificate();
                }

                options
                    .UseAspNetCore()
                    .DisableTransportSecurityRequirement()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();

                options.DisableAccessTokenEncryption();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Authentication & Authorization
        var authenticationBuilder = services
            .AddAuthentication()
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ReturnUrlParameter = "returnUrl";

                options.Cookie.HttpOnly = true;

                options.Cookie.SameSite = SameSiteMode.None;

                // Always require secure cookies in production to prevent mixed content errors
                options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

                // For API requests, return 401 instead of redirecting to login page
                options.Events.OnRedirectToLogin = context =>
                {
                    // Check if this is an API request (not a browser navigation)
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Path.StartsWithSegments("/admin") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    
                    // For browser requests, allow the default redirect behavior
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    // Check if this is an API request
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Path.StartsWithSegments("/admin") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    
                    // For browser requests, allow the default redirect behavior
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

        // Google Authentication Configuration (conditionally register only when configured)
        var googleAuthSection = configuration.GetSection("Authentication:Google");
        var googleClientId = googleAuthSection["ClientId"];
        var googleClientSecret = googleAuthSection["ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authenticationBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = googleClientId;
                googleOptions.ClientSecret = googleClientSecret;
                googleOptions.SaveTokens = true;

                // Request additional scopes for user information
                googleOptions.Scope.Add("profile");
                googleOptions.Scope.Add("email");

                // Map claims from Google to ASP.NET Identity claims
                googleOptions.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
                googleOptions.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");
            });
        }
        services.AddAuthorization();

        // Infrastructure Services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOpenIddictService, OpenIddictService>();
        services.AddScoped<IAuthorizationProcessingService, AuthorizationProcessingService>();
        services.AddScoped<ITokenExchangeService, TokenExchangeService>();
        services.AddScoped<IInvitationEmailService, InvitationEmailService>();
        services.AddScoped<IOrganizationUserLicenseProjectionService, OrganizationUserLicenseProjectionService>();

        // External Authentication Services
        services.AddScoped<IExternalAuthenticationService, GoogleAuthenticationService>();

        // OAuth Services 
        services.AddSingleton<IOAuthStateService, OAuthStateService>();
        services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
        services.AddHttpClient("GoogleOAuth");
        // Configure shared email service with mapped configuration
        // Configure email settings using strongly-typed configuration
        services.Configure<global::Infrastructure.Configurations.SMTPEmailSetting>(configuration.GetSection("EmailSettings"));

        var check = configuration.GetSection("EmailSettings");

        // Register shared email services with concrete implementation
        services.AddSingleton<Contracts.Configurations.IEmailSettings>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<global::Infrastructure.Configurations.SMTPEmailSetting>>().Value;
            return settings; // SMTPEmailSetting implements IEmailSettings
        });

        // Fix: Register IOptions<IEmailSettings> for shared EmailService constructor
        services.AddSingleton<IOptions<Contracts.Configurations.IEmailSettings>>(provider =>
        {
            var concreteSettings = provider.GetRequiredService<IOptions<global::Infrastructure.Configurations.SMTPEmailSetting>>();
            return Options.Create<Contracts.Configurations.IEmailSettings>(concreteSettings.Value);
        });

        services.AddSingleton<Contracts.Abstractions.Services.IEmailTemplateService, global::Infrastructure.Services.EmailTemplateService>();
        services.AddScoped<Contracts.Abstractions.Services.IEmailService, global::Infrastructure.Services.EmailService>();

        services.AddScoped<IdentityIEmailService, IdentityEmailServiceAdapter>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobRoleRepository, JobRoleRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();

        // Bulk provisioning repositories
        services.AddScoped<IBulkImportJobRepository, BulkImportJobRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
        services.AddScoped<IOrganizationUserLicenseReadRepository, OrganizationUserLicenseReadRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();

        // CSV Parser Service
        services.AddScoped<ICsvParserService, CsvParserService>();

        // MassTransit Configuration for Event-Driven Architecture
        if (enableConsumers)
        {
            AddMassTransitConfiguration(services, configuration, enableConsumers);
        }

        // Background Services
        if (enableConsumers)
        {
            services.AddHostedService<ExpiredInvitationCleanupService>();
        }

        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<IOpenIddictConfigurationService, OpenIddictConfigurationService>();
        services.AddPollyResilience(configuration);

        // gRPC Clients
        AddGrpcClients(services, configuration);
        return services;
    }

    private static void AddMassTransitConfiguration(
        IServiceCollection services,
        IConfiguration configuration,
        bool enableConsumers)
    {
        services.AddMassTransit(x =>
        {

            if (enableConsumers)
            {
                x.AddConsumer<BulkInviteRequestedEventConsumer>();
                x.AddConsumer<InvitationAcceptedEventConsumer>();
                x.AddConsumer<BulkImportJobCompletedEventConsumer>();

                x.AddConsumer<LicenseAssignmentActivatedEventConsumer>();
                x.AddConsumer<LicenseAssignmentRevokedEventConsumer>();
                x.AddConsumer<LicenseAssignmentCreatedEventConsumer>();
                x.AddConsumer<LicenseAssignmentDeletedEventConsumer>();
                x.AddConsumer<SubscriptionCancelledEventConsumer>();
            }

            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.QueryDelay = TimeSpan.FromSeconds(10);
                o.UsePostgres();
                o.UseBusOutbox();
            });

            // Configure RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("messaging")
                    ?? configuration["RabbitMq:Url"]
                    ?? configuration["RabbitMQ:ConnectionString"];

                if (!string.IsNullOrEmpty(connectionString))
                {
                    // Use connection string (Aspire or full URI format)
                    cfg.Host(new Uri(connectionString));
                }
                else
                {
                    // Fallback to individual configuration values (backward compatibility)
                    var rabbitMqSection = configuration.GetSection("RabbitMQ");
                    var host = rabbitMqSection["Host"] ?? "localhost";
                    var port = rabbitMqSection.GetValue<ushort>("Port", 5672);
                    var username = rabbitMqSection["Username"] ?? "guest";
                    var password = rabbitMqSection["Password"] ?? "guest";
                    var virtualHost = rabbitMqSection["VirtualHost"] ?? "/";

                    cfg.Host(host, port, virtualHost, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });
                }

                // Configure receive endpoints for each consumer only if consumers are enabled
                if (enableConsumers)
                {
                    cfg.ReceiveEndpoint("bulk-invite-requested", e =>
                    {
                        e.ConfigureConsumer<BulkInviteRequestedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(2),
                            maxInterval: TimeSpan.FromSeconds(30),
                            intervalDelta: TimeSpan.FromSeconds(2)));

                        e.PrefetchCount = 10;
                        e.ConcurrentMessageLimit = 5;
                    });

                    cfg.ReceiveEndpoint("invitation-accepted", e =>
                    {
                        e.ConfigureConsumer<InvitationAcceptedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });

                    cfg.ReceiveEndpoint("bulk-import-job-completed", e =>
                    {
                        e.ConfigureConsumer<BulkImportJobCompletedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 10;
                        e.ConcurrentMessageLimit = 5;
                    });

                    cfg.ReceiveEndpoint("license-assignment-activated", e =>
                    {
                        e.ConfigureConsumer<LicenseAssignmentActivatedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });

                    cfg.ReceiveEndpoint("license-assignment-revoked", e =>
                    {
                        e.ConfigureConsumer<LicenseAssignmentRevokedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });

                    cfg.ReceiveEndpoint("license-assignment-created", e =>
                    {
                        e.ConfigureConsumer<LicenseAssignmentCreatedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });

                    cfg.ReceiveEndpoint("license-assignment-deleted", e =>
                    {
                        e.ConfigureConsumer<LicenseAssignmentDeletedEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });

                    cfg.ReceiveEndpoint("subscription-deleted", e =>
                    {
                        e.ConfigureConsumer<SubscriptionCancelledEventConsumer>(context);

                        e.UseMessageRetry(r => r.Exponential(
                            retryLimit: 3,
                            minInterval: TimeSpan.FromSeconds(1),
                            maxInterval: TimeSpan.FromSeconds(10),
                            intervalDelta: TimeSpan.FromSeconds(1)));

                        e.PrefetchCount = 20;
                        e.ConcurrentMessageLimit = 10;
                    });
                }

                // Global retry
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)));
            });
        });
    }

    private static X509Certificate2? LoadCertificateFromBase64(
        IConfiguration configuration,
        string base64Key,
        string? passwordKey,
        bool requireProduction
    )
    {
        if (requireProduction)
        {
            var env = configuration["DOTNET_ENVIRONMENT"] ?? configuration["ASPNETCORE_ENVIRONMENT"];
            if (!string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        var base64 = configuration[base64Key];
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        try
        {
            var rawData = Convert.FromBase64String(base64);
            var password = string.IsNullOrWhiteSpace(passwordKey) ? null : configuration[passwordKey];

            return new X509Certificate2(
                rawData,
                password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
            );
        }
        catch
        {
            return null;
        }
    }

    private static X509Certificate2? LoadCertificateFromKeyVault(IConfiguration configuration)
    {
        var kvUrl = configuration["KeyVaultUrl"];
        var kvCertName = configuration["KeyVaultCertName"];

        if (string.IsNullOrWhiteSpace(kvUrl) || string.IsNullOrWhiteSpace(kvCertName))
        {
            return null;
        }

        // Get Managed Identity Client ID from configuration (outside try block for error logging)
        // Priority: IdentityClientId > AZURE_CLIENT_ID (environment variable)
        var managedIdentityClientId = configuration["IdentityClientId"];
        if (string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        }

        try
        {
            var isProduction = string.Equals(
                configuration["DOTNET_ENVIRONMENT"]
                ?? configuration["ASPNETCORE_ENVIRONMENT"],
                "Production",
                StringComparison.OrdinalIgnoreCase
            );

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
            var logger = loggerFactory.CreateLogger("Identity.Infrastructure.DependencyInjection");
            var credentialOptions = new DefaultAzureCredentialOptions();
            
            var currentEnv = configuration["DOTNET_ENVIRONMENT"] 
                ?? configuration["ASPNETCORE_ENVIRONMENT"] 
                ?? "Unknown";
            logger.LogInformation(
                "Key Vault authentication - Environment: {Environment}, IsProduction: {IsProduction}, " +
                "ManagedIdentityClientId configured: {HasClientId}",
                currentEnv,
                isProduction,
                !string.IsNullOrWhiteSpace(managedIdentityClientId)
            );
            
            if (isProduction)
            {
                if (string.IsNullOrWhiteSpace(managedIdentityClientId))
                {
                    var errorMessage = 
                        "Production environment requires Managed Identity Client ID for Key Vault access. " +
                        "Set 'IdentityClientId' configuration or 'AZURE_CLIENT_ID' environment variable. " +
                        $"Current environment: {currentEnv}";
                    
                    logger.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
                credentialOptions.ExcludeInteractiveBrowserCredential = true;
                credentialOptions.ExcludeAzureCliCredential = true;
                credentialOptions.ExcludeVisualStudioCredential = true;
                credentialOptions.ExcludeVisualStudioCodeCredential = true;
                credentialOptions.ExcludeEnvironmentCredential = true;
                
                logger.LogInformation(
                    "Production mode: Using Managed Identity with Client ID: {ClientId} for Key Vault access. " +
                    "All user-based credentials are excluded.",
                    managedIdentityClientId
                );
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
                {
                    credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
                    
                    credentialOptions.ExcludeInteractiveBrowserCredential = true;
                    credentialOptions.ExcludeAzureCliCredential = true;
                    credentialOptions.ExcludeVisualStudioCredential = true;
                    credentialOptions.ExcludeVisualStudioCodeCredential = true;
                    
                    logger.LogInformation(
                        "Development mode: Using Managed Identity with Client ID: {ClientId} for Key Vault access. " +
                        "User-based credentials (Azure CLI, VS, etc.) are excluded to ensure Managed Identity is used.",
                        managedIdentityClientId
                    );
                }
                else
                {
                    logger.LogWarning(
                        "Development mode: No Managed Identity Client ID configured. " +
                        "DefaultAzureCredential will use credential chain (Azure CLI, VS, etc.). " +
                        "This may cause 403 errors if the logged-in user doesn't have Key Vault permissions."
                    );
                }
            }

            var credential = new DefaultAzureCredential(credentialOptions);
            var secretClient = new SecretClient(new Uri(kvUrl), credential);
            
            
            logger.LogInformation(
                "Attempting to access Key Vault: {KeyVaultUrl}, Secret: {SecretName}. " +
                "Managed Identity Client ID: {ClientId}",
                kvUrl,
                kvCertName,
                managedIdentityClientId ?? "Not configured (will use credential chain)"
            );

            logger.LogWarning("Fetching signing certificate from Key Vault secret {SecretName}", kvCertName);

            try
            {
                var secret = secretClient.GetSecret(kvCertName);

                var pfxBytes = Convert.FromBase64String(secret.Value.Value);
                logger.LogWarning("Loaded signing certificate from Key Vault secret {SecretName}", kvCertName);

                return new X509Certificate2(
                    pfxBytes,
                    (string?)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
                );
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                logger.LogError(
                    ex,
                    "1. The Managed Identity Client ID '{ClientId}' is incorrect, OR\n" +
                    "2. The Managed Identity doesn't have 'Key Vault Secrets User' role on the Key Vault.\n" +
                    "Error details: {ErrorCode} - {Message}\n" +
                    "To verify the correct Client ID, run: az identity show --name <identity-name> --resource-group <rg> --query clientId\n" +
                    "To check role assignments, run: az role assignment list --scope <key-vault-resource-id> --assignee <client-id>",
                    managedIdentityClientId ?? "Not configured",
                    ex.ErrorCode,
                    ex.Message
                );
                throw;
            }
        }
        catch (Exception ex)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
            var logger = loggerFactory.CreateLogger("Identity.Infrastructure.DependencyInjection");
            logger.LogError(
                ex,
                "Failed to load certificate from Key Vault. URL: {KeyVaultUrl}, Secret: {SecretName}, " +
                "Managed Identity Client ID: {ClientId}",
                kvUrl,
                kvCertName,
                managedIdentityClientId ?? "Not configured"
            );
            return null;
        }
    }
    private static void AddGrpcClients(IServiceCollection services, IConfiguration configuration)
    {
        var orderServiceAddress = configuration["GrpcServices:OrderService:Address"]
            ?? "https://localhost:7006";

        services.AddGrpcClient<GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient>(options =>
        {
            options.Address = new Uri(orderServiceAddress);
        });

        services.AddGrpcClient<GrpcOrganizationService.GrpcOrganizationServiceClient>(options =>
        {
            options.Address = new Uri(orderServiceAddress);
        });

        services.AddScoped<IOrderLicenseService,
           OrderLicenseService>();
    }

    private static void EnsureDbContextRegistered(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dbContextDescriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ApplicationDbContext)
        );
        var dbContextOptionsDescriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
        );

        if (dbContextDescriptor == null && dbContextOptionsDescriptor == null)
        {
            var connectionString =
                configuration.GetConnectionString("stemifyidentity")
    ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No database connection string found. Please configure 'DefaultConnection' or 'stemifyidentity' connectionstring."
                );

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly("Identity.Infrastructure");
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null
                        );
                    }
                );

                options.UseOpenIddict<Guid>();

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
        }
    }
}
