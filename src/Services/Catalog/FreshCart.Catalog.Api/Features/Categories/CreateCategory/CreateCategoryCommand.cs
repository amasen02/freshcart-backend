using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder) : ICommand<CreateCategoryResult>;
