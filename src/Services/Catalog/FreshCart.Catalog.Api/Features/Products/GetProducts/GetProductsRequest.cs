namespace FreshCart.Catalog.Api.Features.Products.GetProducts;

public sealed record GetProductsRequest(
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MaxPrice = null,
    bool? IsDigital = null,
    string? Sort = null,
    int PageNumber = 1,
    int PageSize = 20);
