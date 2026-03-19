
using Infrastructure.Middlewares;
using MassTransit;

namespace Order.API.Extensions
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

                // Configure MassTransit for publishing integration events
                services.AddMassTransit(x =>
                {
                   x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));

                    x.UsingRabbitMq((context, cfg) =>
                    {
                        var rabbitUrl = config.GetConnectionString("messaging") 
                            ?? config["RabbitMq:Url"] 
                            ?? "amqp://guest:guest@localhost:5672";
                        cfg.Host(new Uri(rabbitUrl));

                       cfg.ConfigureEndpoints(context);
                    });
                });
                services.AddGrpc(options =>
                {
                    options.Interceptors.Add<GrpcExceptionInterceptor>();
                });

            return services;
        }
    }
}
