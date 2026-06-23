using System.Net.Http.Headers;
using FreshCart.Ordering.Infrastructure.Security;

namespace FreshCart.Ordering.Infrastructure.Payment;

/// <summary>
/// Attaches the service-to-service bearer token to every outbound Payment request so the capture
/// endpoint's <c>ServiceCaller</c> policy is satisfied. Without it the saga's capture call is anonymous
/// and the Payment service rejects it.
/// </summary>
internal sealed class ServiceAuthenticationHandler(IServiceTokenProvider serviceTokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await serviceTokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
