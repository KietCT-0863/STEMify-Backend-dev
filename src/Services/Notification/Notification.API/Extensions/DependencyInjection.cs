using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Notification.Application.Common.Hubs;

namespace Notification.API.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services.AddDataProtection();

            services.AddRouting(opt => opt.LowercaseUrls = true);
            services.AddCors(options =>
            {
                options.AddPolicy(
                    "customPolicy",
                    b =>
                    {
                        var clientAppBaseUrl = config["ClientApp:BaseUrl"] ?? string.Empty;
                        var allowedOrigins = clientAppBaseUrl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        b.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithOrigins(allowedOrigins);
                    }
                );
            });

            // Add service for authentication
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = config["IdentityServiceUrl"];
                    options.RequireHttpsMetadata = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        NameClaimType = "username",
                        ValidIssuer = config["Issuer"],
                        // Fallback: if the token has no kid, try every signing key from the discovery document.
                        IssuerSigningKeyResolver = (_, _, keyId, validationParameters) =>
                        {
                            var configuration = options.ConfigurationManager?
                                .GetConfigurationAsync(CancellationToken.None)
                                .GetAwaiter()
                                .GetResult();

                            if (configuration is null)
                            {
                                return Enumerable.Empty<SecurityKey>();
                            }

                            return string.IsNullOrWhiteSpace(keyId)
                                ? configuration.SigningKeys
                                : configuration.SigningKeys.Where(k => string.Equals(k.KeyId, keyId, StringComparison.Ordinal));
                        }
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // Chỉ áp dụng cho kết nối đến SignalR hub
                            if (
                                !string.IsNullOrEmpty(accessToken)
                                && path.StartsWithSegments("/api/notifications")
                            )
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                    };
                });
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            return services;
        }
    }
}
