using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STEMifyBackend.AppHost.Extensions
{
    internal sealed class OpenTelemetryCollectorLifecycleHook : IDistributedApplicationLifecycleHook
    {
        private const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";

        private readonly ILogger<OpenTelemetryCollectorLifecycleHook> _logger;

        public OpenTelemetryCollectorLifecycleHook(ILogger<OpenTelemetryCollectorLifecycleHook> logger)
        {
            _logger = logger;
        }

        public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
        {
            var collectorResource = appModel.Resources.OfType<OpenTelemetryCollectorResource>().FirstOrDefault();
            if (collectorResource == null)
            {
                _logger.LogWarning($"No {nameof(OpenTelemetryCollectorResource)} resource found.");
                return Task.CompletedTask;
            }

            var endpoint = collectorResource.GetEndpoint(OpenTelemetryCollectorResource.OtlpGrpcEndpointName);
            if (!endpoint.Exists)
            {
                _logger.LogWarning($"No {OpenTelemetryCollectorResource.OtlpGrpcEndpointName} endpoint for the collector.");
                return Task.CompletedTask;
            }

            foreach (var resource in appModel.Resources)
            {
                resource.Annotations.Add(new EnvironmentCallbackAnnotation((context) =>
                {
                    if (context.EnvironmentVariables.ContainsKey(OtelExporterOtlpEndpoint))
                    {
                        _logger.LogDebug("Forwarding telemetry for {ResourceName} to the collector ({url}).", resource.Name, endpoint.Url);

                        context.EnvironmentVariables[OtelExporterOtlpEndpoint] = endpoint;
                    }
                }));
            }

            return Task.CompletedTask;
        }
    }
}
