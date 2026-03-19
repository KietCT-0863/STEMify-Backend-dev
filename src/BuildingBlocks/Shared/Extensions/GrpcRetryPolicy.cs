using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Shared.Extensions
{
    public class GrpcRetryPolicy
    {
        // This static helper class centralizes the gRPC retry policy configuration.
        // It allows consistent retry logic across all gRPC clients.
        public static MethodConfig GetDefaultMethodConfig() =>
            new MethodConfig
            {
                // Returns a default MethodConfig with retry policy.
                // This policy retries on UNAVAILABLE errors, with exponential backoff.
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 10,
                    InitialBackoff = TimeSpan.FromSeconds(5),
                    MaxBackoff = TimeSpan.FromSeconds(30),
                    BackoffMultiplier = 2,
                    RetryableStatusCodes =
                    {
                        StatusCode.Unavailable,
                        StatusCode.DeadlineExceeded,
                        StatusCode.Internal,
                        StatusCode.ResourceExhausted,
                    },
                },
            };
    }
}
