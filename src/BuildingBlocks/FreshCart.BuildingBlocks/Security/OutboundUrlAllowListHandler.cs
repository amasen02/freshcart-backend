using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreshCart.BuildingBlocks.Security;

/// <summary>
/// Delegating handler that rejects any outbound request whose host is not on the configured
/// allow list. Defence-in-depth against SSRF: even a vulnerable downstream cannot be coerced
/// into reaching arbitrary internal endpoints, including the cloud instance-metadata service.
/// </summary>
public sealed partial class OutboundUrlAllowListHandler(
    IOptions<OutboundUrlAllowListOptions> options,
    ILogger<OutboundUrlAllowListHandler> logger) : DelegatingHandler
{
    private const string BlockedReasonPhrase = "Destination host is not on the outbound allow list.";

    private readonly HashSet<string> allowedHosts = new(
        options.Value.AllowedHosts ?? Array.Empty<string>(),
        StringComparer.OrdinalIgnoreCase);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targetHost = request.RequestUri?.Host;
        if (string.IsNullOrWhiteSpace(targetHost) || !allowedHosts.Contains(targetHost))
        {
            LogBlockedOutboundCall(targetHost, request.RequestUri);

            return new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                RequestMessage = request,
                ReasonPhrase = BlockedReasonPhrase,
            };
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Warning,
        Message = "Blocked outbound HTTP call to disallowed host {TargetHost} ({TargetUri})")]
    private partial void LogBlockedOutboundCall(string? targetHost, Uri? targetUri);
}
