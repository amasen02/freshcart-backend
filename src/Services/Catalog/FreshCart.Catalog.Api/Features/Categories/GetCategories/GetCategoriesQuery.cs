using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Categories.GetCategories;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryNodeDto>>;
