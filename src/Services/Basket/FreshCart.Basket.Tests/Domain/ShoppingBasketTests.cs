using FluentAssertions;
using FreshCart.Basket.Api.Domain;
using FreshCart.Basket.Tests.Support;
using Xunit;

namespace FreshCart.Basket.Tests.Domain;

public sealed class ShoppingBasketTests
{
    [Fact]
    public void AddingANewProductAppendsALine()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        var item = TestBasketItems.Create(quantity: 3);

        basket.AddOrMergeItem(item);

        basket.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
        basket.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void AddingAnExistingProductMergesQuantitiesInsteadOfDuplicatingTheLine()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId, quantity: 2));

        basket.AddOrMergeItem(TestBasketItems.Create(productId, quantity: 5));

        basket.Items.Should().ContainSingle().Which.Quantity.Should().Be(7);
    }

    [Fact]
    public void MergingQuantitiesIsCappedAtTheMaximumPerLine()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId, quantity: 95));

        basket.AddOrMergeItem(TestBasketItems.Create(productId, quantity: 10));

        basket.Items.Should().ContainSingle().Which.Quantity.Should().Be(ShoppingBasket.MaxQuantityPerLine);
    }

    [Fact]
    public void MergingAnExistingLineRefreshesItsUnitPriceSnapshot()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId, unitPrice: 2.50m));

        basket.AddOrMergeItem(TestBasketItems.Create(productId, unitPrice: 3.10m));

        basket.Items.Should().ContainSingle().Which.UnitPrice.Should().Be(3.10m);
    }

    [Fact]
    public void AddingANewLineAboveTheMaximumClampsItsQuantity()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());

        basket.AddOrMergeItem(TestBasketItems.Create(quantity: 150));

        basket.Items.Should().ContainSingle().Which.Quantity.Should().Be(ShoppingBasket.MaxQuantityPerLine);
    }

    [Fact]
    public void SettingQuantityToZeroRemovesTheLine()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId, quantity: 4));

        var lineWasFound = basket.SetItemQuantity(productId, 0);

        lineWasFound.Should().BeTrue();
        basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SettingQuantityAboveTheMaximumClampsToTheCap()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId));

        basket.SetItemQuantity(productId, 500);

        basket.Items.Should().ContainSingle().Which.Quantity.Should().Be(ShoppingBasket.MaxQuantityPerLine);
    }

    [Fact]
    public void SettingQuantityOnAMissingLineReportsNotFound()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());

        basket.SetItemQuantity(Guid.NewGuid(), 3).Should().BeFalse();
    }

    [Fact]
    public void SettingANegativeQuantityIsRejected()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());

        var settingNegativeQuantity = () => basket.SetItemQuantity(Guid.NewGuid(), -1);

        settingNegativeQuantity.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemovingAnExistingLineSucceeds()
    {
        var productId = Guid.NewGuid();
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(productId));

        basket.RemoveItem(productId).Should().BeTrue();
        basket.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void RemovingAMissingLineReportsNotFound()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());

        basket.RemoveItem(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void BasketContainsPhysicalItemsOnlyWhenAtLeastOneLineIsNotDigital()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(isDigital: true));

        basket.ContainsPhysicalItems.Should().BeFalse();

        basket.AddOrMergeItem(TestBasketItems.Create(isDigital: false));

        basket.ContainsPhysicalItems.Should().BeTrue();
    }

    [Fact]
    public void StoredSubtotalSumsUnitPriceTimesQuantityAcrossLines()
    {
        var basket = ShoppingBasket.CreateForCustomer(Guid.NewGuid());
        basket.AddOrMergeItem(TestBasketItems.Create(unitPrice: 2.50m, quantity: 2));
        basket.AddOrMergeItem(TestBasketItems.Create(unitPrice: 4.00m, quantity: 3));

        basket.StoredSubtotal.Should().Be(17.00m);
    }
}
