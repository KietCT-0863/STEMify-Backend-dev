using Saga.Orchestrator.Extensions;
using Serilog;

namespace Saga.Orchestrator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        Log.Information($"Starting {builder.Environment.ApplicationName} up");
        try
        {
            builder.Host.AddAppConfiguration();

            builder.AddServiceDefaults();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            string type = ex.GetType().Name;
            if (
                type == "OperationCanceledException"
                || type == "TaskCanceledException"
                || type.Equals("StopTheHostException", StringComparison.Ordinal)
            )
                throw;
            Log.Fatal(ex, $"Unhandled exception: {ex.Message}");
        }
        finally
        {
            Log.Information($"Shut down {builder.Environment.ApplicationName} complete");
            Log.CloseAndFlush();
        }
    }
}
