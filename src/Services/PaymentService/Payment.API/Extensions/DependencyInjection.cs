using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using MassTransit;
using Payment.Infrastructure.Persistence;

namespace Payment.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Controllers
            services.AddControllers();

            // Health Checks
            services.AddHealthChecks();

            // Data Protection & Routing
            services.AddDataProtection();
            services.AddRouting(opt => opt.LowercaseUrls = true);

            // Configure MassTransit
            services.AddMassTransit(x =>
                {
                    // Set endpoint name formatter
                    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payment", false));

                    // Register consumers
                    x.AddConsumersFromNamespaceContaining<Payment.API.Consumers.OrderCreatedConsumer>();

                    // Enable the Entity Framework Outbox pattern
                    x.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
                    {
                        // How often to check for pending outbox messages
                        o.QueryDelay = TimeSpan.FromSeconds(10);

                        // Use PostgreSQL for outbox storage
                        o.UsePostgres();

                        // Use bus outbox to send messages via the bus
                        o.UseBusOutbox();
                    });

                    x.UsingRabbitMq(
                        (context, cfg) =>
                        {
                            var rabbitMqConnection = configuration.GetConnectionString("messaging")
                               ?? configuration["RabbitMq:Url"]
                               ?? "amqp://guest:guest@localhost:5672";

                            cfg.Host(new Uri(rabbitMqConnection));

                            // Enable message retry for transient failures
                            cfg.UseMessageRetry(r =>
                            {
                                r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
                            });

                            // Configure endpoints with Inbox pattern for idempotency
                            cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("payment", false));
                        }
                    );
                });

            // Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Payment Service API",
                    Version = "v1",
                    Description = "Payment gateway integration service with gRPC and REST support"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Authentication 
            /*
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["Authentication:Authority"];
                    options.Audience = configuration["Authentication:Audience"];
                    options.RequireHttpsMetadata = false; // Only for development
                });
            */

            // Authorization
            services.AddAuthorization();

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}
