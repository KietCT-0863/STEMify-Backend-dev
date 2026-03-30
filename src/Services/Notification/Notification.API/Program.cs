using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Notification.API.Extensions;
using Notification.API.Services;
using Notification.Application.Common.Hubs;
using Notification.Application.Extensions;
using Notification.Infrastructure.Extensions;
using Notification.Infrastructure.Persistence;

namespace Notification.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<NotificationDbContext>(
            "stemifynotification",
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

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddApiServices(builder.Configuration);

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumersFromNamespaceContaining<Consumers.CourseCreatedConsumer>();

            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notification", false));

            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    cfg.Host(
                        new Uri(
                            builder.Configuration.GetConnectionString("messaging") ??
                            builder.Configuration["RabbitMq:Url"]
                                ?? "amqp://guest:guest@localhost:5672"
                        )
                    );

                    cfg.ReceiveEndpoint(
                        "notification-course-created",
                        e =>
                        {
                            // Configure retry policy: retry 5 times with a 5 second interval if message processing fails
                            e.UseMessageRetry(r => r.Interval(5, 5));
                            // Attach the CourseCreatedConsumer to the endpoint to handle incoming messages
                            e.ConfigureConsumer<Consumers.CourseCreatedConsumer>(context);
                        }
                    );

                    cfg.ReceiveEndpoint(
                        "notification-enrollment-created",
                        e =>
                        {
                            // Configure retry policy: retry 5 times with a 5 second interval if message processing fails
                            e.UseMessageRetry(r => r.Interval(5, 5));
                            // Attach the EnrollmentCreatedConsumer to the endpoint to handle incoming messages
                            e.ConfigureConsumer<Consumers.EnrollmentCreatedConsumer>(context);
                        }
                    );

                    cfg.ReceiveEndpoint(
                        "notification-subscription-expiry-warning",
                        e =>
                        {
                            // Configure retry policy: retry 5 times with a 5 second interval if message processing fails
                            e.UseMessageRetry(r => r.Interval(5, 5));
                            // Attach the SubscriptionExpiryWarningConsumer to the endpoint to handle incoming messages
                            e.ConfigureConsumer<Consumers.SubscriptionExpiryWarningConsumer>(context);
                        }
                    );

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        builder.Services.AddGrpcSwagger();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Notification Service API",
                Version = "v1",
                Description = "gRPC service with HTTP/JSON transcoding"
            });
        });

        // Add services to the container.
        builder.Services.AddGrpc().AddJsonTranscoding();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service v1");
            c.RoutePrefix = "swagger";
        });

        app.MapDefaultEndpoints();

        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");

        app.MapGrpcService<NotificationGrpcService>();

        app.MapHub<NotificationHub>("/hub/notifications");
        app.UseCors("customPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        // Configure the HTTP request pipeline.
        app.MapGet(
            "/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
        );

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var notificationDbContext = services.GetRequiredService<NotificationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                await notificationDbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A message occurred during migration");
            }
        }
        await app.RunAsync();
    }
}
