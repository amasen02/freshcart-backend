using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Brands.CreateBrand;

public sealed record CreateBrandCommand(string Name, string? LogoUrl) : ICommand<CreateBrandResult>;
