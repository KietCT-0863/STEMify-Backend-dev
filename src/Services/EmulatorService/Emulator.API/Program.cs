
using Caching.Cache;
using Emulator.API.Services;
using Emulator.Service.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for dual ports (HTTP + gRPC)
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var httpPort = builder.Configuration.GetValue<int?>("PORT")
            ?? builder.Configuration.GetValue<int?>("ASPNETCORE_HTTP_PORTS")
            ?? 8080;
        
        var grpcPort = builder.Configuration.GetValue<int?>("GRPC_PORT") ?? 5009;
        
        options.ListenAnyIP(httpPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
        
        options.ListenAnyIP(grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
        
        Console.WriteLine($"Emulator API - HTTP port {httpPort}, gRPC port {grpcPort}");
    });
}

var mongoConnectionString = builder.Configuration.GetConnectionString("emulator-mongodb");
if (!string.IsNullOrEmpty(mongoConnectionString) &&
    !builder.Environment.IsDevelopment() &&
    (mongoConnectionString.Contains("mongodb.net") || mongoConnectionString.Contains("mongodb+srv://")))
{
    if (!mongoConnectionString.Contains("tlsDisableCertificateRevocationCheck"))
    {
        var separator = mongoConnectionString.Contains("?") ? "&" : "?";
        mongoConnectionString = $"{mongoConnectionString}{separator}tlsDisableCertificateRevocationCheck=true";

        builder.Configuration["ConnectionStrings:emulator-mongodb"] = mongoConnectionString;
    }
}

builder.AddMongoDBClient("emulator-mongodb", configureClientSettings: settings =>
{
    var connectionString = builder.Configuration.GetConnectionString("emulator-mongodb");
    if (!string.IsNullOrEmpty(connectionString) &&
        (connectionString.Contains("mongodb.net") || connectionString.Contains("mongodb+srv://")))
    {
        settings.UseTls = true;

        if (builder.Environment.IsDevelopment())
        {
            settings.AllowInsecureTls = false;
        }
    }
});

builder.Services.AddServiceServices(builder.Configuration);

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
    //options.ResponseCompressionAlgorithm = "gzip";
}).AddJsonTranscoding();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Emulator Service API",
        Version = "v1",
        Description = "gRPC service for managing 3D assembly emulations with HTTP/JSON transcoding support"
    });
});

builder.Services.AddHealthChecks();

// Add response compression (Gzip)
// builder.Services.AddResponseCompression(options =>
// {
//     options.EnableForHttps = true;
//     options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
//     options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
//         new[] { "application/json", "application/grpc" });
// });

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Emulator Service v1");
    });

    app.MapGrpcReflectionService();
}

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // Handle empty body for POST requests that expect JSON
    if (context.Request.Method == "POST" &&
        context.Request.Path.StartsWithSegments("/v1"))
    {

        // Enable buffering to read the body multiple times
        context.Request.EnableBuffering();

    }

    await next();
});

// Enable response compression
//app.UseResponseCompression();

app.MapGrpcService<EmulatorGrpcService>();
app.MapGrpcService<TemplateGrpcService>();
app.MapGrpcService<OperationGrpcService>();

app.MapHealthChecks("/health");

app.MapGet("/metrics/cache", () =>
{
    var stats = CacheRedis.GetCacheStatistics();
    return Results.Ok(new
    {
        Summary = stats.GetSummary(),
        OverallHitRate = $"{stats.GetOverallHitRate():F2}%",
        HitRateByPrefix = stats.Hits.Keys
            .Union(stats.Misses.Keys)
            .Distinct()
            .ToDictionary(
                prefix => prefix,
                prefix => new
                {
                    HitRate = $"{stats.GetHitRate(prefix):F2}%",
                    Hits = stats.Hits.GetValueOrDefault(prefix, 0),
                    Misses = stats.Misses.GetValueOrDefault(prefix, 0),
                    Sets = stats.Sets.GetValueOrDefault(prefix, 0),
                    Clears = stats.Clears.GetValueOrDefault(prefix, 0)
                }
            ),
        Errors = stats.Errors,
        Timestamp = DateTime.UtcNow
    });
}).WithTags("Monitoring");

var seedHandler = async (Emulator.Service.Interfaces.ITemplateService templateService, ILogger<Program> logger) =>
{
    try
    {
        
        var octahedronPath = @"D:\8_LASTCHANGE\STEMify-Backend\docs\octahedron.json";

        if (!File.Exists(octahedronPath))
        {
            logger.LogError("octahedron.json not found at: {Path}", octahedronPath);
            return Results.NotFound(new { Error = "octahedron.json not found", Path = octahedronPath });
        }

        var jsonContent = await File.ReadAllTextAsync(octahedronPath);
        var octahedronData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

        if (octahedronData == null)
        {
            return Results.BadRequest(new { Error = "Failed to parse octahedron.json" });
        }

      
        var (componentCount, materialCount) = await templateService
            .ImportTemplatesFromOctahedronAsync(octahedronData, "system");

        logger.LogInformation("Templates seeded: {Components} components, {Materials} materials",
            componentCount, materialCount);

        return Results.Ok(new
        {
            Message = "Octahedron templates seeded successfully",
            ComponentCount = componentCount,
            MaterialCount = materialCount,
            Templates = new
            {
                Components = new[] { "green_11_2", "2leg" },
                Materials = new[] { "plastic_green" }
            },
            Note = "Templates already exist will be skipped automatically"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed octahedron templates");
        return Results.Problem(
            detail: ex.Message,
            title: "Seeding failed"
        );
    }
};

app.MapGet("/admin/seed-octahedron", seedHandler).WithTags("Admin");
app.MapPost("/admin/seed-octahedron", seedHandler).WithTags("Admin");

app.MapGet("/", () => new
{
    Service = "Emulator API",
    Status = "Running",
    Protocols = new[] { "HTTP/1.1", "HTTP/2 (gRPC)" },
    Environment = app.Environment.EnvironmentName,
    Transcoding = "Enabled (gRPC to HTTP/JSON)",
    Endpoints = new
    {
        Health = "/health",
        CacheMetrics = "/metrics/cache",
        SeedOctahedron = "GET|POST /admin/seed-octahedron",
        Swagger = "/swagger",
        GrpcReflection = app.Environment.IsDevelopment() ? "/grpc.reflection.v1alpha.ServerReflection" : null,
        HttpApi = new
        {
            Emulations = "/v1/emulations",
            Templates = "/v1/templates",
            Operations = "/v1/operations"
        }
    }
});

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var templateService = scope.ServiceProvider.GetRequiredService<Emulator.Service.Interfaces.ITemplateService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            await Task.Delay(2000);

            await templateService.WarmupHotTemplatesAsync(topCount: 20);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to warmup templates cache on startup. Templates will be cached on-demand.");
        }
    });
});

app.Run();
