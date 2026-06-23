using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Installs a request transform on every YARP route that attaches the BFF bearer token. Because it is
/// registered as an <see cref="ITransformProvider"/> it runs for ordinary HTTP proxying and for the
/// WebSocket upgrade requests that carry the SignalR hubs, so the hubs receive the same downstream
/// JWT as the REST routes.
/// </summary>
public sealed class TokenExchangeTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.AddRequestTransform(transformContext =>
        {
            var httpContext = transformContext.HttpContext;

            if (!DownstreamAuthorizationPolicy.ShouldExchangeCookieForBearerToken(httpContext))
            {
                return ValueTask.CompletedTask;
            }

            var tokenExchanger = httpContext.RequestServices.GetRequiredService<CookieToJwtTokenExchanger>();
            var bearerToken = tokenExchanger.ExchangeForBearerToken(httpContext.User);

            transformContext.ProxyRequest.Headers.Remove(HeaderNames.Authorization);
            transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                HeaderNames.Authorization,
                DownstreamAuthorizationPolicy.BuildAuthorizationHeaderValue(bearerToken));

            return ValueTask.CompletedTask;
        });
    }
}
