using FluentAssertions;
using FreshCart.Ordering.Application.Orders.Commands.CancelOrder;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator validator = new();

    [Fact]
    public void AcceptsACommandWithAnOrderCustomerAndReason()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid(), "Changed my mind");

        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyOrderId()
    {
        var command = new CancelOrderCommand(Guid.Empty, Guid.NewGuid(), "Changed my mind");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAnEmptyRequestingCustomerId()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.Empty, "Changed my mind");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsABlankReason()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid(), "   ");

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectsAReasonLongerThanTheMaximum()
    {
        var reason = new string('x', CancelOrderCommandValidator.ReasonMaxLength + 1);
        var command = new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid(), reason);

        validator.Validate(command).IsValid.Should().BeFalse();
    }
}
