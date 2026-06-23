using System.Text.RegularExpressions;
using FluentValidation;
using FreshCart.Catalog.Api.Models;

namespace FreshCart.Catalog.Api.Features.Products.UpdateProduct;

public sealed partial class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private const int RegexTimeoutMilliseconds = 100;

    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(ProductConstraints.MaxNameLength);

        RuleFor(command => command.Description)
            .MaximumLength(ProductConstraints.MaxDescriptionLength);

        RuleFor(command => command.BasePrice)
            .GreaterThan(0)
            .LessThanOrEqualTo(ProductConstraints.MaximumBasePrice);

        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Must(currencyCode => string.IsNullOrEmpty(currencyCode) || CurrencyCodeRegex().IsMatch(currencyCode))
            .WithMessage("CurrencyCode must be a three-letter uppercase ISO 4217 code.");

        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.BrandId).NotEmpty();

        RuleFor(command => command.Images)
            .Must(images => images is null || images.Count <= ProductConstraints.MaxImageCount)
            .WithMessage($"A product can hold at most {ProductConstraints.MaxImageCount} images.")
            .Must(images => images is null || images.Count(image => image.IsPrimary) <= 1)
            .WithMessage("At most one image can be marked as primary.");

        RuleForEach(command => command.Images).SetValidator(new ProductImageValidator());

        RuleFor(command => command.Attributes)
            .Must(attributes => attributes is null || attributes.Count <= ProductConstraints.MaxAttributeCount)
            .WithMessage($"A product can hold at most {ProductConstraints.MaxAttributeCount} attributes.");

        RuleForEach(command => command.Attributes).SetValidator(new ProductAttributeValidator());
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.None, RegexTimeoutMilliseconds)]
    private static partial Regex CurrencyCodeRegex();
}
