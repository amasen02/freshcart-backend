namespace FreshCart.Catalog.Api.Features;

public static class CatalogEndpointConventions
{
    public const string ProductsRoute = "/products";
    public const string ProductsSearchRoute = "/products/search";
    public const string ProductByIdRoute = "/products/{productId:guid}";
    public const string ProductByIdOrSlugRoute = "/products/{idOrSlug}";
    public const string CategoriesRoute = "/categories";
    public const string BrandsRoute = "/brands";

    public const string ProductsTag = "Products";
    public const string CategoriesTag = "Categories";
    public const string BrandsTag = "Brands";
}
