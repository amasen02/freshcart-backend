using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.AddItem;
using Xunit;

namespace FreshCart.Basket.Tests.Features.AddItem;

public sealed class AddItemCommandValidatorTests
{
    private readonly AddItemCommandValidator _validator = new();

    [Fact]
    public void WellFormedCommandPasses()
    {
        var result = _validator.TestValidate(new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 3));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCustomerIdFails()
    {
        var result = _validator.TestValidate(new AddItemCommand(Guid.Empty, Guid.NewGuid(), 1));

        result.ShouldHaveValidationErrorFor(command => command.CustomerId);
    }

    [Fact]
    public void EmptyProductIdFails()
    {
        var result = _validator.TestValidate(new AddItemCommand(Guid.NewGuid(), Guid.Empty, 1));

        result.ShouldHaveValidationErrorFor(command => command.ProductId);
    }

    [Fact]
    public void ZeroQuantityFails()
    {
        var result = _validator.TestValidate(new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.ShouldHaveValidationErrorFor(command => command.Quantity);
    }

    [Fact]
    public void QuantityAboveThePerLineMaximumFails()
    {
        var result = _validator.TestValidate(
            new AddItemCommand(Guid.NewGuid(), Guid.NewGuid(), ShoppingBasket.MaxQuantityPerLine + 1));

        result.ShouldHaveValidationErrorFor(command => command.Quantity);
    }
}
