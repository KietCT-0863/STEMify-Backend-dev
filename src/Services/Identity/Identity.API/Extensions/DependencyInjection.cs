using Infrastructure.Middlewares;

namespace Identity.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services.AddDataProtection();

            // Change routing urls to lowercase
            services.AddRouting(opt => opt.LowercaseUrls = true);

            // Add Grpc
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });

            return services;
        }
    }
}
