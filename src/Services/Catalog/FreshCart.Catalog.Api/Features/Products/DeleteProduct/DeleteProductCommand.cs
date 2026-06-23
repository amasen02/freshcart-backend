using FreshCart.BuildingBlocks.CQRS;

namespace FreshCart.Catalog.Api.Features.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : ICommand;
