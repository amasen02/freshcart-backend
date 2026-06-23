using FluentValidation;

namespace FreshCart.Ordering.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public const int ReasonMaxLength = 500;

    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty()
            .WithMessage("An order identifier is required.");

        RuleFor(command => command.RequestingCustomerId)
            .NotEmpty()
            .WithMessage("The requesting customer identifier is required.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage("A cancellation reason is required.")
            .MaximumLength(ReasonMaxLength)
            .WithMessage($"A cancellation reason cannot exceed {ReasonMaxLength} characters.");
    }
}
