using Infrastructure.Middlewares;
using MassTransit;

namespace Resource.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services.AddDataProtection();

            services.AddRouting(opt => opt.LowercaseUrls = true);

            // *** Temporarily

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "customPolicy",
                    b =>
                    {
                        var clientAppUrls = config["ClientApp"] ?? config["Cors__Origins"] ?? "https://localhost:3000";
                        var allowedOrigins = clientAppUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                   
                        b.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithOrigins(allowedOrigins);
                    }
                );
            });

            // Configure MassTransit
            services.AddMassTransit(x =>
            {
                // Set endpoint for the exchange
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("classroom", false));

                x.UsingRabbitMq(
                    (context, cfg) =>
                    {
                        cfg.Host(
                            new Uri(config.GetConnectionString("messaging") ?? config["RabbitMq:Url"] ?? "amqp://guest:guest@localhost:5672")
                        );

                        cfg.ConfigureEndpoints(context);
                    }
                );
            });

            // Add Grpc
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });

            return services;
        }
    }
}
