using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STEMifyBackend.AppHost.Extensions
{
    public static class OpenTelemetryCollectorBuilderExtensions
{
        private const string DashboardOtlpUrlVariableName = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
        private const string DashboardOtlpApiKeyVariableName = "AppHost:OtlpApiKey";
        private const string DashboardOtlpUrlDefaultValue = "http://localhost:18889";
        private const string OTelCollectorImageName = "otel/opentelemetry-collector-contrib";
        private const string OTelCollectorImageTag = "latest";

        public static IResourceBuilder<OpenTelemetryCollectorResource> AddOpenTelemetryCollector(this IDistributedApplicationBuilder builder, string name, string configFileLocation)
        {
            builder.AddOpenTelemetryCollectorInfrastructure();

            var url = builder.Configuration[DashboardOtlpUrlVariableName] ?? DashboardOtlpUrlDefaultValue;
            var isHttpsEnabled = url.StartsWith("https", StringComparison.OrdinalIgnoreCase);

            var dashboardOtlpEndpoint = new HostUrl(url);

            var resource = new OpenTelemetryCollectorResource(name);
            var resourceBuilder = builder.AddResource(resource)
                .WithImage(OTelCollectorImageName, OTelCollectorImageTag)
                .WithEndpoint(targetPort: 4317, name: OpenTelemetryCollectorResource.OtlpGrpcEndpointName, scheme: isHttpsEnabled ? "https" : "http")
                .WithEndpoint(targetPort: 4318, name: OpenTelemetryCollectorResource.OtlpHttpEndpointName, scheme: isHttpsEnabled ? "https" : "http")
                .WithBindMount(configFileLocation, "/etc/otelcol-contrib/config.yaml");

            // Only add Aspire environment variables for development
            if (builder.Environment.IsDevelopment())
            {
                resourceBuilder = resourceBuilder
                    .WithEnvironment("ASPIRE_ENDPOINT", $"{dashboardOtlpEndpoint}")
                    .WithEnvironment("ASPIRE_API_KEY", builder.Configuration[DashboardOtlpApiKeyVariableName])
                    .WithEnvironment("ASPIRE_INSECURE", isHttpsEnabled ? "false" : "true");
            }

            return resourceBuilder;
        }

    }
    public class OpenTelemetryCollectorResource(string name) : ContainerResource(name)
    {
        internal const string OtlpGrpcEndpointName = "grpc";
        internal const string OtlpHttpEndpointName = "http";
    }
    internal static class OpenTelemetryCollectorServiceExtensions
    {
        public static IDistributedApplicationBuilder AddOpenTelemetryCollectorInfrastructure(this IDistributedApplicationBuilder builder)
        {
            builder.Services.TryAddLifecycleHook<OpenTelemetryCollectorLifecycleHook>();

            return builder;
        }
    }

}
