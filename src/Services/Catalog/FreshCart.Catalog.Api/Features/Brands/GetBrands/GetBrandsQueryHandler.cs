using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Catalog.Api.Data;

namespace FreshCart.Catalog.Api.Features.Brands.GetBrands;

public sealed class GetBrandsQueryHandler(ICatalogQueries catalogQueries)
    : IQueryHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{
    public async Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var activeBrands = await catalogQueries.GetActiveBrandsAsync(cancellationToken).ConfigureAwait(false);
        return activeBrands.Select(BrandDto.FromBrand).ToList();
    }
}
