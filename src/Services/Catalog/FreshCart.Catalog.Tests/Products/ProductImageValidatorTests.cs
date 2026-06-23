using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Products;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Tests.Products;

public sealed class ProductImageValidatorTests
{
    private readonly ProductImageValidator validator = new();

    [Fact]
    public void AcceptsAnAbsoluteHttpsUrlWithAltText()
    {
        var image = new ProductImage("https://cdn.freshcart.test/box-art.png", "Box art", IsPrimary: true);

        validator.TestValidate(image).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://cdn.freshcart.test/box-art.png")]
    [InlineData("ftp://cdn.freshcart.test/box-art.png")]
    [InlineData("/relative/path.png")]
    public void RejectsUrlsThatAreNotAbsoluteHttps(string url)
    {
        var image = new ProductImage(url, "Box art", IsPrimary: false);

        validator.TestValidate(image).ShouldHaveValidationErrorFor(invalid => invalid.Url);
    }

    [Fact]
    public void RejectsUrlLongerThanTheLimit()
    {
        var oversizedUrl = "https://cdn.freshcart.test/" + new string('a', ProductConstraints.MaxImageUrlLength);
        var image = new ProductImage(oversizedUrl, "Box art", IsPrimary: false);

        validator.TestValidate(image).ShouldHaveValidationErrorFor(invalid => invalid.Url);
    }

    [Fact]
    public void RejectsMissingAltTextBecauseImagesMustStayAccessible()
    {
        var image = new ProductImage("https://cdn.freshcart.test/box-art.png", string.Empty, IsPrimary: false);

        validator.TestValidate(image).ShouldHaveValidationErrorFor(invalid => invalid.AltText);
    }

    [Fact]
    public void RejectsAltTextLongerThanTheLimit()
    {
        var image = new ProductImage(
            "https://cdn.freshcart.test/box-art.png",
            new string('a', ProductConstraints.MaxImageAltTextLength + 1),
            IsPrimary: false);

        validator.TestValidate(image).ShouldHaveValidationErrorFor(invalid => invalid.AltText);
    }
}
