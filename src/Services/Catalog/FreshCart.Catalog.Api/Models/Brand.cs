namespace FreshCart.Catalog.Api.Models;

public sealed class Brand
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedOnUtc { get; init; }
}
