using BuildingBlocks.Authorization.Extensions;
using Classroom.API.Extensions;
using Classroom.API.Services;
using Classroom.Application.Extensions;
using Classroom.Infrastructure.Extensions;
using Classroom.Infrastructure.Persistence;
using Infrastructure.Middlewares;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure Kestrel for HTTP/2 without TLS (for Azure Container Apps)
builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsProduction())
    {
        var httpPort = builder.Configuration.GetValue<int?>("PORT")
            ?? builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
            ?? 8081;
        
        var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5081;
        
        // Port for HTTP/1.1 + HTTP/2 (REST API, JSON transcoding from gateway)
        options.ListenAnyIP(
            httpPort,
            listenOptions =>
            {
                listenOptions.Protocols = Microsoft
                    .AspNetCore
                    .Server
                    .Kestrel
                    .Core
                    .HttpProtocols
                    .Http1AndHttp2;
                listenOptions.UseConnectionLogging();
            }
        );
        
        // Dedicated port for gRPC (HTTP/2 only) - internal service-to-service calls
        options.ListenAnyIP(
            grpcPort,
            listenOptions =>
            {
                listenOptions.Protocols = Microsoft
                    .AspNetCore
                    .Server
                    .Kestrel
                    .Core
                    .HttpProtocols
                    .Http2;
                listenOptions.UseConnectionLogging();
            }
        );
        
        Console.WriteLine($"Classroom API - Production mode: HTTP port {httpPort}, gRPC port {grpcPort}");
    }
    else
    {

        var httpPort = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
          ?? builder.Configuration.GetValue<int?>("PORT")
          ?? 5001;

        var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5081;

        options.ListenAnyIP(
          httpPort,
          listenOptions =>
          {
              listenOptions.Protocols = Microsoft
                  .AspNetCore
                  .Server
                  .Kestrel
                  .Core
                  .HttpProtocols
                  .Http1AndHttp2;
              listenOptions.UseConnectionLogging();
          }
        );

        // Dedicated port for gRPC (HTTP/2 only) - internal service-to-service calls
        options.ListenAnyIP(
          grpcPort,
          listenOptions =>
          {
              listenOptions.Protocols = Microsoft
                  .AspNetCore
                  .Server
                  .Kestrel
                  .Core
                  .HttpProtocols
                  .Http2;
              listenOptions.UseConnectionLogging();
          }
        );


        //options.ConfigureEndpointDefaults(endpointOptions =>
        //{
        //    endpointOptions.Protocols = Microsoft
        //        .AspNetCore
        //        .Server
        //        .Kestrel
        //        .Core
        //        .HttpProtocols
        //        .Http1AndHttp2;
        //});
        //Console.WriteLine("Classroom API - Development mode: Using default Kestrel configuration");
    }
});



// Add organization-level authorization
builder.Services.AddOrganizationPermissions();
builder.Services.AddOrganizationAuthorization(options =>
{
    // Add all permission policies for classroom operations
    options.AddAllOrganizationPermissionPolicies();
});

// Add services to the container.

builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Classroom Service API",
        Version = "v1",
        Description = "gRPC service with HTTP/JSON transcoding"
    });
});

builder.Services.AddGrpc().AddJsonTranscoding();
//builder.Services.AddControllers();

builder.AddNpgsqlDbContext<ClassroomDbContext>(
    "stemifyclassroom",
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

builder
    .Services.AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Classroom Service v1");
    c.RoutePrefix = "swagger";
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorWrappingMiddleware>();

//  Azure Container Apps gRPC debugging middleware
app.Use(
    async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        // Log all incoming requests for debugging
        logger.LogInformation(
            "Classroom API - Incoming request: {Method} {Path} | Protocol: {Protocol} | Host: {Host} | RemoteIP: {RemoteIP}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Protocol,
            context.Request.Host,
            context.Connection.RemoteIpAddress
        );

        // Special logging for gRPC requests
        if (
            context.Request.Path.StartsWithSegments("/classroom.")
            || context.Request.ContentType?.Contains("application/grpc") == true
        )
        {
            logger.LogInformation(
                "gRPC Request - Path: {Path} | Content-Type: {ContentType} | User-Agent: {UserAgent}",
                context.Request.Path,
                context.Request.ContentType,
                context.Request.Headers.UserAgent.ToString()
            );
        }

        await next();

    }
);

// Add security middleware for internal request validation
// app.Use(async (context, next) =>
// {
//     var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

//     // Check if this is a gRPC request
//     var isGrpcRequest = context.Request.Path.StartsWithSegments("/classroom.");

//     if (isGrpcRequest)
//     {
//         // Log security events for gRPC calls
//         logger.LogInformation(
//             "gRPC Security Check - Path: {Path}, Protocol: {Protocol}, RemoteIP: {RemoteIP}",
//             context.Request.Path,
//             context.Request.Protocol,
//             context.Connection.RemoteIpAddress
//         );

//         // Validate internal request (optional - for additional security)
//         var userAgent = context.Request.Headers.UserAgent.ToString();
//         var isInternalRequest = userAgent.Contains("ApiGateway") ||
//                                context.Request.Headers.ContainsKey("X-Internal-Request");

//         if (!isInternalRequest)
//         {
//             logger.LogWarning(
//                 "Potential external gRPC access attempt - UserAgent: {UserAgent}",
//                 userAgent
//             );
//         }
//     }

//     await next();
// });

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<ClassroomGrpcService>();
app.MapGrpcService<CourseEnrollmentGrpcService>();
app.MapGrpcService<StudentProgressGrpcService>();
app.MapGrpcService<CurriculumEnrollmentGrpcService>();
app.MapGrpcService<CertificateGrpcService>();
app.MapGrpcService<StudentQuizGrpcService>();
app.MapGrpcService<QuizAttemptGrpcService>();
app.MapGrpcService<ClassroomStudentGrpcService>();
app.MapGrpcService<StudentAssignmentGrpcService>();
app.MapGrpcService<AssignmentAttemptGrpcService>();

//app.MapControllers();

// Safe database initialization with retry logic (similar to Identity service approach)
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var classroomDbContext = services.GetRequiredService<ClassroomDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    const int retryDelayMs = 5000;

    for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
    {
        try
        {
            logger.LogInformation(
                "Classroom API - Database initialization attempt {RetryCount}/{MaxRetries}...",
                retryCount,
                maxRetries
            );

            // Wait a bit for PostgreSQL to be ready
            if (retryCount == 1)
            {
                await Task.Delay(10000); // Initial wait
            }

            // Test connection and create database if not exists
            await EnsureDatabaseExists(classroomDbContext, logger);

            // Test connection first
            await classroomDbContext.Database.OpenConnectionAsync();
            await classroomDbContext.Database.CloseConnectionAsync();

            // Migrate changes to the database
            await classroomDbContext.Database.MigrateAsync();
            logger.LogInformation(" Classroom API - Database migrations completed");

            logger.LogInformation("Classroom API - Database initialization successful!");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Classroom API - Database initialization attempt {RetryCount} failed: {Message}",
                retryCount,
                ex.Message
            );

            if (retryCount < maxRetries)
            {
                logger.LogInformation(
                    " Classroom API - Retrying in {Delay} seconds...",
                    retryDelayMs / 1000
                );
                await Task.Delay(retryDelayMs);
            }
            else
            {
                logger.LogError(
                    ex,
                    "Classroom API - Database initialization failed after {MaxRetries} attempts. Service will start but data may not be available.",
                    maxRetries
                );
            }
        }
    }
});

static async Task EnsureDatabaseExists(ClassroomDbContext context, ILogger logger)
{
    try
    {
        logger.LogInformation("Testing PostgreSQL server and Classroom database...");

        // Get connection string and parse it
        var connectionString = context.Database.GetConnectionString();
        logger.LogInformation(
            "[DEBUG] Classroom API - Connection String: {ConnectionString}",
            connectionString
        );

        // Parse connection string to get database name
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connectionBuilder.Database;
        var serverConnectionString = connectionBuilder.ConnectionString.Replace(
            $"Database={databaseName}",
            "Database=postgres"
        );

        logger.LogInformation("Checking if database '{DatabaseName}' exists...", databaseName);

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
                        " Database '{DatabaseName}' already exists",
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

app.Run();
