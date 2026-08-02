using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RateLimitingOptions options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Sliding window rather than fixed: a fixed window lets a caller spend its entire
            // budget in the last second of one window and again in the first second of the next,
            // producing a burst of twice the intended rate across the boundary.
            limiter.AddPolicy(RateLimitPolicies.PerUser, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetPartitionKey(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PerUserPermitLimit,
                        Window = TimeSpan.FromSeconds(options.PerUserWindowSeconds),
                        SegmentsPerWindow = 6,
                        QueueLimit = options.PerUserQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            limiter.AddPolicy(RateLimitPolicies.Sensitive, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.SensitivePermitLimit,
                        Window = TimeSpan.FromSeconds(options.SensitiveWindowSeconds),
                        QueueLimit = 0
                    }));

            limiter.AddPolicy(RateLimitPolicies.Burst, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetPartitionKey(httpContext),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.BurstTokenLimit,
                        TokensPerPeriod = options.BurstTokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(options.BurstReplenishmentPeriodSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                // Telling the client when to come back is the difference between a well-behaved
                // caller and one that hammers the endpoint until it succeeds.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                ILogger logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Rate limit rejected {Method} {Path} for partition {Partition}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    GetPartitionKey(context.HttpContext));

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too Many Requests",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Request quota exceeded. Retry after the interval indicated by the Retry-After header.",
                        errorCode = "General.RateLimited",
                        traceId = context.HttpContext.TraceIdentifier
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Partitions by authenticated subject where possible, falling back to remote IP.
    /// <para>
    /// IP alone is a poor key: everyone behind one corporate NAT shares a budget, so a single
    /// enthusiastic user locks out their colleagues. Behind a proxy it is also attacker-controlled
    /// unless <c>ForwardedHeaders</c> is configured with a known-proxy allowlist.
    /// </para>
    /// </summary>
    private static string GetPartitionKey(HttpContext httpContext)
    {
        string? subject = httpContext.User.FindFirstValue("oid")
                          ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"user:{subject}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
