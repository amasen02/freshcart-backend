using System.Net;
using System.Text;
using FluentAssertions;
using FreshCart.Basket.Api.Catalog;
using FreshCart.BuildingBlocks.Exceptions;
using Xunit;

namespace FreshCart.Basket.Tests.Catalog;

public sealed class CatalogProductClientTests
{
    private const string JsonMediaType = "application/json";

    private static readonly Uri CatalogBaseAddress = new("https://catalog.test");

    [Fact]
    public async Task UnknownProductMapsToNull()
    {
        var client = ClientReturning(new HttpResponseMessage(HttpStatusCode.NotFound), out _);

        var catalogProduct = await client.GetProductAsync(Guid.NewGuid(), CancellationToken.None);

        catalogProduct.Should().BeNull();
    }

    [Fact]
    public async Task ProductPayloadIsMappedCompletely()
    {
        var productId = Guid.NewGuid();
        var payload = $$"""
            {
              "id": "{{productId}}",
              "sku": "SKU-7001",
              "name": "Cold brew coffee 1L",
              "primaryCategory": "Beverages",
              "price": 6.40,
              "imageUrl": "https://cdn.freshcart.local/products/cold-brew.jpg",
              "isDigital": false,
              "isActive": true
            }
            """;
        var client = ClientReturning(JsonResponse(payload), out var stubHandler);

        var catalogProduct = await client.GetProductAsync(productId, CancellationToken.None);

        catalogProduct.Should().NotBeNull();
        catalogProduct!.ProductId.Should().Be(productId);
        catalogProduct.Sku.Should().Be("SKU-7001");
        catalogProduct.Name.Should().Be("Cold brew coffee 1L");
        catalogProduct.PrimaryCategory.Should().Be("Beverages");
        catalogProduct.Price.Should().Be(6.40m);
        catalogProduct.ImageUrl.Should().Be("https://cdn.freshcart.local/products/cold-brew.jpg");
        catalogProduct.IsDigital.Should().BeFalse();
        catalogProduct.IsActive.Should().BeTrue();

        stubHandler.LastRequestUri.Should().Be(new Uri(CatalogBaseAddress, $"/products/{productId}"));
    }

    [Fact]
    public Task NullJsonBodyIsAnInternalFault()
    {
        var client = ClientReturning(JsonResponse("null"), out _);

        var gettingProduct = () => client.GetProductAsync(Guid.NewGuid(), CancellationToken.None);

        return gettingProduct.Should().ThrowAsync<InternalServerException>();
    }

    [Fact]
    public Task ServerFailureSurfacesAsAnHttpError()
    {
        var client = ClientReturning(new HttpResponseMessage(HttpStatusCode.InternalServerError), out _);

        var gettingProduct = () => client.GetProductAsync(Guid.NewGuid(), CancellationToken.None);

        return gettingProduct.Should().ThrowAsync<HttpRequestException>();
    }

    private static CatalogProductClient ClientReturning(HttpResponseMessage response, out StubHttpMessageHandler stubHandler)
    {
        stubHandler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(stubHandler) { BaseAddress = CatalogBaseAddress };
        return new CatalogProductClient(httpClient);
    }

    private static HttpResponseMessage JsonResponse(string jsonPayload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(jsonPayload, Encoding.UTF8, JsonMediaType),
    };

    private sealed class StubHttpMessageHandler(HttpResponseMessage cannedResponse) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(cannedResponse);
        }
    }
}
