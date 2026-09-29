using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace WestCoastFitness.Api;

public static class RateLimitPolicies
{
    public const string Auth = "auth";

    /// <summary>Adds a per-client global limiter plus a much stricter one for login and registration.</summary>
    public static IServiceCollection AddClubRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 120);
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:WindowSeconds", 60));

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Fixed(permitLimit, window)));
            options.AddPolicy(Auth, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Fixed(authPermitLimit, window)));
        });
    }

    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions Fixed(int permitLimit, TimeSpan window) =>
        new() { PermitLimit = permitLimit, Window = window, QueueLimit = 0 };
}
