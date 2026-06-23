using FluentValidation.TestHelper;
using FreshCart.Catalog.Api.Features.Brands;
using FreshCart.Catalog.Api.Features.Brands.CreateBrand;

namespace FreshCart.Catalog.Tests.Brands;

public sealed class CreateBrandCommandValidatorTests
{
    private readonly CreateBrandCommandValidator validator = new();

    [Fact]
    public void AcceptsABrandWithAnHttpsLogo()
    {
        validator.TestValidate(new CreateBrandCommand("PixelForge Studios", "https://cdn.freshcart.test/logo.png"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AcceptsABrandWithoutALogo()
    {
        validator.TestValidate(new CreateBrandCommand("PixelForge Studios", null))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsMissingName(string name)
    {
        validator.TestValidate(new CreateBrandCommand(name, null))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Fact]
    public void RejectsNameLongerThanTheLimit()
    {
        validator.TestValidate(new CreateBrandCommand(new string('a', BrandConstraints.MaxNameLength + 1), null))
            .ShouldHaveValidationErrorFor(invalid => invalid.Name);
    }

    [Theory]
    [InlineData("http://cdn.freshcart.test/logo.png")]
    [InlineData("not-a-url")]
    public void RejectsLogoUrlsThatAreNotAbsoluteHttps(string logoUrl)
    {
        validator.TestValidate(new CreateBrandCommand("PixelForge Studios", logoUrl))
            .ShouldHaveValidationErrorFor(invalid => invalid.LogoUrl);
    }

    [Fact]
    public void RejectsLogoUrlLongerThanTheLimit()
    {
        var oversizedLogoUrl = "https://cdn.freshcart.test/" + new string('a', BrandConstraints.MaxLogoUrlLength);

        validator.TestValidate(new CreateBrandCommand("PixelForge Studios", oversizedLogoUrl))
            .ShouldHaveValidationErrorFor(invalid => invalid.LogoUrl);
    }
}
