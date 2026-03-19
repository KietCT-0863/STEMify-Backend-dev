using Identity.Web.Extensions;

namespace Identity.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure all services using extension methods
        builder.ConfigureServices();

        var app = builder.Build();

        // Configure middleware pipeline using extension methods
        app.ConfigurePipeline();

        await app.RunAsync();
    }
}
