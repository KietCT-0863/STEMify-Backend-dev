using Infrastructure.Middlewares;

namespace Cart.API.Extensions
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

            // Configure MassTransit
            //services.AddMassTransit(x =>
            //{
            //    // Set endpoint for the exchange
            //    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("classroom", false));

            //    x.UsingRabbitMq(
            //        (context, cfg) =>
            //        {
            //            cfg.Host(
            //                new Uri(config["RabbitMq:Url"] ?? "amqp://guest:guest@localhost:5672")
            //            );

            //            cfg.ConfigureEndpoints(context);
            //        }
            //    );
            //});

            // Add Grpc
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });

            return services;
        }
    }
}
