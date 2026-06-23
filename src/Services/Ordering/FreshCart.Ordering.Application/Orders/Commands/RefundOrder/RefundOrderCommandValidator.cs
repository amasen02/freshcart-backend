using FluentValidation;

namespace FreshCart.Ordering.Application.Orders.Commands.RefundOrder;

public sealed class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public const int ReasonMaxLength = 500;

    public RefundOrderCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty()
            .WithMessage("An order identifier is required.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .WithMessage("A refund reason is required.")
            .MaximumLength(ReasonMaxLength)
            .WithMessage($"A refund reason cannot exceed {ReasonMaxLength} characters.");
    }
}
