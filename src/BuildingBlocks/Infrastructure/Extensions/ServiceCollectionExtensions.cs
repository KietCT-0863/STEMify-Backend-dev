using Contracts.Abstractions.Services;
using Contracts.Configurations;
using Infrastructure.Configurations;
using Infrastructure.HealthChecks;
using Infrastructure.Idempotency;
using Infrastructure.Resilience;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPollyResilience(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<PollyResilienceOptions>(configuration.GetSection("PollyResilience"));

        services.AddSingleton<IPollyResilienceService, PollyResilienceService>();

        return services;
    }

    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddDatabaseHealthCheck<TDbContext>(
        this IServiceCollection services
    )
        where TDbContext : DbContext
    {
        services.AddScoped<IHealthCheckService, DatabaseHealthCheck<TDbContext>>();

        return services;
    }

    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        // Dang dùng InMemory implementation
        services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();

        return services;
    }

    /// <summary>
    /// Add email service with SMTP configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="sectionName">Configuration section name (default: "EmailSettings")</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SMTPEmailSetting.SectionName)
    {
        // Configure email settings
        services.Configure<IEmailSettings>(configuration.GetSection(sectionName));

        // Validate configuration on startup
        services.AddSingleton<IValidateOptions<IEmailSettings>, ValidateEmailSettings>();

        // Register email template service
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Register email service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    /// <summary>
    /// Add email service with custom settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureSettings">Action to configure email settings</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        Action<SMTPEmailSetting> configureSettings)
    {
        var emailSettings = new SMTPEmailSetting();
        configureSettings(emailSettings);

        // Validate settings
        emailSettings.Validate();

        // Register as singleton
        services.AddSingleton<IEmailSettings>(emailSettings);

        // Register email template service
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        // Register email service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddPollyResilience(configuration);
        services.AddCustomHealthChecks();
        services.AddIdempotency();

        // Add email service if configuration exists
        if (configuration.GetSection(SMTPEmailSetting.SectionName).Exists())
        {
            services.AddEmailService(configuration);
        }

        return services;
    }
}
