using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;

namespace ApiGateway.Extensions
{
    public static class DependencyInjection
    {
        private static bool TryGetKidFromToken(string token, out string? kid)
        {
            kid = null;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    kid = jwt.Header.Kid;
                    return !string.IsNullOrEmpty(kid);
                }
            }
            catch
            {
            }
            return false;
        }
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            // Change routing urls to lowercase
            services.AddRouting(opt => opt.LowercaseUrls = true);
            var clientApps = config["ClientApp"]?
          .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
          ?? Array.Empty<string>();

            var microbitApp = config["MicrobitApp"];

            var origins = clientApps
                .Concat(string.IsNullOrWhiteSpace(microbitApp)
                    ? Array.Empty<string>()
                    : new[] { microbitApp });
           services.AddCors(options =>
            {
                options.AddPolicy(
                    "customPolicy",
                    b =>
                    {
                         var clientApps = config["ClientApp"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        var microbitApp = config["MicrobitApp"];

        var origins = clientApps
            .Concat(string.IsNullOrWhiteSpace(microbitApp) 
                ? Array.Empty<string>() 
                : new[] { microbitApp })
            .ToArray();
                        b.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithOrigins(origins);
                    }
                );
                // options.AddPolicy("aiPublic", b =>
                // {
                //     b.AllowAnyHeader()
                //      .AllowAnyMethod()
                //      .SetIsOriginAllowed(_ => true);
                // });
            });

            // Add service for authentication
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = config["IdentityServiceUrl"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.TokenValidationParameters.NameClaimType = "username";
                    options.TokenValidationParameters.RoleClaimType = "platform_role";
                    options.TokenValidationParameters.ValidIssuer = config["Issuer"];
                    options.RefreshOnIssuerKeyNotFound = true;
                    options.MetadataAddress = $"{config["IdentityServiceUrl"]}/.well-known/openid-configuration";
                    
                    options.RefreshInterval = TimeSpan.FromHours(1);
                    options.AutomaticRefreshInterval = TimeSpan.FromHours(1);
                    
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetService<ILogger<JwtBearerEvents>>();
                            
                            var exception = context.Exception;
                            var token = context.Request.Headers["Authorization"].ToString()
                                .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                            
                            logger?.LogWarning(
                                "JWT Authentication failed: {Error}. Token has kid: {HasKid}, TokenValidationParameters keys: {KeyCount}",
                                exception.Message,
                                !string.IsNullOrEmpty(token) && TryGetKidFromToken(token, out _),
                                context.Options.TokenValidationParameters?.IssuerSigningKeys?.Count() ?? 0
                            );
                            

                            if (exception is SecurityTokenSignatureKeyNotFoundException sigEx)
                            {
                                logger?.LogError(
                                    "Signature key not found. Token kid: {Kid}, Available keys: {KeyCount}",
                                    TryGetKidFromToken(token, out var kid) ? kid : "NONE",
                                    context.Options.TokenValidationParameters?.IssuerSigningKeys?.Count() ?? 0
                                );
                            }
                            
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetService<ILogger<JwtBearerEvents>>();
                            logger?.LogDebug(
                                "JWT Token validated for user: {User}",
                                context.Principal?.Identity?.Name
                            );
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetService<ILogger<JwtBearerEvents>>();
                            logger?.LogWarning(
                                "JWT Challenge triggered: {Error}, {ErrorDescription}",
                                context.Error,
                                context.ErrorDescription
                            );
                            
                            // Prevent automatic redirect to login page for API endpoints
                            // Return 401 instead of redirecting
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            var result = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                data = (object?)null,
                                isSucceeded = false,
                                message = "Unauthorized. Please login first.",
                                statusCode = 401
                            });
                            return context.Response.WriteAsync(result);
                        }
                    };
                });

            // Add Swagger/OpenAPI support
            //services.AddSwaggerGen(c =>
            //{
            //    // Add Bearer token support
            //    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //    {
            //        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            //        Name = "Authorization",
            //        In = ParameterLocation.Header,
            //        Type = SecuritySchemeType.Http,
            //        Scheme = "bearer",
            //        BearerFormat = "JWT"
            //    });

            //    // Add security scheme in OpenAPI
            //    c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //    {
            //        {
            //            new OpenApiSecurityScheme
            //            {
            //                Reference = new OpenApiReference
            //                {
            //                    Type = ReferenceType.SecurityScheme,
            //                    Id = "Bearer"
            //                }
            //            },
            //            new string[] {}
            //        }
            //    });
            //});

            return services;
        }
    }
}
