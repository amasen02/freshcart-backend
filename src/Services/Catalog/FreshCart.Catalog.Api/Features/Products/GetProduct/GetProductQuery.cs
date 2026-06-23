using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Products.GetProduct;

public sealed record GetProductQuery(string IdOrSlug) : IQuery<ProductDetailsDto>;
