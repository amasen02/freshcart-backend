using FluentAssertions;
using FreshCart.Catalog.Api.Features.Categories.GetCategories;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Categories;

public sealed class CategoryTreeBuilderTests
{
    [Fact]
    public void NestsChildrenUnderTheirParentsAcrossMultipleLevels()
    {
        var softwareId = Guid.NewGuid();
        var developerToolsId = Guid.NewGuid();
        var profilersId = Guid.NewGuid();

        var categoryTree = CategoryTreeBuilder.Build(
        [
            CreateCategory(softwareId, "Software", sortOrder: 1),
            CreateCategory(developerToolsId, "Developer Tools", sortOrder: 1, parentCategoryId: softwareId),
            CreateCategory(profilersId, "Profilers", sortOrder: 1, parentCategoryId: developerToolsId),
        ]);

        var softwareNode = categoryTree.Should().ContainSingle().Subject;
        softwareNode.Id.Should().Be(softwareId);
        var developerToolsNode = softwareNode.Children.Should().ContainSingle().Subject;
        developerToolsNode.Id.Should().Be(developerToolsId);
        developerToolsNode.Children.Should().ContainSingle().Which.Id.Should().Be(profilersId);
    }

    [Fact]
    public void PromotesOrphansWhoseParentIsAbsentToTheRootSoTheyStayReachable()
    {
        var missingParentId = Guid.NewGuid();
        var orphanId = Guid.NewGuid();

        var categoryTree = CategoryTreeBuilder.Build(
        [
            CreateCategory(Guid.NewGuid(), "Software", sortOrder: 1),
            CreateCategory(orphanId, "Orphaned Subcategory", sortOrder: 2, parentCategoryId: missingParentId),
        ]);

        categoryTree.Should().HaveCount(2);
        var orphanNode = categoryTree.Single(rootNode => rootNode.Id == orphanId);
        orphanNode.ParentCategoryId.Should().Be(missingParentId);
        orphanNode.Children.Should().BeEmpty();
    }

    [Fact]
    public void OrdersEveryLevelBySortOrderThenByName()
    {
        var parentId = Guid.NewGuid();

        var categoryTree = CategoryTreeBuilder.Build(
        [
            CreateCategory(Guid.NewGuid(), "Zeta Root", sortOrder: 1),
            CreateCategory(Guid.NewGuid(), "Alpha Root", sortOrder: 2),
            CreateCategory(parentId, "Parent", sortOrder: 3),
            CreateCategory(Guid.NewGuid(), "Beta Child", sortOrder: 2, parentCategoryId: parentId),
            CreateCategory(Guid.NewGuid(), "Alpha Child", sortOrder: 2, parentCategoryId: parentId),
            CreateCategory(Guid.NewGuid(), "First Child", sortOrder: 1, parentCategoryId: parentId),
        ]);

        categoryTree.Select(rootNode => rootNode.Name)
            .Should().ContainInOrder("Zeta Root", "Alpha Root", "Parent");

        categoryTree.Single(rootNode => rootNode.Id == parentId)
            .Children.Select(childNode => childNode.Name)
            .Should().ContainInOrder("First Child", "Alpha Child", "Beta Child");
    }

    [Fact]
    public void ReturnsAnEmptyTreeForAnEmptyCatalog()
    {
        CategoryTreeBuilder.Build([]).Should().BeEmpty();
    }

    private static Category CreateCategory(
        Guid categoryId,
        string name,
        int sortOrder,
        Guid? parentCategoryId = null) => new()
    {
        Id = categoryId,
        Name = name,
        Slug = name.Replace(' ', '-').ToLowerInvariant(),
        ParentCategoryId = parentCategoryId,
        SortOrder = sortOrder,
        IsActive = true,
    };
}
