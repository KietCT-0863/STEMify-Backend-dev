using Hangfire;
using Hangfire.API.Extensions;
using Hangfire.API.Jobs;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application.Extensions;
using Order.Infrastructure.Extensions;
using Order.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();

// Configure additional console logging for development (works alongside OpenTelemetry)
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole(options =>
    {
        options.FormatterName = "simple";
    });
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Order Service dependencies (Application + Infrastructure layers)
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddServsices(builder.Configuration);

// Add Order DbContext for subscription lifecycle workflows (reads from Order database)
builder.AddNpgsqlDbContext<OrderDbContext>(
    "stemifyorder",
    configureSettings: settings =>
    {
        settings.CommandTimeout = 30;
    },
    configureDbContextOptions: options =>
    {
        options.UseNpgsql(npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
        });
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    }
);

// Add Hangfire services with PostgreSQL storage (separate database for Hangfire jobs)


var app = builder.Build();

// Map Aspire default endpoints (health, metrics)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Configure Hangfire Dashboard with basic authentication
var dashboardUsername = builder.Configuration["HangfireDashboard:Username"] ?? "admin";
var dashboardPassword = builder.Configuration["HangfireDashboard:Password"] ?? "stemify@2025";

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization =
    [
        new HangfireCustomBasicAuthenticationFilter
        {
            User = dashboardUsername,
            Pass = dashboardPassword
        }
    ],
    DashboardTitle = "STEMify Background Jobs",
    StatsPollingInterval = 5000
});

// Register recurring jobs on startup
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        recurringJobManager.AddOrUpdate<SubscriptionLifecycleJob>(
            "activate-pending-subscriptions",
            job => job.ActivatePendingSubscriptionsAsync(),
           Cron.Hourly(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            }
            );

        recurringJobManager.AddOrUpdate<SubscriptionLifecycleJob>(
            "expire-ended-subscriptions",
            job => job.ExpireEndedSubscriptionsAsync(),
            Cron.Hourly(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        recurringJobManager.AddOrUpdate<SubscriptionLifecycleJob>(
            "check-expiring-subscriptions-30d",
            job => job.CheckExpiringSubscriptions30DaysAsync(),
            Cron.Daily(0,1),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        recurringJobManager.AddOrUpdate<SubscriptionLifecycleJob>(
            "check-expiring-subscriptions-7d",
            job => job.CheckExpiringSubscriptions7DaysAsync(),
            Cron.Daily(0,1),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        recurringJobManager.AddOrUpdate<SubscriptionLifecycleJob>(
            "check-expiring-subscriptions-1d",
            job => job.CheckExpiringSubscriptions1DayAsync(),
            Cron.Daily(0, 1),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        recurringJobManager.AddOrUpdate<ScheduledInvitationEmailJob>(
            "send-scheduled-invitation-emails",
            job => job.SendScheduledInvitationsAsync(CancellationToken.None),
            Cron.Daily(9, 0), // 9:00 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to register recurring jobs");
    }
}

app.Run();

// Basic Authentication Filter for Hangfire Dashboard
public class HangfireCustomBasicAuthenticationFilter : IDashboardAuthorizationFilter
{
    public required string User { get; set; }
    public required string Pass { get; set; }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(header) && header.StartsWith("Basic "))
        {
            var encodedAuth = header["Basic ".Length..].Trim();
            var decodedAuth = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedAuth));
            var credentials = decodedAuth.Split(':');

            if (credentials.Length == 2 && credentials[0] == User && credentials[1] == Pass)
            {
                return true;
            }
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = 401;
        return false;
    }
}
