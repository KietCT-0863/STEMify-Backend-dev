using ApiGateway.Extensions;
using ApiGateway.Middleware;
using BuildingBlocks.Authorization.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Allow large file uploads for presentation/lesson assets
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 250_000_000; // 250 MB
            });
            
            // Configure ForwardedHeaders to handle HTTPS properly behind reverse proxy (Cloudflare)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = 
                    ForwardedHeaders.XForwardedFor | 
                    ForwardedHeaders.XForwardedProto | 
                    ForwardedHeaders.XForwardedHost;
                
                // Trust all proxies (Cloudflare, load balancers, etc.)
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                
                // Required for proper HTTPS scheme detection
                options.ForwardLimit = null;
            });

            // Observability - logging + metrics
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Services.AddLogging();
            builder.Services.AddHttpLogging(o => { }); // Request/response logging
            builder
                .Services.AddOpenTelemetry()
                .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation())
                .WithTracing(t => t.AddAspNetCoreInstrumentation());

            builder.Services.AddApiServices(builder.Configuration);

            builder.Services.AddOrganizationPermissions();

            builder.Services.AddScoped<IAuthorizationHandler, Handlers.GatewayOrganizationPermissionHandler>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddAllOrganizationPermissionPolicies();

                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff"));
                options.AddPolicy("MemberOnly", policy => policy.RequireRole("Member"));

                options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff"));
                options.AddPolicy("AdminOrMember", policy => policy.RequireRole("Admin", "Member"));
                options.AddPolicy("StaffOrMember", policy => policy.RequireRole("Staff", "Member"));

                options.AddPolicy(
                    "AdminOrStaffOrMember",
                    policy => policy.RequireRole("Admin", "Staff", "Member")
                );

                options.AddPolicy(
                    "AllRoles",
                    policy => policy.RequireRole("Admin", "Staff", "Member")
                );


                options.AddPolicy("Management", policy => policy.RequireRole("Admin", "Staff"));


                // OBSOLETE: Use AdminOnly, StaffOnly, or MemberOnly instead
                options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Admin", "Staff", "Member"));

                // OBSOLETE: Use AdminOnly, StaffOnly, or MemberOnly instead
                options.AddPolicy("StudentOnly", policy => policy.RequireRole("Admin", "Staff", "Member"));

                // OBSOLETE: Use AdminOnly, StaffOnly, or MemberOnly instead
                options.AddPolicy("OrganizationAdminOnly", policy => policy.RequireRole("Admin", "Staff", "Member"));

                // Legacy combined policies - map to platform roles (OBSOLETE)
                options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStaff", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("StaffOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("StaffOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("StudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrStaff", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrStaffOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStaffOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrStaffOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStaffOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("StaffOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrStaffOrStudent", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrStaffOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrTeacherOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("AdminOrStaffOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));
                options.AddPolicy("TeacherOrStaffOrStudentOrOrganizationAdmin", policy => policy.RequireRole("Admin", "Staff", "Member"));

                // OBSOLETE: Use AdminOrStaffOrMember instead
                options.AddPolicy("EducationalStaff", policy => policy.RequireRole("Admin", "Staff", "Member"));

                // OBSOLETE: Use AdminOrStaff instead
                options.AddPolicy("Academic", policy => policy.RequireRole("Admin", "Staff", "Member"));
            });

            // Output Caching Configuration
            builder.Services.AddOutputCache(options =>
            {
                // Short cache: 30 seconds - for frequently changing data (skills, categories, questions)
                options.AddPolicy("short", builder => builder
                    .Expire(TimeSpan.FromSeconds(30))
                    .SetVaryByQuery("*")
                    .Tag("short-cache"));

                // Medium cache: 5 minutes - for moderately stable data (courses, lessons, kits)
                options.AddPolicy("medium", builder => builder
                    .Expire(TimeSpan.FromMinutes(5))
                    .SetVaryByQuery("*")
                    .Tag("medium-cache"));

                // Long cache: 15 minutes - for stable data (standards, tags, plans)
                options.AddPolicy("long", builder => builder
                    .Expire(TimeSpan.FromMinutes(15))
                    .SetVaryByQuery("*")
                    .Tag("long-cache"));

                // User-specific cache: 2 minutes - for user-related data
                options.AddPolicy("user-cache", builder => builder
                    .Expire(TimeSpan.FromMinutes(2))
                    .SetVaryByQuery("*")
                    .SetVaryByHeader("X-User-Id", "X-Active-Organization")
                    .Tag("user-cache"));

                // No cache for write operations and real-time data
                options.AddPolicy("no-cache", builder => builder
                    .NoCache());
            });

            // Rate Limiting Configuration
            builder.Services.AddRateLimiter(options =>
            {
                // Global rate limiter: 200 requests per minute per IP
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var userIdentifier = context.User.Identity?.IsAuthenticated == true
                        ? context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                        : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: userIdentifier,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 10,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                // Standard API rate limit: 100 requests per minute
                options.AddFixedWindowLimiter("standard", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 5;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // Read-heavy rate limit: 150 requests per minute
                options.AddFixedWindowLimiter("read-heavy", opt =>
                {
                    opt.PermitLimit = 150;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 10;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // Write operations rate limit: 50 requests per minute
                options.AddFixedWindowLimiter("write-limited", opt =>
                {
                    opt.PermitLimit = 50;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 2;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // AI operations rate limit: 20 requests per minute (expensive operations)
                options.AddFixedWindowLimiter("ai-limited", opt =>
                {
                    opt.PermitLimit = 20;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Authentication/Invitation rate limit: 30 requests per minute
                options.AddFixedWindowLimiter("auth-limited", opt =>
                {
                    opt.PermitLimit = 30;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 2;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                options.RejectionStatusCode = 429;
            });

            // YARP - Load configuration from appsettings.json
            builder
                .Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddTransforms(builderContext =>
                {
                    // Forward X-Forwarded-* headers to downstream services for proper HTTPS detection
                    builderContext.AddRequestTransform(async transformContext =>
                    {
                        // Forward scheme information to downstream services
                        var scheme = transformContext.HttpContext.Request.Scheme;
                        var host = transformContext.HttpContext.Request.Host.ToString();
                        
                        // Add X-Forwarded headers if not already present
                        if (!transformContext.ProxyRequest.Headers.Contains("X-Forwarded-Proto"))
                        {
                            transformContext.ProxyRequest.Headers.Add("X-Forwarded-Proto", scheme);
                        }
                        
                        if (!transformContext.ProxyRequest.Headers.Contains("X-Forwarded-Host"))
                        {
                            transformContext.ProxyRequest.Headers.Add("X-Forwarded-Host", host);
                        }
                        
                        transformContext.ProxyRequest.Headers.Add("X-Gateway", "YARP");
                        
                        await Task.CompletedTask;
                    });
                    
                    // Pass through authentication headers
                    builderContext.AddRequestTransform(async transformContext =>
                    {

                        // Pass through the original Authorization header
                        //if (transformContext.HttpContext.Request.Headers.ContainsKey("Authorization"))
                        //{
                        //    var authHeader = transformContext.HttpContext.Request.Headers["Authorization"].ToString();
                        //    transformContext.ProxyRequest.Headers.Add("Authorization", authHeader);
                        //}

                        // Add user context headers
                        if (transformContext.HttpContext.User.Identity?.IsAuthenticated == true)
                        {
                            if (transformContext.ProxyRequest.Headers.Contains("Authorization"))
                            {
                                transformContext.ProxyRequest.Headers.Remove("Authorization");
                            }

                            var authHeader = transformContext
                                .HttpContext.Request.Headers["Authorization"]
                                .ToString();
                            transformContext.ProxyRequest.Headers.Add("Authorization", authHeader);

                            var userId =
                                transformContext.HttpContext.User.FindFirst("sub")?.Value
                                ?? transformContext
                                    .HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                                    ?.Value;
                            var userRole = transformContext
                                .HttpContext.User.FindFirst(ClaimTypes.Role)
                                ?.Value;
                            var userName = transformContext
                                .HttpContext.User.FindFirst(ClaimTypes.Name)
                                ?.Value;

                            if (!string.IsNullOrEmpty(userId))
                                transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);

                            if (!string.IsNullOrEmpty(userRole))
                                transformContext.ProxyRequest.Headers.Add("X-User-Role", userRole);

                            if (!string.IsNullOrEmpty(userName))
                                transformContext.ProxyRequest.Headers.Add("X-User-Name", userName);
                        }

                        if (transformContext.HttpContext.Request.Headers.ContainsKey("X-Active-Organization"))
                        {
                            var activeOrg = transformContext.HttpContext.Request.Headers["X-Active-Organization"].ToString();
                            if (!string.IsNullOrEmpty(activeOrg))
                            {
                                transformContext.ProxyRequest.Headers.Add("X-Active-Organization", activeOrg);
                            }
                        }

                        if (transformContext.HttpContext.Request.Headers.ContainsKey("X-Active-Subscription"))
                        {
                            var activeSub = transformContext.HttpContext.Request.Headers["X-Active-Subscription"].ToString();
                            if (!string.IsNullOrEmpty(activeSub))
                            {
                                transformContext.ProxyRequest.Headers.Add("X-Active-Subscription", activeSub);
                            }
                        }

                        await Task.CompletedTask;
                    });

                    // Add CORS headers to response
                    builderContext.AddResponseTransform(async transformContext =>
                    {
                        var origin = transformContext.HttpContext.Request.Headers["Origin"].ToString();
                        if (!string.IsNullOrEmpty(origin))
                        {
                            // Check if origin is allowed
                            var config = transformContext.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                            var clientApps = config["ClientApp"]?
                                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                ?? Array.Empty<string>();
                            
                            var microbitApp = config["MicrobitApp"];
                            var allowedOrigins = clientApps
                                .Concat(string.IsNullOrWhiteSpace(microbitApp) ? Array.Empty<string>() : new[] { microbitApp })
                                .Select(o => o.Trim())
                                .Where(o => !string.IsNullOrEmpty(o))
                                .ToArray();

                            // Log for debugging
                            var logger = transformContext.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("CORS Transform - Origin: {Origin}, Allowed: {Allowed}", origin, string.Join(", ", allowedOrigins));

                            if (allowedOrigins.Any(allowed => allowed.Equals(origin, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Remove any existing CORS headers to avoid duplicates
                                transformContext.HttpContext.Response.Headers.Remove("Access-Control-Allow-Origin");
                                transformContext.HttpContext.Response.Headers.Remove("Access-Control-Allow-Credentials");
                                transformContext.HttpContext.Response.Headers.Remove("Access-Control-Allow-Methods");
                                transformContext.HttpContext.Response.Headers.Remove("Access-Control-Allow-Headers");
                                
                                // Add CORS headers
                                transformContext.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                                transformContext.HttpContext.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                                transformContext.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS");
                                transformContext.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Active-Organization, X-Active-Subscription");
                                
                                logger.LogInformation("CORS headers added for origin: {Origin}", origin);
                            }
                            else
                            {
                                logger.LogWarning("Origin {Origin} not in allowed list", origin);
                            }
                        }
                        await Task.CompletedTask;
                    });
                })
                .AddConfigFilter<AuthorizationPolicyConfigFilter>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Health checks
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Apply ForwardedHeaders middleware FIRST to properly detect HTTPS scheme
            app.UseForwardedHeaders();

            //app.UseWhen(
            //    ctx => ctx.Request.Path.StartsWithSegments("/api/ai"),
            //    branch =>
            //    {
            //        branch.UseCors("aiPublic");
            //    });
            
            app.UseCors("customPolicy");
            // Observability
            app.UseHttpLogging();

            app.UseMiddleware<ErrorWrappingApiMiddleware>();
            app.UseMiddleware<ApiResultWrappingMiddleware>();

            app.UseHttpsRedirection();

            app.UseRateLimiter();
            app.UseOutputCache();

            app.UseAuthentication();
            app.UseAuthorization();


            // API Gateway
            app.MapReverseProxy(proxyPipeline =>
            {
                proxyPipeline.UseAuthentication();
                proxyPipeline.UseAuthorization();

                // Convert JSON { redirect_url: "..." } from downstream into 302 redirect for selected routes
                proxyPipeline.Use(async (context, next) =>
                {
                    // Only apply to the specific OAuth routes
                    var path = context.Request.Path.Value ?? string.Empty;
                    var applies = path.Equals("/api/invitations/accept-oauth", StringComparison.OrdinalIgnoreCase)
                                  || path.Equals("/api/auth/google/callback", StringComparison.OrdinalIgnoreCase);

                    if (!applies)
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    var originalBody = context.Response.Body;
                    await using var buffer = new MemoryStream();
                    context.Response.Body = buffer;

                    try
                    {
                        await next().ConfigureAwait(false);

                        buffer.Position = 0;
                        if (buffer.Length == 0)
                        {
                            buffer.Position = 0;
                            await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                            return;
                        }

                        // Only inspect JSON responses
                        var contentType = context.Response.ContentType ?? string.Empty;
                        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                        {
                            buffer.Position = 0;
                            await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                            return;
                        }

                        try
                        {
                            buffer.Position = 0;
                            using var doc = await JsonDocument.ParseAsync(buffer, cancellationToken: context.RequestAborted).ConfigureAwait(false);

                            string? redirectUrl = null;
                            var root = doc.RootElement;

                            // Check root level: redirect_url or redirectUrl
                            if (root.TryGetProperty("redirect_url", out var redirectPropSnake)
                                && redirectPropSnake.ValueKind == JsonValueKind.String)
                            {
                                redirectUrl = redirectPropSnake.GetString();
                            }
                            else if (root.TryGetProperty("redirectUrl", out var redirectPropCamel)
                                && redirectPropCamel.ValueKind == JsonValueKind.String)
                            {
                                redirectUrl = redirectPropCamel.GetString();
                            }
                            // Check in data object (for wrapped responses)
                            else if (root.TryGetProperty("data", out var dataNode)
                                     && dataNode.ValueKind == JsonValueKind.Object)
                            {
                                // Check data.redirect_url
                                if (dataNode.TryGetProperty("redirect_url", out var dataRedirectSnake)
                                    && dataRedirectSnake.ValueKind == JsonValueKind.String)
                                {
                                    redirectUrl = dataRedirectSnake.GetString();
                                }
                                // Check data.redirectUrl
                                else if (dataNode.TryGetProperty("redirectUrl", out var dataRedirectCamel)
                                    && dataRedirectCamel.ValueKind == JsonValueKind.String)
                                {
                                    redirectUrl = dataRedirectCamel.GetString();
                                }
                                // Check if data itself is an object with redirect_url (nested structure)
                                else if (dataNode.ValueKind == JsonValueKind.Object)
                                {
                                    // Try to find redirect_url in nested data structure
                                    if (dataNode.TryGetProperty("redirect_url", out var nestedRedirectSnake)
                                        && nestedRedirectSnake.ValueKind == JsonValueKind.String)
                                    {
                                        redirectUrl = nestedRedirectSnake.GetString();
                                    }
                                    else if (dataNode.TryGetProperty("redirectUrl", out var nestedRedirectCamel)
                                        && nestedRedirectCamel.ValueKind == JsonValueKind.String)
                                    {
                                        redirectUrl = nestedRedirectCamel.GetString();
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(redirectUrl))
                            {
                                context.Response.Clear();
                                context.Response.StatusCode = StatusCodes.Status302Found;
                                context.Response.Headers["Location"] = redirectUrl!;
                                return;
                            }
                        }
                        catch
                        {
                            // Ignore invalid JSON and fall through to normal response
                        }

                        // Fall back to original downstream response
                        buffer.Position = 0;
                        await buffer.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
                    }
                    finally
                    {
                        context.Response.Body = originalBody;
                    }
                });
            });

            app.MapHealthChecks("/health");

            app.MapGet("/", () => "API Gateway is running");

            app.Run();
        }
    }
}
