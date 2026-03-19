using Common.Logging.Enrichers;
using Common.Logging.Extensions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Extensions;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Identity.Web.Middlewares;
using Identity.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Identity.Web.Extensions;

/// <summary>
/// Extension methods for WebApplicationBuilder to configure services
/// </summary>
public static class ProgramExtensions
{
    /// <summary>
    /// Configure all application services
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure Serilog
        builder.AddCustomSerilog();

        // Add service defaults
        builder.AddServiceDefaults();

        // Configure database
        builder.ConfigureDatabase();
        
        // Configure ForwardedHeaders to handle HTTPS properly behind reverse proxy
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | 
                ForwardedHeaders.XForwardedProto | 
                ForwardedHeaders.XForwardedHost;
            
            // Trust all proxies (Cloudflare, API Gateway, etc.)
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            
            // Required for proper HTTPS scheme detection
            options.ForwardLimit = null;
        });
            // Configure application services
        builder.Services.AddApplicationServices().AddInfrastructureServices(builder.Configuration, enableConsumers: false);

        builder.ConfigureDataProtection();

        // Configure web services
        builder.ConfigureWebServices();

        // Configure health checks
        builder.ConfigureHealthChecks();

        return builder;
    }

    /// <summary>
    /// Configure database context
    /// </summary>
    private static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        // DEBUG: Log connection string for debugging
        var connectionString = builder.Configuration.GetConnectionString("stemifyidentity");
        Console.WriteLine($"[DEBUG] Identity Web - Aspire Connection String: {connectionString}");

        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            "stemifyidentity",
            configureSettings: settings =>
            {
                settings.CommandTimeout = 30;
            },
            configureDbContextOptions: options =>
            {
                options.UseNpgsql(npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("Identity.Infrastructure");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null
                    );
                });

                options.UseOpenIddict<Guid>();

                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            }
        );
    }

    private static void ConfigureDataProtection(this WebApplicationBuilder builder)
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var dataProtectionBuilder = builder.Services.AddDataProtection()
            .SetApplicationName("Stemify.Identity");

        if (!isDevelopment)
        {
            dataProtectionBuilder.PersistKeysToDbContext<ApplicationDbContext>();
        }
        else
        {
            var dataProtectionKeysPath = Environment.GetEnvironmentVariable("ASPNETCORE_DATA_PROTECTION_KEYS_PATH") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stemify", "DataProtectionKeys");
            var keysDirectory = new DirectoryInfo(dataProtectionKeysPath);

            if (!keysDirectory.Exists)
            {
                try
                {
                    keysDirectory.Create();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not create Data Protection keys directory at {dataProtectionKeysPath}: {ex.Message}");
                }
            }

            dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
        }
    }

    /// <summary>
    /// Configure web-specific services
    /// </summary>
    private static void ConfigureWebServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHybridCache();
        builder.Services.TryAddSingleton<RevokeAccessTokenMiddleware>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllersWithViews();
        
    
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        // Register database initialization services first
        var dbInitService = new DatabaseInitializationService();
        builder.Services.AddSingleton(dbInitService);
        builder.Services.AddSingleton<IDatabaseInitializationService>(dbInitService);
        builder.Services.AddSingleton<DatabaseInitializationHealthCheck>();
        builder.Services.AddHostedService<DatabaseInitializationHostedService>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "customPolicy",
                b =>
                {
                    b.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(builder.Configuration["ClientApp"]!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            );
        });
    }

    /// <summary>
    /// Configure health checks
    /// </summary>
    private static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database")
            .AddCheck<DatabaseInitializationHealthCheck>("database-initialization");
    }

    /// <summary>
    /// Configure middleware pipeline
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();

        app.Use(
            async (context, next) =>
            {
                var scheme = context.Request.Scheme;
                var host = context.Request.Host;
                var pathBase = context.Request.PathBase;
                var headers = context.Request.Headers;

                await next();
            }
        );
        // Map default endpoints
        app.MapDefaultEndpoints();

        // Configure environment-specific pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Configure Serilog request logging with custom level filter
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = LogEnricher.GetLogLevel;
            options.EnrichDiagnosticContext = LogEnricher.EnrichFromRequest;
        });

        // Forward headers

        app.UseCors("customPolicy");
        // Configure middleware
        app.UseMiddleware<RevokeAccessTokenMiddleware>();
        app.UseRevokeAccessTokenMiddleware();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession(); 
        app.UseAuthentication();
        app.UseAuthorization();

        // Configure routing
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapControllers();

        // Configure health check endpoints
        app.MapHealthChecks("/health");
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = check =>
                    check.Name == "database-initialization" || check.Name == "database",
            }
        );
        app.MapHealthChecks("/health/live");

        return app;
    }
}
