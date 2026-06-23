using FluentValidation;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products;

public sealed class ProductImageValidator : AbstractValidator<ProductImage>
{
    public ProductImageValidator()
    {
        RuleFor(image => image.Url)
            .NotEmpty()
            .MaximumLength(ProductConstraints.MaxImageUrlLength)
            .Must(BeAnAbsoluteHttpsUrl)
            .WithMessage("Image Url must be an absolute https address.");

        RuleFor(image => image.AltText)
            .NotEmpty()
            .MaximumLength(ProductConstraints.MaxImageAltTextLength);
    }

    private static bool BeAnAbsoluteHttpsUrl(string url) =>
        string.IsNullOrEmpty(url)
        || (Uri.TryCreate(url, UriKind.Absolute, out var parsedUri)
            && string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
