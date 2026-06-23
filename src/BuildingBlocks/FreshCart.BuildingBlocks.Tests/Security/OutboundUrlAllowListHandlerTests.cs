using System.Net;
using System.Net.Http;
using FluentAssertions;
using FreshCart.BuildingBlocks.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FreshCart.BuildingBlocks.Tests.Security;

public sealed class OutboundUrlAllowListHandlerTests
{
    private static readonly string[] AllowedFreshCartHosts = ["api.freshcart.local"];

    [Fact]
    public async Task BlocksRequestWhenTargetHostIsNotOnTheAllowList()
    {
        var handler = BuildHandler(AllowedFreshCartHosts);
        var httpClient = new HttpClient(handler);

        var response = await httpClient.GetAsync(new Uri("https://malicious.example.com/data"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.ReasonPhrase.Should().Contain("not on the outbound allow list");
    }

    [Fact]
    public async Task AllowsRequestWhenTargetHostIsOnTheAllowList()
    {
        var handler = BuildHandler(
            AllowedFreshCartHosts,
            innerResponse: new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);

        var response = await httpClient.GetAsync(new Uri("https://api.freshcart.local/products"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BlocksAzureInstanceMetadataServiceEvenWhenLocalNetworkOpensIt()
    {
        var handler = BuildHandler(AllowedFreshCartHosts);
        var httpClient = new HttpClient(handler);

        var response = await httpClient.GetAsync(new Uri("http://169.254.169.254/metadata/instance"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static OutboundUrlAllowListHandler BuildHandler(
        string[] allowedHosts,
        HttpResponseMessage? innerResponse = null)
    {
        var options = Options.Create(new OutboundUrlAllowListOptions { AllowedHosts = allowedHosts });
        var logger = Substitute.For<ILogger<OutboundUrlAllowListHandler>>();
        return new OutboundUrlAllowListHandler(options, logger)
        {
            InnerHandler = new StubHandler(innerResponse ?? new HttpResponseMessage(HttpStatusCode.OK)),
        };
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
