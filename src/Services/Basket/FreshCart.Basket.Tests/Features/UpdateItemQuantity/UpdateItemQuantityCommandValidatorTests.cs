using FluentValidation.TestHelper;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Api.Features.ShoppingBaskets.UpdateItemQuantity;
using Xunit;

namespace FreshCart.Basket.Tests.Features.UpdateItemQuantity;

public sealed class UpdateItemQuantityCommandValidatorTests
{
    private readonly UpdateItemQuantityCommandValidator _validator = new();

    [Fact]
    public void ZeroQuantityIsAllowedBecauseItMeansRemoveTheLine()
    {
        var result = _validator.TestValidate(new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeQuantityFails()
    {
        var result = _validator.TestValidate(new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        result.ShouldHaveValidationErrorFor(command => command.Quantity);
    }

    [Fact]
    public void QuantityAboveThePerLineMaximumFails()
    {
        var result = _validator.TestValidate(
            new UpdateItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), ShoppingBasket.MaxQuantityPerLine + 1));

        result.ShouldHaveValidationErrorFor(command => command.Quantity);
    }

    [Fact]
    public void EmptyIdentifiersFail()
    {
        var result = _validator.TestValidate(new UpdateItemQuantityCommand(Guid.Empty, Guid.Empty, 1));

        result.ShouldHaveValidationErrorFor(command => command.CustomerId);
        result.ShouldHaveValidationErrorFor(command => command.ProductId);
    }
}
