using Classroom.API.Consumers;
using Infrastructure.Middlewares;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Classroom.API.Extensions
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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = config["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false; 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateIssuer = false,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "platform_role"
        };
    });

            


            // Add Grpc
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GrpcExceptionInterceptor>();
            });

            // Configure MassTransit
            services.AddMassTransit(x =>
            {
                // Set endpoint for the exchange
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("classroom", false));

                // Enable the Entity Framework Outbox pattern using the AuctionDbContext.
                // This ensures messages are only published if the surrounding database transaction succeeds.
                //x.AddEntityFrameworkOutbox<ClassroomDbContext>(o =>
                //{
                //    o.QueryDelay = TimeSpan.FromSeconds(10);
                //    o.UsePostgres();
                //    o.UseBusOutbox();
                //});
                x.AddConsumer<CertificateGenerationRequestedConsumer>();

                x.UsingRabbitMq(
                    (context, cfg) =>
                    {
                        cfg.Host(
                            new Uri(config.GetConnectionString("messaging") ?? config["RabbitMq:Url"] ?? "amqp://guest:guest@localhost:5672")
                        );

                        cfg.ConfigureEndpoints(context);
                    }
                );
            });

            return services;
        }
    }
}
