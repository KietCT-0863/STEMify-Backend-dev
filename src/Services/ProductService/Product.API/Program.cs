using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Product.API.Extensions;
using Product.API.Services;
using Product.Application.Extensions;
using Product.Infrastructure.Extensions;
using Product.Infrastructure.Persistence;

namespace Product.API;

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
                    ?? 8084;
                
                var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5084;
                
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
                
                Console.WriteLine($"Product API - HTTP port {httpPort}, gRPC port {grpcPort}");
            });
        }

        builder.AddNpgsqlDbContext<ProductDbContext>(
            "stemifyproduct",
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
                Title = "Product Service API",
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
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service v1");
            c.RoutePrefix = "swagger";
        });

        app.MapDefaultEndpoints();

        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");

        app.MapGrpcService<KitProductGrpcService>();
        app.MapGrpcService<ComponentGrpcService>();
        app.MapGrpcService<KitComponentGrpcService>();
        app.MapGrpcService<PlanGrpcService>();
        app.MapGrpcService<PlanBillingCycleGrpcService>();
        app.MapGrpcService<ProductGrpcService>();

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
            var productDbContext = services.GetRequiredService<ProductDbContext>();
            var productDbSeed = scope.ServiceProvider.GetRequiredService<ProductDbContextSeed>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                await productDbContext.Database.MigrateAsync(); 
                
                logger.LogInformation("Product API - Database migrations completed");

                // Seed the database with data
                logger.LogInformation("Product API - Development environment detected, seeding sample data...");
                await productDbSeed.SeedAsync();
                logger.LogInformation("Product API - Database seeding completed");

                logger.LogInformation("Product API - Database initialization successful!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A message occurred during migration");
            }
        }
        await app.RunAsync();
    }
}