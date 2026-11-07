using System.Threading.RateLimiting;
using System.Security.Claims;
using OAS.Contracts.Common.Errors;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using OAS.API.Database;
using OAS.API.Printing;
using OAS.API.Security;
using OAS.Application.Abstractions.Security;
using OAS.Application.Database.Abstractions;

namespace OAS.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHttpContextAccessor();

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IPrintJobQueue, PrintJobQueue>();
        services.AddSingleton<DesktopPrintApiKeyValidator>();
        services.AddScoped<VoucherPrintPayloadFactory>();
        services.AddScoped<AccountingReportPrintPayloadFactory>();
        services.AddScoped<SupplierReportSnapshotService>();

        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<ICurrentRequestInfo, HttpCurrentRequestInfo>();

        services.AddScoped<IPermissionChecker, ClaimsPermissionChecker>();
        services.AddScoped<IDatabaseProfileSelection, HttpDatabaseProfileSelection>();
        services.AddScoped<IIdentityCookieSessionService, IdentityCookieSessionService>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-OAS.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(configuration.GetValue("Authentication:SessionHours", 8));
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("database-bootstrap", limiter =>
            {
                limiter.PermitLimit = 20;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
                limiter.AutoReplenishment = true;
            });
            // Authentication must run before the limiter so every signed-in user
            // receives an independent request budget (not one shared by the ERP).
            options.AddPolicy("api", context =>
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? context.User.Identity?.Name;
                var partition = !string.IsNullOrWhiteSpace(userId)
                    ? $"user:{userId}"
                    : $"ip:{context.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(new ApiError
                {
                    Code = "api_rate_limited",
                    Status = StatusCodes.Status429TooManyRequests,
                    Message = "تم تجاوز عدد الطلبات المسموح. أعد المحاولة بعد قليل."
                }, cancellationToken);
            };
            options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length > 0)
        {
            services.AddCors(options => options.AddPolicy("ConfiguredOrigins", policy =>
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        }
        return services;
    }
}
