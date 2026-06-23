using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Brands.GetBrands;

public sealed record GetBrandsQuery : IQuery<IReadOnlyList<BrandDto>>;
