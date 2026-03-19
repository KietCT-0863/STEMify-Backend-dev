using Microsoft.Extensions.Hosting;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace STEMifyBackend.AppHost.Extensions
{
    public static class ServiceExtensions
    {
        public static IDistributedApplicationBuilder AddApplicationServices(this IDistributedApplicationBuilder builder)
        {
            // Get or create snapshot-based data directory for Postgres persistence
            var postgresDataPath = GetOrCreatePostgresSnapshotPath(builder.Environment.IsDevelopment());

            var postgres = builder.AddPostgres("stemify-postgres");
            if (builder.Environment.IsDevelopment())
                postgres.WithBindMount(postgresDataPath, "/var/lib/postgresql/data", isReadOnly: false);

            var identityDb = postgres.AddDatabase("stemifyidentity");

            var classroomDb = postgres.AddDatabase("stemifyclassroom");

            var resourceDb = postgres.AddDatabase("stemifyresource");

            var notificationDb = postgres.AddDatabase("stemifynotification");

            var productDb = postgres.AddDatabase("stemifyproduct");

            var orderDb = postgres.AddDatabase("stemifyorder");
            var paymentDb = postgres.AddDatabase("stemifypayment");

            var cartDb = postgres.AddDatabase("stemifycart");

            var hangfireDb = postgres.AddDatabase("stemifyhangfire");

            var aiMemoryDb = postgres.AddDatabase("stemifyaimemory");

            // // Add pgAdmin for database administration (Production)
            // var pgAdmin = builder
            //     .AddContainer("pgadmin", "dpage/pgadmin4")
            //     .WithHttpEndpoint(targetPort: 5050, name: "http")
            //     .WithExternalHttpEndpoints()
            //     .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@stemify.com")
            //     .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", Environment.GetEnvironmentVariable("PGADMIN_PASSWORD") ?? "admin")
            //     .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False")
            //     .WithEnvironment("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False")
            //     .WithEnvironment("PGADMIN_LISTEN_PORT", "5050")
            //     .WaitFor(postgres);

            // Add RabbitMQ for messaging (required for Payment Service Saga pattern)
            IResourceBuilder<IResourceWithConnectionString> rabbitmqReference;
            if (builder.Environment.IsProduction())
            {
                var cloudAmqpUrl = builder.Configuration["RabbitMQ:Url"]
        ?? Environment.GetEnvironmentVariable("RABBITMQ_URL")
        ?? throw new InvalidOperationException("RabbitMQ URL not configured.");

                rabbitmqReference = builder.AddConnectionString("messaging", cloudAmqpUrl);
            }
            else
            {
                var rabbitmq = builder.AddRabbitMQ("messaging")
                .WithManagementPlugin();
                rabbitmqReference = rabbitmq;
            }


            var manualFlag = Environment.GetEnvironmentVariable("ENABLE_OBSERVABILITY_STACK");
            var externalObservabilityEnabled = builder.Environment.IsDevelopment() ||
                                               string.Equals(manualFlag, "true", StringComparison.OrdinalIgnoreCase);
            EndpointReference? grafanaEndpoint = null;
            externalObservabilityEnabled = false;
            if (externalObservabilityEnabled)
            {
                var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.5.0")
                    .WithBindMount("../../../../config/prometheus", "/etc/prometheus", isReadOnly: true)
                    .WithArgs(
                        "--web.enable-otlp-receiver",
                        "--config.file=/etc/prometheus/prometheus.yml",
                        "--storage.tsdb.retention.time=15d",
                        "--storage.tsdb.retention.size=10GB"
                    )
                    .WithHttpEndpoint(targetPort: 9090, name: "http");

                var alertmanager = builder.AddContainer("alertmanager", "prom/alertmanager", "v0.27.0")
                    .WithBindMount("../../../../config/alertmanager", "/etc/alertmanager", isReadOnly: true)
                    .WithArgs("--config.file=/etc/alertmanager/alertmanager.yml", "--storage.path=/alertmanager")
                    .WithHttpEndpoint(targetPort: 9093, name: "http");

                // Loki - Log aggregation system
                var loki = builder.AddContainer("loki", "grafana/loki", "2.9.0")
                    .WithBindMount("../../../../config/loki", "/etc/loki", isReadOnly: true)
                    .WithArgs("-config.file=/etc/loki/loki-config.yaml")
                    .WithHttpEndpoint(targetPort: 3100, name: "http")
                    .WithEnvironment("LOKI_CONFIG_FILE", "/etc/loki/loki-config.yaml");

                // Promtail - Log shipper for Loki
                var promtail = builder.AddContainer("promtail", "grafana/promtail", "2.9.0")
                    .WithBindMount("../../../../config/promtail", "/etc/promtail", isReadOnly: true)
                    .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock", isReadOnly: true)
                    .WithArgs("-config.file=/etc/promtail/promtail-config.yaml")
                    .WithHttpEndpoint(targetPort: 9080, name: "http")
                    .WithEnvironment("PROMTAIL_CONFIG_FILE", "/etc/promtail/promtail-config.yaml")
                    .WaitFor(loki);

                // Tempo - Distributed tracing backend
                var tempo = builder.AddContainer("tempo", "grafana/tempo", "2.3.0")
                    .WithBindMount("../../../../config/tempo", "/etc/tempo", isReadOnly: true)
                    .WithArgs("-config.file=/etc/tempo/tempo-config.yaml")
                    .WithHttpEndpoint(targetPort: 3200, name: "http")
                    .WithHttpEndpoint(targetPort: 4317, name: "otlp-grpc")
                    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
                    .WithHttpEndpoint(targetPort: 14268, name: "jaeger-http")
                    .WithHttpEndpoint(targetPort: 14250, name: "jaeger-grpc")
                    .WithHttpEndpoint(targetPort: 9411, name: "zipkin")
                    .WithEnvironment("TEMPO_CONFIG_FILE", "/etc/tempo/tempo-config.yaml");

                var grafana = builder.AddContainer("grafana", "grafana/grafana")
                                     .WithBindMount("../../../../config/grafana/config", "/etc/grafana", isReadOnly: true)
                                     .WithBindMount("../../../../config/grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                                     .WithEnvironment("PROMETHEUS_ENDPOINT", prometheus.GetEndpoint("http"))
                                     .WithEnvironment("LOKI_ENDPOINT", loki.GetEndpoint("http"))
                                     .WithEnvironment("TEMPO_ENDPOINT", tempo.GetEndpoint("http"))
                                     .WithHttpEndpoint(targetPort: 3000, name: "http")
                                     .WaitFor(prometheus)
                                     .WaitFor(loki)
                                     .WaitFor(tempo);

                grafanaEndpoint = grafana.GetEndpoint("http");

                var configFile = "../../../../config/otelcollector/config.dev.yaml";

                builder.AddOpenTelemetryCollector("otelcollector", configFile)
                       .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheus.GetEndpoint("http")}/api/v1/otlp")
                       .WithEnvironment("ALERTMANAGER_ENDPOINT", alertmanager.GetEndpoint("http"))
                       .WithEnvironment("LOKI_ENDPOINT", loki.GetEndpoint("http"))
                       .WithEnvironment("TEMPO_ENDPOINT_GRPC", tempo.GetEndpoint("otlp-grpc"))
                       .WaitFor(loki)
                       .WaitFor(tempo)
                       .WaitFor(tempo);
            }

            var pgAdmin = builder.Environment.IsProduction()
                ? builder
                    .AddContainer("pgadmin", "dpage/pgadmin4:8.10")
                    .WithHttpEndpoint(targetPort: 5050, name: "pgadmin-http")
                    .WithExternalHttpEndpoints()
                    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@stemify.com")
                    .WithEnvironment(
                        "PGADMIN_DEFAULT_PASSWORD",
                        Environment.GetEnvironmentVariable("PGADMIN_PASSWORD") ?? "admin"
                    )
                    .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False")
                    .WithEnvironment("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False")
                    .WithEnvironment("PGADMIN_LISTEN_PORT", "5050")
                    .WithEnvironment("PGADMIN_CONFIG_CHECK_EMAIL_DELIVERABILITY", "False")
                    .WithEnvironment("GUNICORN_TIMEOUT", "120")
                    .WaitFor(postgres)
                : postgres.WithPgAdmin(x =>
                {
                    x.WithHttpEndpoint(targetPort: 5050, name: "pgadmin-dev");
                    x.WithExternalHttpEndpoints();
                    x.WithEnvironment("PGADMIN_CONFIG_CHECK_EMAIL_DELIVERABILITY", "False");
                    x.WithEnvironment("GUNICORN_TIMEOUT", "120");
                });

            var identityService = builder
                .AddProject<Projects.Identity_Web>("identity")
                .WithExternalHttpEndpoints()
                .WithReference(identityDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(identityDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(identityService);

            var identityApiService = builder
                .AddProject<Projects.Identity_API>("identity-api")
                .WithExternalHttpEndpoints()
                .WithReference(identityDb)
                .WithReference(identityService)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(identityDb)
                .WaitFor(identityService)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(identityApiService);

            // var sagaOrchestrator = builder
            //     .AddProject<Projects.Saga_Orchestrator>("saga-orchestrator")
            //     .WithReference(identityService)
            //     .WithReference(rabbitmqReference)
            //     .WaitFor(identityService)
            //     .WaitFor(rabbitmqReference)
            //     ;
            // ConfigureObservability(sagaOrchestrator);

            var classroomService = builder
                .AddProject<Projects.Classroom_API>("classroom-api")
                .WithExternalHttpEndpoints()
                .WithReference(classroomDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(classroomDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(classroomService);

            var resourceApiService = builder
                .AddProject<Projects.Resource_API>("resource-api")
                .WithExternalHttpEndpoints()
                .WithReference(resourceDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(resourceDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(resourceApiService);

            var notificationApiService = builder
                .AddProject<Projects.Notification_API>("notification-api")
                .WithExternalHttpEndpoints()
                .WithReference(notificationDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(notificationDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(notificationApiService);

            var productApiService = builder
                .AddProject<Projects.Product_API>("product-api")
                .WithExternalHttpEndpoints()
                .WithReference(productDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(productDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(productApiService);

            var orderApiService = builder
                .AddProject<Projects.Order_API>("order-api")
                .WithExternalHttpEndpoints()
                .WithReference(orderDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(orderDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(orderApiService);

            // var paymentApiService = builder
            //     .AddProject<Projects.Payment_API>("payment-api")
            //     .WithExternalHttpEndpoints()
            //     .WithReference(paymentDb)
            //     .WithReference(rabbitmq)
            //     .WaitFor(postgres)
            //     .WaitFor(paymentDb)
            //     .WaitFor(rabbitmq)
            //     ;
            // ConfigureObservability(paymentApiService);

            // var cartApiService = builder
            //     .AddProject<Projects.Cart_API>("cart-api")
            //     .WithExternalHttpEndpoints()
            //     .WithReference(cartDb)
            //     .WithReference(rabbitmq)
            //     .WaitFor(postgres)
            //     .WaitFor(cartDb)
            //     .WaitFor(rabbitmq)
            //     ;
            // ConfigureObservability(cartApiService);
           
            IResourceBuilder<IResourceWithConnectionString>? emulatorMongoDbReference = null;
            
                var mongo = builder.AddMongoDB("emulator-mongodb")
                                   .WithLifetime(ContainerLifetime.Persistent);
                
                var emulatorMongoDb = mongo.AddDatabase("stemify-emulator");
                emulatorMongoDbReference = emulatorMongoDb;
            

            var emulatorApiService = builder
                .AddProject<Projects.Emulator_API>("emulator-api")
                .WithExternalHttpEndpoints();
            
            if (emulatorMongoDbReference is not null)
            {
                emulatorApiService
                    .WithReference(emulatorMongoDbReference)
                    .WaitFor(emulatorMongoDbReference);
            }
            
            ConfigureObservability(emulatorApiService);

            var hangfireApiService = builder
                .AddProject<Projects.Hangfire_API>("hangfire-api")
                .WithExternalHttpEndpoints()
                .WithReference(hangfireDb)
                .WithReference(identityDb)
                .WithReference(orderDb)
                .WithReference(rabbitmqReference)
                .WaitFor(postgres)
                .WaitFor(hangfireDb)
                .WaitFor(identityDb)
                .WaitFor(orderDb)
                .WaitFor(rabbitmqReference)
                ;
            ConfigureObservability(hangfireApiService);

            
            var resourceGrpcPort = builder.Configuration["ResourceApi:GrpcPort"] ?? "5082";
            var resourceGrpcEndpoint = $"localhost:{resourceGrpcPort}";
            
            var aiService = builder
                .AddUvicornApp("ai-service", "../../AIService", "app.main:app")
                .WithExternalHttpEndpoints()
                .WithHttpHealthCheck("/health")
                .WithReference(aiMemoryDb)
                .WithReference(rabbitmqReference)
                .WithEnvironment("AI_MEMORY_DB_CONNECTION", aiMemoryDb.Resource.ConnectionStringExpression)
                .WithEnvironment("RABBITMQ_URL", rabbitmqReference.Resource.ConnectionStringExpression)
                .WithEnvironment("RESOURCE_GRPC_ENDPOINT", resourceGrpcEndpoint)
                .WithEnvironment("RESOURCE_GRPC_USE_TLS", "false")
                // Redis (optional) - use configuration or environment variables if provided
                .WithEnvironment("REDIS_HOST", builder.Configuration["AIService:Redis:Host"]
                    ?? Environment.GetEnvironmentVariable("REDIS_HOST") ?? string.Empty)
                .WithEnvironment("REDIS_PORT", builder.Configuration["AIService:Redis:Port"]
                    ?? Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379")
                .WithEnvironment("REDIS_PASSWORD", builder.Configuration["AIService:Redis:Password"]
                    ?? Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? string.Empty)
                .WithEnvironment("REDIS_SSL", builder.Configuration["AIService:Redis:SSL"]
                    ?? Environment.GetEnvironmentVariable("REDIS_SSL") ?? "false")
                .WithEnvironment("PYTHONUNBUFFERED", "1")
                .WithEnvironment("PYTHONDONTWRITEBYTECODE", "1")
                .WithEnvironment("PORT", "8000")
                .WithEnvironment("OPENAI_API_KEY", builder.Configuration["AIService:LLM:OpenAI:ApiKey"]
                    ?? builder.Configuration["OpenAI:ApiKey"]
                    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                    ?? "")
                .WithEnvironment("DEEPSEEK_API_KEY", builder.Configuration["AIService:LLM:DeepSeek:ApiKey"]
                    ?? builder.Configuration["DeepSeek:ApiKey"]
                    ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                    ?? "")
                .WithReference(resourceApiService)
                .WaitFor(resourceApiService)
                .WaitFor(rabbitmqReference);
            ConfigureObservability(aiService);

            var apiGateway = builder
                .AddProject<Projects.ApiGateway>("apigateway")
                .WithExternalHttpEndpoints()
                .WithReference(identityService)
                //    .WithReference(identityApiService)
                .WithReference(classroomService)
                .WithReference(resourceApiService)
                .WithReference(notificationApiService)
                .WithReference(emulatorApiService)
                .WaitFor(identityService)
                .WaitFor(resourceApiService)
                .WaitFor(classroomService)
                .WaitFor(notificationApiService)
                .WaitFor(productApiService)
                .WaitFor(orderApiService)
                .WaitFor(emulatorApiService)
                .WaitFor(aiService)
                ;
            ConfigureObservability(apiGateway);

            void ConfigureObservability<TResource>(IResourceBuilder<TResource> resourceBuilder)
                where TResource : IResourceWithEnvironment
            {
                var enabledValue = externalObservabilityEnabled ? "true" : "false";
                resourceBuilder.WithEnvironment("Observability__ExternalStackEnabled", enabledValue);

                if (grafanaEndpoint is not null)
                {
                    resourceBuilder.WithEnvironment("GRAFANA_URL", grafanaEndpoint);
                }
            }

            return builder;
        }

       
        private static string GetOrCreatePostgresSnapshotPath(bool isDevelopment)
        {
            // if (!isDevelopment)
            // {
            //     var productionPath = Path.Combine(
            //         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            //         "STEMify",
            //         "postgres-data");
            //     Directory.CreateDirectory(productionPath);
            //     return productionPath;
            // }

            var baseSnapshotsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "STEMify",
                "postgres-snapshots");

            
            Directory.CreateDirectory(baseSnapshotsPath);

            var mostRecentSnapshot = FindMostRecentSnapshot(baseSnapshotsPath);

           var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var newSnapshotPath = Path.Combine(baseSnapshotsPath, $"postgres-data-{timestamp}");
            
            if (mostRecentSnapshot != null && Directory.Exists(mostRecentSnapshot))
            {
                CopyDirectory(mostRecentSnapshot, newSnapshotPath);
                }
            else
            {
                
                Directory.CreateDirectory(newSnapshotPath);
            }

            return newSnapshotPath;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, overwrite: true);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        private static string? FindMostRecentSnapshot(string snapshotsBasePath)
        {
            if (!Directory.Exists(snapshotsBasePath))
                return null;

            var snapshotDirectories = Directory.GetDirectories(snapshotsBasePath, "postgres-data-*")
                .Where(dir =>
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName.Length < 24 || !dirName.StartsWith("postgres-data-"))
                        return false;
                    
                    var timestampPart = dirName.Substring("postgres-data-".Length);
                    return timestampPart.Length == 15 && // YYYYMMDD-HHmmss = 15 chars
                           timestampPart[8] == '-' &&
                           DateTime.TryParseExact(timestampPart, "yyyyMMdd-HHmmss", null, 
                               System.Globalization.DateTimeStyles.None, out _);
                })
                .OrderByDescending(dir =>
                {
                    var dirName = Path.GetFileName(dir);
                    var timestampPart = dirName.Substring("postgres-data-".Length);
                    if (DateTime.TryParseExact(timestampPart, "yyyyMMdd-HHmmss", null,
                        System.Globalization.DateTimeStyles.None, out var timestamp))
                    {
                        return timestamp;
                    }
                    return DateTime.MinValue;
                })
                .ToList();

            return snapshotDirectories.FirstOrDefault();
        }
    }
}
