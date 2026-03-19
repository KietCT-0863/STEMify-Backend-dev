using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions
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
            MethodConfig? methodConfig = null,
            bool enableLoadBalancing = false
        )
            where T : ClientBase<T>
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                var clientTypeName = typeof(T).Name;
                Console.WriteLine(
                    $"ERROR: gRPC client {clientTypeName} address is null or empty!"
                );
                throw new ArgumentException(
                    $"gRPC client {clientTypeName} address cannot be null or empty",
                    nameof(address)
                );
            }

            Console.WriteLine(
                $"Configuring gRPC client {typeof(T).Name} with address: {address}"
            );

            services
                .AddGrpcClient<T>(options =>
                {
                    // Set gRPC service base address
                    options.Address = new Uri(address);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new SocketsHttpHandler
                    {
                        // Enhanced connection pooling for better performance
                        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                        EnableMultipleHttp2Connections = true,

                        // Connection timeout settings
                        ConnectTimeout = TimeSpan.FromSeconds(10),
                        PooledConnectionLifetime = TimeSpan.FromMinutes(10),

                        // Response timeout
                        ResponseDrainTimeout = TimeSpan.FromSeconds(5),

                        // Enable HTTP/2 without TLS for Azure Container Apps
                        UseProxy = false,
                        UseCookies = false,

                        // Automatic decompression
                        AutomaticDecompression =
                            System.Net.DecompressionMethods.GZip
                            | System.Net.DecompressionMethods.Deflate,
                    };

                    Console.WriteLine(
                        $"{typeof(T).Name}: SocketsHttpHandler configured with HTTP/2 optimizations"
                    );
                    return handler;
                })
                .ConfigureChannel(c =>
                {
                    var serviceConfig = new ServiceConfig
                    {
                        MethodConfigs = { methodConfig ?? GetDefaultNoNotFoundRetryPolicy() }
                    };

                    if (enableLoadBalancing)
                    {
                        serviceConfig.LoadBalancingConfigs.Add(new RoundRobinConfig());
                    }

                    // Enhanced channel configuration
                    c.ServiceConfig = serviceConfig;

                    // Set channel options for better reliability
                    c.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
                    c.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
                    c.MaxRetryAttempts = 3;
                    c.MaxRetryBufferSize = 1024 * 1024; // 1MB
                    c.MaxRetryBufferPerCallSize = 1024 * 1024; // 1MB

                    Console.WriteLine(
                        $"{typeof(T).Name}: Channel configured with retry policy and size limits"
                    );
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
                    InitialBackoff = TimeSpan.FromSeconds(1),
                    MaxBackoff = TimeSpan.FromSeconds(10),
                    BackoffMultiplier = 2,
                    // Only retry on transient errors
                    RetryableStatusCodes =
                    {
                        StatusCode.Unavailable,
                        StatusCode.DeadlineExceeded,
                        StatusCode.ResourceExhausted,
                        StatusCode.Aborted,
                    },
                },
            };
        }

        /// <summary>
        /// Enhanced retry policy for critical operations with longer timeouts
        /// </summary>
        public static MethodConfig GetEnhancedRetryPolicy()
        {
            return new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromSeconds(2),
                    MaxBackoff = TimeSpan.FromSeconds(30),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes =
                    {
                        StatusCode.Unavailable,
                        StatusCode.DeadlineExceeded,
                        StatusCode.ResourceExhausted,
                        StatusCode.Aborted,
                        StatusCode.Internal,
                    },
                },
            };
        }

        /// <summary>
        /// Conservative retry policy for read operations
        /// </summary>
        public static MethodConfig GetConservativeRetryPolicy()
        {
            return new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 2,
                    InitialBackoff = TimeSpan.FromSeconds(0.5),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded },
                },
            };
        }
    }
}
