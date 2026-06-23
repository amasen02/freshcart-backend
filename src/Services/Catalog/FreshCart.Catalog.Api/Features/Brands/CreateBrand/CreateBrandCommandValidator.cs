using FluentValidation;

namespace FreshCart.Catalog.Api.Features.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(BrandConstraints.MaxNameLength);

        RuleFor(command => command.LogoUrl)
            .MaximumLength(BrandConstraints.MaxLogoUrlLength)
            .Must(BeAnAbsoluteHttpsUrlWhenProvided)
            .WithMessage("LogoUrl must be an absolute https address.");
    }

    private static bool BeAnAbsoluteHttpsUrlWhenProvided(string? logoUrl) =>
        string.IsNullOrEmpty(logoUrl)
        || (Uri.TryCreate(logoUrl, UriKind.Absolute, out var parsedUri)
            && string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
