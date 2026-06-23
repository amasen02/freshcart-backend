using FluentAssertions;
using FreshCart.Ordering.Application.Orders.Commands.RefundOrder;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class RefundOrderCommandValidatorTests
{
    private readonly RefundOrderCommandValidator validator = new();

    [Fact]
    public void AcceptsACommandWithAnOrderAndReason()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), "Damaged on delivery");

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyOrderId()
    {
        var command = new RefundOrderCommand(Guid.Empty, "Damaged on delivery");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsABlankReason()
    {
        var command = new RefundOrderCommand(Guid.NewGuid(), "");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAReasonLongerThanTheMaximum()
    {
        var reason = new string('x', RefundOrderCommandValidator.ReasonMaxLength + 1);
        var command = new RefundOrderCommand(Guid.NewGuid(), reason);

        validator.Validate(command).IsValid.Should().BeFalse();
    }
}
