using Common.Logging.Extensions;
using FluentValidation;
using Identity.API.Middleware;
using Identity.API.Services;
using Identity.Application.Extensions;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Infrastructure.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomSerilog();

builder.AddServiceDefaults();

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var httpPort = builder.Configuration.GetValue<int?>("PORT")
            ?? builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
            ?? 8083;
        
        var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5083;
        
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
        
        options.Limits.MaxConcurrentConnections = 1000;
        options.Limits.MaxConcurrentUpgradedConnections = 100;
        options.Limits.MaxRequestBodySize = 30_000_000; // 30MB
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });
}

builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Identity Service API",
        Version = "v1",
        Description = "gRPC service with HTTP/JSON transcoding"
    });
    c.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["OpenIddict:Authority"]; 
        options.Audience = "stemify-api"; 

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false, 
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "role",
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Prevent automatic redirect to login page for API endpoints
                // Return 401 instead of redirecting
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    data = (object?)null,
                    isSucceeded = false,
                    message = "Unauthorized. Please login first.",
                    statusCode = 401
                });
                return context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Teacher", policy => policy.RequireClaim("role", "Teacher"));
    options.AddPolicy("Student", policy => policy.RequireClaim("role", "Student"));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

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

builder
    .Services.AddGrpc(options =>
    {
        options.Interceptors.Add<GrpcExceptionInterceptor>();
    })
    .AddJsonTranscoding();

// Add gRPC client for Order service (Dashboard)
builder.Services.AddGrpcClient<Shared.Protos.Order.GrpcDashboardService.GrpcDashboardServiceClient>(options =>
{
    var grpcOrderUrl = builder.Configuration["GrpcServices:OrderService:Address"] 
        ?? builder.Configuration["GrpcOrderUrl"] 
        ?? "http://order-service:5006";
    options.Address = new Uri(grpcOrderUrl);
});

// Apply migrations BEFORE building the app to ensure Outbox tables exist
// before MassTransit Outbox processor starts querying
await ApplyMigrationsBeforeBuildAsync(builder);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                      .CreateLogger("RequestLogger");

    logger.LogInformation("Incoming request: {method} {path}, Protocol={protocol}, Scheme={scheme}, Host={host}",
        context.Request.Method,
        context.Request.Path,
        context.Request.Protocol,
        context.Request.Scheme,
        context.Request.Host);

    foreach (var header in context.Request.Headers)
    {
        logger.LogInformation("Header: {key}: {value}", header.Key, header.Value);
    }

    await next();
});


// Map default endpoints (Aspire)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "STEMify Identity API v1");
    c.RoutePrefix = "swagger";
});



app.MapGrpcService<UserGrpcService>();
app.MapGrpcService<JobRoleGrpcService>();
app.MapGrpcService<ContactGrpcService>();
app.MapGrpcService<BulkProvisioningGrpcService>();
app.MapGrpcService<InvitationGrpcService>();
app.MapGrpcService<GroupGrpcService>();

app.UseHttpsRedirection();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Admin Dashboard Endpoint
app.MapGet("/admin/dashboard", async (
    string? period,
    Shared.Protos.Order.GrpcDashboardService.GrpcDashboardServiceClient dashboardClient,
    HttpContext httpContext) =>
{
    try
    {
        var request = new Shared.Protos.Order.GetSystemAdminDashboardRequest
        {
            Period = period
        };

        var response = await dashboardClient.GetSystemAdminDashboardAsync(request);

        return Results.Ok(new
        {
            data = response,
            isSucceeded = true,
            message = "Success",
            statusCode = 200
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            data = (object?)null,
            isSucceeded = false,
            message = ex.Message,
            statusCode = 500
        }, statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetSystemAdminDashboard")
.WithTags("Admin");

app.Run();

static async Task ApplyMigrationsBeforeBuildAsync(WebApplicationBuilder builder)
{
    var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
    var logger = loggerFactory.CreateLogger<Program>();
    
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("stemifyidentity")
            ?? builder.Configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogWarning("No connection string found. Skipping pre-build migrations.");
            return;
        }
        
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("Identity.Infrastructure");
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
        optionsBuilder.UseOpenIddict<Guid>();
        
        var tempServiceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        
        using var context = new ApplicationDbContext(optionsBuilder.Options, tempServiceProvider);
        
        // Check for pending migrations first
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            
            await context.Database.MigrateAsync();
         }
       
    }
    
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply migrations before app build. This may cause Outbox errors.");
    }
}
