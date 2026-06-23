namespace FreshCart.Catalog.Api.Features.Categories;

public sealed record CategoryNodeDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    IReadOnlyList<CategoryNodeDto> Children);
