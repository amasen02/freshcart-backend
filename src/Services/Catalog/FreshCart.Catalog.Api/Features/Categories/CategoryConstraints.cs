namespace FreshCart.Catalog.Api.Features.Categories;

/// <summary>
/// Domain limits shared by the category validators and the tree endpoint.
/// </summary>
public static class CategoryConstraints
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1000;
    public const int MaxSortOrder = 10_000;
}
