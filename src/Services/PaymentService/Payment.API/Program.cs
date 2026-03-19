using Microsoft.EntityFrameworkCore;
using Payment.API.Extensions;
using Payment.Application.Extensions;
using Payment.Infrastructure.Extensions;
using Payment.Infrastructure.Persistence;

namespace Payment.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults (Aspire)
        builder.AddServiceDefaults();

        // Add database with Aspire
        builder.AddNpgsqlDbContext<PaymentDbContext>(
            "stemifypayment",
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

        // Add layer services
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddApiServices(builder.Configuration);

        // Add gRPC services
        builder.Services.AddGrpc().AddJsonTranscoding();
        builder.Services.AddGrpcReflection();

        // Add gRPC Swagger
        builder.Services.AddGrpcSwagger();

        var app = builder.Build();

        // Configure HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");

        // Authentication & Authorization (commented out until Identity integration)
        // app.UseAuthentication();
        // app.UseAuthorization();

        app.MapControllers();

        // Default endpoints from Aspire
        app.MapDefaultEndpoints();

        // Health checks
        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");

        // gRPC reflection (for tools like grpcurl)
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        // Default route
        app.MapGet("/", () => Results.Ok(new
        {
            service = "Payment Service",
            version = "1.0.0",
            status = "Running",
            endpoints = new
            {
                swagger = "/swagger",
                health = "/health/ready",
                api = "/api/payments"
            }
        }));

        // Run migrations
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var paymentDbContext = services.GetRequiredService<PaymentDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting database migration...");
                await paymentDbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during migration");
            }
        }

        await app.RunAsync();
    }
}
