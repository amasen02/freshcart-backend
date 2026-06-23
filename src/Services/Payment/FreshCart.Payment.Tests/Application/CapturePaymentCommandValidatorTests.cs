using FluentValidation.TestHelper;
using FreshCart.Payment.Application.Payments.Commands.CapturePayment;
using Xunit;

namespace FreshCart.Payment.Tests.Application;

public sealed class CapturePaymentCommandValidatorTests
{
    private readonly CapturePaymentCommandValidator _validator = new();

    [Fact]
    public void WellFormedCommandPassesEveryRule()
    {
        var validationResult = _validator.TestValidate(ValidCommand());

        validationResult.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyOrderIdIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { OrderId = Guid.Empty });

        validationResult.ShouldHaveValidationErrorFor(command => command.OrderId);
    }

    [Fact]
    public void EmptyCustomerIdIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { CustomerId = Guid.Empty });

        validationResult.ShouldHaveValidationErrorFor(command => command.CustomerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void NonPositiveAmountIsRejected(decimal invalidAmount)
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { Amount = invalidAmount });

        validationResult.ShouldHaveValidationErrorFor(command => command.Amount);
    }

    [Fact]
    public void AmountWithMoreThanTwoDecimalPlacesIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { Amount = 10.999m });

        validationResult.ShouldHaveValidationErrorFor(command => command.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("U5D")]
    public void CurrencyCodeMustBeThreeUppercaseLetters(string invalidCurrencyCode)
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { CurrencyCode = invalidCurrencyCode });

        validationResult.ShouldHaveValidationErrorFor(command => command.CurrencyCode);
    }

    [Fact]
    public void EmptyMethodIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { Method = string.Empty });

        validationResult.ShouldHaveValidationErrorFor(command => command.Method);
    }

    [Fact]
    public void OverlongMethodIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { Method = new string('m', 51) });

        validationResult.ShouldHaveValidationErrorFor(command => command.Method);
    }

    [Fact]
    public void EmptyIdempotencyKeyIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { IdempotencyKey = string.Empty });

        validationResult.ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
    }

    [Fact]
    public void OverlongIdempotencyKeyIsRejected()
    {
        var validationResult = _validator.TestValidate(ValidCommand() with { IdempotencyKey = new string('k', 101) });

        validationResult.ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
    }

    private static CapturePaymentCommand ValidCommand() => new(
        OrderId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
        CustomerId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
        Amount: 49.99m,
        CurrencyCode: "USD",
        Method: "card",
        IdempotencyKey: "order-44444444-attempt-1");
}
