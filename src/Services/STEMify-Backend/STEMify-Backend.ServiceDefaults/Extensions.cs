using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            //http.AddStandardResilienceHandler();

            // Turn on service discovery by default
             http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });
        // Configure HTTP/2 transport by default for all services
        builder.Services.Configure<ServiceDiscovery.ServiceDiscoveryOptions>(options =>
        {
            options.AllowedSchemes = ["https", "http"];
        });

        // Configure Aspire HTTP/2 transport
        builder.Configuration["Aspire:EndpointDefaults:Transport"] = "Http2";

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                // Built-in metrics
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                // Custom meters
                metrics.AddMeter("STEMifyBackend.Identity")
                       .AddMeter("STEMifyBackend.Classroom")
                       .AddMeter("STEMifyBackend.Resource")
                       .AddMeter("STEMifyBackend.Product")
                       .AddMeter("STEMifyBackend.Order")
                       .AddMeter("STEMifyBackend.Payment")
                       .AddMeter("STEMifyBackend.Cart")
                       .AddMeter("STEMifyBackend.Notification")
                       .AddMeter("STEMifyBackend.Emulator");

                // Enhanced HTTP metrics configuration
                metrics.AddView(
                    "http.server.request.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries =
                        [
                            0,
                            0.005,
                            0.01,
                            0.025,
                            0.05,
                            0.075,
                            0.1,
                            0.25,
                            0.5,
                            0.75,
                            1,
                            2.5,
                            5,
                            7.5,
                            10,
                        ],
                    }
                );
            })
            .WithTracing(tracing =>
            {
                // Built-in instrumentation
                tracing.AddSource(builder.Environment.ApplicationName);

                // Enhanced ASP.NET Core instrumentation
                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);

                    // Enrich spans with additional data
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag(
                            "http.request.header.user-agent",
                            request.Headers.UserAgent.ToString()
                        );
                        activity.SetTag(
                            "http.request.header.correlation-id",
                            request.Headers["X-Correlation-ID"].FirstOrDefault()
                        );
                        activity.SetTag(
                            "http.request.client_ip",
                            request.HttpContext.Connection.RemoteIpAddress?.ToString()
                        );
                    };
                    options.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag(
                            "http.response.header.content-type",
                            response.Headers.ContentType.ToString()
                        );
                    };
                });

                // Enhanced HTTP client instrumentation
                tracing.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequestMessage = (activity, request) =>
                    {
                        if (
                            request.Headers.TryGetValues("X-Correlation-ID", out var correlationIds)
                        )
                        {
                            activity.SetTag(
                                "http.client.request.header.correlation-id",
                                correlationIds.FirstOrDefault()
                            );
                        }
                    };
                });

                tracing.AddGrpcClientInstrumentation();
                // Message bus instrumentation
                tracing.AddSource("MassTransit");

                // Custom activity sources
                tracing.AddSource("STEMifyBackend.Identity")
                       .AddSource("STEMifyBackend.Classroom")
                       .AddSource("STEMifyBackend.Resource")
                       .AddSource("STEMifyBackend.Product")
                       .AddSource("STEMifyBackend.Order")
                       .AddSource("STEMifyBackend.Payment")
                       .AddSource("STEMifyBackend.Cart")
                       .AddSource("STEMifyBackend.Notification")
                       .AddSource("STEMifyBackend.Emulator");

                // Development vs Production sampling
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
                else
                {
                    tracing.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling in production
                }
            });
        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

        if (useOtlpExporter)
        {
             builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        // External observability stack integration
        var externalStackEnabled = builder.Configuration.GetValue<bool>(
            "Observability:ExternalStackEnabled"
        );
        if (externalStackEnabled)
        {
            builder
                .Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    // Jaeger
                    var jaegerEndpoint = builder.Configuration[
                        "Observability:Exporters:Jaeger:Endpoint"
                    ];
                    if (!string.IsNullOrEmpty(jaegerEndpoint))
                    {
                        tracing.AddOtlpExporter(opt => opt.Endpoint = new Uri(jaegerEndpoint));
                    }
                })
                .WithMetrics(metrics =>
                {
                    // Prometheus - automatically enabled when external stack is enabled
                    metrics.AddPrometheusExporter();
                });
        }

        // // Add Azure Application Insights
        // if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
        // {
        //     builder.Services.AddApplicationInsightsTelemetry();
        // }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health checks
        app.MapHealthChecks("/health");
        app.MapHealthChecks(
            "/alive",
            new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
        );

        // Prometheus metrics endpoint (when using external stack)
        var externalStackEnabled = app.Configuration.GetValue<bool>(
            "Observability:ExternalStackEnabled"
        );
        if (externalStackEnabled)
        {
            app.MapPrometheusScrapingEndpoint();
        }

        return app;
    }
}
