using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Brands;

public sealed record BrandDto(Guid Id, string Name, string Slug, string? LogoUrl)
{
    public static BrandDto FromBrand(Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        return new BrandDto(brand.Id, brand.Name, brand.Slug, brand.LogoUrl);
    }
}
