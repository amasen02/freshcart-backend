namespace FreshCart.Catalog.Api.Models;

public sealed class Category
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedOnUtc { get; init; }
}
