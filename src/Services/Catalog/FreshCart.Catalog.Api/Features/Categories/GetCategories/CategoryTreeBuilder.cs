using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Categories.GetCategories;

/// <summary>
/// Pure in-memory assembly of the nested category tree. A category whose parent is absent from the
/// input (deactivated or deleted) is promoted to the root level instead of being dropped, so its
/// whole subtree stays reachable on the storefront. Every level is ordered by sort order, then name,
/// so the rendered menu is deterministic.
/// </summary>
public static class CategoryTreeBuilder
{
    public static IReadOnlyList<CategoryNodeDto> Build(IReadOnlyCollection<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);

        var knownCategoryIds = categories.Select(category => category.Id).ToHashSet();

        var childrenByParentId = categories
            .Where(category => category.ParentCategoryId is { } parentId && knownCategoryIds.Contains(parentId))
            .ToLookup(category => category.ParentCategoryId!.Value);

        var rootCategories = categories.Where(category =>
            category.ParentCategoryId is null
            || !knownCategoryIds.Contains(category.ParentCategoryId.Value));

        return BuildLevel(rootCategories, childrenByParentId);
    }

    private static List<CategoryNodeDto> BuildLevel(
        IEnumerable<Category> levelCategories,
        ILookup<Guid, Category> childrenByParentId) =>
        levelCategories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(category => new CategoryNodeDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ParentCategoryId,
                category.SortOrder,
                BuildLevel(childrenByParentId[category.Id], childrenByParentId)))
            .ToList();
}
