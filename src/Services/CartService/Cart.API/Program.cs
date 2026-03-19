using Cart.API.Extensions;
using Cart.API.Services;
using Cart.Application.Extensions;
using Cart.Infrastructure.Extensions;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CartDbContext>(
    "stemifycart",
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
        Title = "Cart Service API",
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

app.MapGrpcService<CartGrpcService>();

// Configure the HTTP request pipeline.
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var notificationDbContext = services.GetRequiredService<CartDbContext>();
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
