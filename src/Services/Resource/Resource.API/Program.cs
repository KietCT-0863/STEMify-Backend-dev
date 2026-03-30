using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Resource.API.Extensions;
using Resource.API.Services;
using Resource.Application.Extensions;
using Resource.Infrastructure.Extensions;
using Resource.Infrastructure.Persistence;

namespace Resource.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        if (!builder.Environment.IsDevelopment())
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                var httpPort = builder.Configuration.GetValue<int?>("PORT")
                    ?? builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
                    ?? 8082;
                
                var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5082;

                // Port for HTTP/1.1 + HTTP/2 (REST API, JSON transcoding from gateway)
                options.ListenAnyIP(
                    httpPort,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        listenOptions.UseConnectionLogging();
                    }
                );
                
                // Dedicated port for gRPC (HTTP/2 only) - internal service-to-service calls
                options.ListenAnyIP(
                    grpcPort,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        listenOptions.UseConnectionLogging();
                    }
                );
            });
        }

        // Apply limits globally (including Development environment where Docker runs)
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxConcurrentConnections = 1000;
            options.Limits.MaxConcurrentUpgradedConnections = 100;
            options.Limits.MaxRequestBodySize = 250_000_000; // 250 MB
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        });

        // Enhanced logging configuration for debugging gRPC issues
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Enable detailed logging for gRPC and HTTP
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Information);
        builder.Logging.AddFilter("Grpc", LogLevel.Debug);
        builder.Logging.AddFilter("Resource.API.Services", LogLevel.Information);
        builder.Logging.AddFilter("Resource.API", LogLevel.Information);
        builder.Logging.AddFilter("Resource.Application", LogLevel.Information);

        // Ensure minimum log level is set correctly
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        Console.WriteLine(
            $"Resource API - Enhanced logging configured for environment: {builder.Environment.EnvironmentName}"
        );
        Console.WriteLine($"Resource API - Kestrel dual protocol: ENABLED for all environments");
        var configuredPort = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
            ?? builder.Configuration.GetValue<string>("PORT")
            ?? builder.Configuration.GetValue<string>("ASPNETCORE_HTTP_PORTS")
            ?? "8082";
        Console.WriteLine($"Resource API - Port: {configuredPort}");
        Console.WriteLine(
            $"Resource API - Log Level: {builder.Configuration.GetValue("Logging:LogLevel:Default", "Information")}"
        );

        builder.AddNpgsqlDbContext<ResourceDbContext>(
            "stemifyresource",
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
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddConsole();

        // Add Controllers support for REST API with streaming
        builder.Services.AddControllers();

        // Configure gRPC services with enhanced logging

        builder.Services.AddGrpcSwagger();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Resource Service API",
                Version = "v1",
                Description = "gRPC service with HTTP/JSON transcoding"
            });
        });

        builder
            .Services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                options.MaxReceiveMessageSize = 250 * 1024 * 1024; // 250 MB
                options.MaxSendMessageSize = 250 * 1024 * 1024; // 250 MB
            })
            .AddJsonTranscoding();

        var app = builder.Build();

        app.UseCors("customPolicy");
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resource Service v1");
            c.RoutePrefix = "swagger";
        });

        app.MapDefaultEndpoints();

        // Add request logging middleware FIRST to capture all requests
        app.Use(
            async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                // logger.LogInformation(
                //     "Incoming Request - Method: {Method}, Path: {Path}, Protocol: {Protocol}, ContentType: {ContentType}, UserAgent: {UserAgent}",
                //     context.Request.Method,
                //     context.Request.Path,
                //     context.Request.Protocol,
                //     context.Request.ContentType ?? "null",
                //     context.Request.Headers.UserAgent.ToString() ?? "null"
                // );

                // Log important headers for gRPC debugging
                if (context.Request.Headers.ContainsKey("grpc-encoding"))
                {
                    logger.LogInformation(
                        "gRPC headers detected - Encoding: {Encoding}, Timeout: {Timeout}",
                        context.Request.Headers["grpc-encoding"],
                        context.Request.Headers["grpc-timeout"]
                    );
                }

                // Enhanced protocol detection for debugging
                var isGrpcRequest =
                    context.Request.Path.StartsWithSegments("/resource.")
                    || context.Request.ContentType?.StartsWith("application/grpc") == true;

                if (isGrpcRequest && context.Request.Protocol != "HTTP/2")
                {
                    logger.LogWarning(
                        "gRPC request detected but protocol is {Protocol}, expected HTTP/2",
                        context.Request.Protocol
                    );

                    // Log all headers for debugging
                    foreach (var header in context.Request.Headers)
                    {
                        logger.LogInformation(
                            " Header: {Key} = {Value}",
                            header.Key,
                            header.Value
                        );
                    }
                }

                try
                {
                    await next();

                    logger.LogInformation(
                        "📤 Request completed - Status: {StatusCode}, Protocol: {Protocol}",
                        context.Response.StatusCode,
                        context.Request.Protocol
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Request failed - Path: {Path}, Protocol: {Protocol}",
                        context.Request.Path,
                        context.Request.Protocol
                    );
                    throw;
                }
            }
        );

        // Add protocol validation middleware for gRPC endpoints
        app.Use(
            async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                // Check if this is a gRPC request
                var isGrpcRequest = context.Request.Path.StartsWithSegments("/resource.");

                if (isGrpcRequest)
                {
                    // For gRPC requests, ensure we're using HTTP/2
                    if (context.Request.Protocol != "HTTP/2")
                    {
                        logger.LogError(
                            "gRPC request received with wrong protocol: {Protocol}. Expected HTTP/2",
                            context.Request.Protocol
                        );

                        // Return a proper gRPC error response
                        context.Response.StatusCode = 426; // Upgrade Required
                        context.Response.Headers.Add("Upgrade", "h2c");
                        context.Response.Headers.Add("Connection", "Upgrade");
                        await context.Response.WriteAsync("HTTP/2 required for gRPC requests");
                        return;
                    }

                    logger.LogInformation(
                        "gRPC request validated - Protocol: {Protocol}",
                        context.Request.Protocol
                    );
                }

                await next();
            }
        );

        // Configure the HTTP request pipeline.
        // Add gRPC services - these will automatically handle HTTP/2 requests
        app.MapGrpcService<CourseGrpcService>();
        app.MapGrpcService<AgeRangeGrpcService>();
        app.MapGrpcService<CategoryGrpcService>();
        app.MapGrpcService<SkillGrpcService>();
        app.MapGrpcService<StandardGrpcService>();
        app.MapGrpcService<LessonGrpcService>();
        app.MapGrpcService<ContentGrpcService>();
        app.MapGrpcService<AnswerGrpcService>();
        app.MapGrpcService<QuestionGrpcService>();
        app.MapGrpcService<SectionGrpcService>();
        app.MapGrpcService<QuizGrpcService>();
        app.MapGrpcService<CurriculumGrpcService>();
        app.MapGrpcService<CourseLearningOutcomeGrpcService>();
        app.MapGrpcService<ProgramLearningOutcomeGrpcService>();
        app.MapGrpcService<CurriculumCourseGrpcService>();
        app.MapGrpcService<LessonAssetGrpcService>();
        app.MapGrpcService<ExportGrpcService>();
        app.MapGrpcService<TagGrpcService>();
        app.MapGrpcService<AssignmentGrpcService>();
        app.MapGrpcService<RubricCriterionGrpcService>();
        app.MapGrpcService<CurriculumEmulationGrpcService>();

        app.MapGrpcService<AgentGrpcService>();

        Console.WriteLine("Resource API - gRPC services mapped successfully");

        // Map REST API Controllers for streaming support
        app.MapControllers();
        Console.WriteLine("Resource API - REST Controllers mapped successfully");

        // Add HTTP endpoints for HTTP/1.1 requests on specific routes to avoid conflicts
        app.MapGet(
                "/api/info",
                () =>
                    new
                    {
                        Service = "Resource API",
                        Status = "Running",
                        Protocols = new[] { "HTTP/1.1", "HTTP/2 (gRPC)" },
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                            ?? "Unknown",
                        Port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
                            ?? Environment.GetEnvironmentVariable("PORT") 
                            ?? builder.Configuration.GetValue<string>("ASPNETCORE_HTTP_PORTS")
                            ?? "8082",
                        Timestamp = DateTimeOffset.UtcNow,
                        Endpoints = new
                        {
                            Health = "/health",
                            Ready = "/ready",
                            Info = "/api/info",
                        },
                        GrpcServices = new[]
                        {
                            "resource.CourseService",
                            "resource.AgeRangeService",
                            "resource.CategoryService",
                            "resource.SkillService",
                            "resource.StandardService",
                            "resource.LessonService",
                            "resource.ContentService",
                            "resource.AnswerService",
                            "resource.QuestionService",
                            "resource.SectionService",
                            "resource.QuizService",
                        },
                    }
            )
            .WithDisplayName("Service Information");

        app.UseDeveloperExceptionPage();
        // Add health check endpoints for both protocols
        app.MapGet(
                "/health",
                () =>
                    new
                    {
                        Status = "Healthy",
                        Timestamp = DateTimeOffset.UtcNow,
                        Service = "Resource API",
                        Environment = builder.Environment.EnvironmentName,
                        Protocols = new[] { "HTTP/1.1", "HTTP/2 (gRPC)" },
                    }
            )
            .WithDisplayName("Health Check")
            .AllowAnonymous();

        app.MapGet(
                "/ready",
                () =>
                    new
                    {
                        Status = "Ready",
                        Timestamp = DateTimeOffset.UtcNow,
                        Service = "Resource API",
                        Database = "Connected",
                        GrpcServices = "Available",
                    }
            )
            .WithDisplayName("Readiness Check")
            .AllowAnonymous();

        // Safe database initialization with retry logic (similar to Identity service approach)
        _ = Task.Run(async () =>
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var resourceDbContext = services.GetRequiredService<ResourceDbContext>();
            var resourceDbSeed = scope.ServiceProvider.GetRequiredService<ResourceDbContextSeed>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            const int maxRetries = 10;
            const int retryDelayMs = 5000;

            for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
            {
                try
                {
                    logger.LogInformation(
                        "Resource API - Database initialization attempt {RetryCount}/{MaxRetries}...",
                        retryCount,
                        maxRetries
                    );

                    // Wait a bit for PostgreSQL to be ready
                    if (retryCount == 1)
                    {
                        await Task.Delay(10000); // Initial wait
                    }

                    // Test connection and create database if not exists
                    await EnsureDatabaseExists(resourceDbContext, logger);

                    // Test connection first
                    await resourceDbContext.Database.OpenConnectionAsync();
                    await resourceDbContext.Database.CloseConnectionAsync();

                    // Migrate changes to the database
                    await resourceDbContext.Database.MigrateAsync();
                    logger.LogInformation(" Resource API - Database migrations completed");

                    // Seed the database with data
                    logger.LogInformation("Resource API - Development environment detected, seeding sample data...");
                    await resourceDbSeed.SeedAsync();
                    logger.LogInformation("Resource API - Database seeding completed");

                    logger.LogInformation("Resource API - Database initialization successful!");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Resource API - Database initialization attempt {RetryCount} failed: {Message}",
                        retryCount,
                        ex.Message
                    );

                    if (retryCount < maxRetries)
                    {
                        logger.LogInformation(
                            " Resource API - Retrying in {Delay} seconds...",
                            retryDelayMs / 1000
                        );
                        await Task.Delay(retryDelayMs);
                    }
                    else
                    {
                        logger.LogError(
                            ex,
                            "Resource API - Database initialization failed after {MaxRetries} attempts. Service will start but data may not be available.",
                            maxRetries
                        );
                    }
                }
            }
        });

        static async Task EnsureDatabaseExists(ResourceDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Testing PostgreSQL server and Resource database...");

                // Get connection string and parse it
                var connectionString = context.Database.GetConnectionString();
                logger.LogInformation(
                    "[DEBUG] Resource API - Connection String: {ConnectionString}",
                    connectionString
                );

                // Parse connection string to get database name
                var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
                var databaseName = connectionBuilder.Database;
                var serverConnectionString = connectionBuilder.ConnectionString.Replace(
                    $"Database={databaseName}",
                    "Database=postgres"
                );

                logger.LogInformation(
                    " Checking if database '{DatabaseName}' exists...",
                    databaseName
                );

                // Connect to postgres database to check if target database exists
                using (var serverConnection = new NpgsqlConnection(serverConnectionString))
                {
                    await serverConnection.OpenAsync();
                    logger.LogInformation("Connected to PostgreSQL server");

                    // Check if database exists
                    using (
                        var cmd = new NpgsqlCommand(
                            $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'",
                            serverConnection
                        )
                    )
                    {
                        var exists = await cmd.ExecuteScalarAsync();

                        if (exists == null)
                        {
                            logger.LogInformation(
                                "Database '{DatabaseName}' does not exist. Creating it...",
                                databaseName
                            );

                            // Create database
                            using (
                                var createCmd = new NpgsqlCommand(
                                    $"CREATE DATABASE \"{databaseName}\"",
                                    serverConnection
                                )
                            )
                            {
                                await createCmd.ExecuteNonQueryAsync();
                                logger.LogInformation(
                                    " Database '{DatabaseName}' created successfully",
                                    databaseName
                                );
                            }
                        }
                        else
                        {
                            logger.LogInformation(
                                "Database '{DatabaseName}' already exists",
                                databaseName
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure database exists: {Message}", ex.Message);
                throw;
            }
        }

        await app.RunAsync();
    }
}
