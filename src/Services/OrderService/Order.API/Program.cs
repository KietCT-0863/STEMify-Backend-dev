using Infrastructure.Middlewares;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Order.API.Extensions;
using Order.API.Services;
using Order.Application.Extensions;
using Order.Infrastructure.Extensions;
using Order.Infrastructure.Persistence;

namespace Order.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        
        // Configure Kestrel for dual ports (HTTP + gRPC)
        if (!builder.Environment.IsDevelopment())
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                var httpPort = builder.Configuration.GetValue<int?>("PORT")
                    ?? builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
                    ?? 8085;
                
                var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5085;
                
                // Port for HTTP/1.1 + HTTP/2 (REST API, JSON transcoding from gateway)
                options.ListenAnyIP(httpPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                });
                
                // Dedicated port for gRPC (HTTP/2 only) - internal service-to-service calls
                options.ListenAnyIP(grpcPort, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
                
                Console.WriteLine($"Order API - HTTP port {httpPort}, gRPC port {grpcPort}");
            });
        }

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

        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddApiServices(builder.Configuration);

        builder.Services.AddGrpcSwagger();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Order Service API",
                Version = "v1",
                Description = "gRPC service with HTTP/JSON transcoding and REST API"
            });
        });

        // Add services to the container.
        // Removed .AddJsonTranscoding() to avoid route conflicts with REST API controllers
        builder.Services.AddGrpc();
        
        // Add REST API controllers
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        // Add CORS
        var clientApp = builder.Configuration["ClientApp"] ?? "http://localhost:3000";
        var allowedOrigins = clientApp.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(origin => origin.Trim())
            .ToArray();
            
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1");
            c.RoutePrefix = "swagger";
        });

        app.MapDefaultEndpoints();

        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");
        
        // Use CORS
        app.UseCors();

        // middleware to handle errors and return proper gRPC status codes
        app.UseMiddleware<ErrorWrappingMiddleware>();

        app.MapGrpcService<ContractGrpcService>();
        app.MapGrpcService<OrganizationGrpcService>();
        app.MapGrpcService<OrganizationSubscriptionOrderGrpcService>();
        app.MapGrpcService<OrganizationTypeGrpcService>();
        app.MapGrpcService<LicenseAssignmentGrpcService>();
        app.MapGrpcService<DashboardGrpcService>();
        
        // Map REST API controllers
        app.MapControllers();

        //app.UseAuthentication();
        //app.UseAuthorization();

        // Configure the HTTP request pipeline.
        app.MapGet(
            "/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909"
        );

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var orderDbContext = services.GetRequiredService<OrderDbContext>();
            var orderDbSeed = scope.ServiceProvider.GetRequiredService<OrderDbContextSeed>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                await orderDbContext.Database.MigrateAsync();

                logger.LogInformation("Order API - Database migrations completed");

                // Seed the database with data
                logger.LogInformation("Order API - Development environment detected, seeding sample data...");
                await orderDbSeed.SeedAsync();
                logger.LogInformation("Order API - Database seeding completed");

                logger.LogInformation("Order API - Database initialization successful!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A message occurred during migration");
            }
        }
        await app.RunAsync();
    }
}