namespace Saga.Orchestrator.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            RedisConfig(services, configuration);
            AddInFrastructureServices(services);
            return services;
        }

        private static IServiceCollection AddInFrastructureServices(
            this IServiceCollection services
        )
        {
            return services;
        }

        private static IServiceCollection RedisConfig(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new ArgumentNullException("Redis connection string is missing");
            }

            return services;
        }
    }
}
