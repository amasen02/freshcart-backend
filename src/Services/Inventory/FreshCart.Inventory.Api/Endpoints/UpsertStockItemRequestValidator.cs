using FluentValidation;

namespace FreshCart.Inventory.Api.Endpoints;

public sealed class UpsertStockItemRequestValidator : AbstractValidator<UpsertStockItemRequest>
{
    private const int MaxProductNameLength = 200;

    public UpsertStockItemRequestValidator()
    {
        RuleFor(request => request.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(MaxProductNameLength)
            .WithMessage($"Product name cannot exceed {MaxProductNameLength} characters.");

        RuleFor(request => request.QuantityOnHand)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity on hand cannot be negative.");
    }
}
