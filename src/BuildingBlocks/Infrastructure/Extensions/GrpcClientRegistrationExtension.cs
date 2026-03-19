using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions
{
    public static class GrpcClientRegistrationExtension
    {
        /// <summary>
        /// Registers a gRPC client with a default retry policy and base address.
        /// The default retry policy will NOT retry on NotFound (404) errors.
        /// </summary>
        /// <typeparam name="T">The gRPC client type.</typeparam>
        /// <param name="services">The DI service collection.</param>
        /// <param name="address">The service address (e.g. http://localhost:5000).</param>
        /// <param name="methodConfig">Optional custom method config, defaults to standard retry policy.</param>
        public static IServiceCollection AddConfiguredGrpcClient<T>(
            this IServiceCollection services,
            string address,
            MethodConfig? methodConfig = null
        )
            where T : ClientBase<T>
        {
            services
                .AddGrpcClient<T>(options =>
                {
                    // Set gRPC service base address
                    options.Address = new Uri(address);
                })
                .ConfigureChannel(c =>
                {
                    // Apply retry and method config policy
                    c.ServiceConfig = new ServiceConfig
                    {
                        MethodConfigs = { methodConfig ?? GetDefaultNoNotFoundRetryPolicy() },
                    };
                });

            return services;
        }

        /// <summary>
        /// Default retry policy: retries only on transient errors (Unavailable, DeadlineExceeded).
        /// Does NOT retry on NotFound or other business errors.
        /// </summary>
        private static MethodConfig GetDefaultNoNotFoundRetryPolicy()
        {
            return new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialBackoff = TimeSpan.FromSeconds(0.5),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 2,
                    // Only retry on transient errors
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded },
                },
            };
        }
    }
}
