using System.Net;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Basket.Api.Catalog;

/// <summary>
/// Typed HttpClient over Catalog's <c>GET /products/{id}</c>. Resilience (retry, circuit breaker,
/// timeout) comes from the standard handler that ServiceDefaults applies to every HttpClient.
/// </summary>
public sealed class CatalogProductClient(HttpClient httpClient) : ICatalogProductClient
{
    public async Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .GetAsync(new Uri($"/products/{productId}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var productPayload = await response.Content
            .ReadFromJsonAsync<CatalogProductPayload>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InternalServerException("Catalog returned an empty product payload.");

        return new CatalogProduct(
            productPayload.Id,
            productPayload.Sku,
            productPayload.Name,
            productPayload.PrimaryCategory,
            productPayload.Price,
            productPayload.ImageUrl,
            productPayload.IsDigital,
            productPayload.IsActive);
    }

    private sealed record CatalogProductPayload(
        Guid Id,
        string Sku,
        string Name,
        string PrimaryCategory,
        decimal Price,
        string? ImageUrl,
        bool IsDigital,
        bool IsActive);
}
