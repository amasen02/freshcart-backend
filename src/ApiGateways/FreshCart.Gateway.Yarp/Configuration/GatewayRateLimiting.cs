using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace FreshCart.Gateway.Yarp.Configuration;

/// <summary>
/// Edge rate limiting. A global fixed window throttles every client IP, and a stricter named policy
/// fronts the authentication routes where credential-stuffing is the real threat. The partition key is
/// the remote IP. Behind an ingress the real client address arrives via forwarded headers, which
/// <see cref="GatewayForwardedHeadersConfiguration"/> normalises before this limiter runs -- but only from
/// a hop named in <c>ForwardedHeaders:KnownProxies</c>/<c>KnownNetworks</c>. That restriction is what keeps
/// the partition key out of the caller's hands: an untrusted <c>X-Forwarded-For</c> would otherwise let one
/// client mint an unlimited number of partitions and walk straight through both limiters.
/// </summary>
public static class GatewayRateLimiting
{
    public const string AuthenticationPolicyName = "auth";

    private const int GlobalPermitLimit = 100;
    private static readonly TimeSpan GlobalWindow = TimeSpan.FromSeconds(10);

    private const int AuthenticationPermitLimit = 10;
    private static readonly TimeSpan AuthenticationWindow = TimeSpan.FromMinutes(1);

    private const string UnknownClientPartitionKey = "unknown";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = GlobalPermitLimit,
                        Window = GlobalWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            rateLimiterOptions.AddPolicy(AuthenticationPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthenticationPermitLimit,
                        Window = AuthenticationWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            rateLimiterOptions.OnRejected = (rejectionContext, cancellationToken) =>
            {
                if (rejectionContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    rejectionContext.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return UnknownClientPartitionKey;
        }

        var canonicalAddress = remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4()
            : remoteIpAddress;

        return canonicalAddress.ToString();
    }
}
