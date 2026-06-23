using FluentAssertions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Infrastructure.Providers;
using Xunit;

namespace FreshCart.Payment.Tests.Infrastructure;

public sealed class SimulatedCardPaymentProviderTests
{
    private static readonly Guid PaymentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const string CurrencyCode = "USD";
    private const string CardMethod = "card";

    private readonly SimulatedCardPaymentProvider _paymentProvider = new();

    [Theory]
    [InlineData("card-declined")]
    [InlineData("CARD-DECLINED")]
    public async Task DeclinedTestMethodIsDeclinedByTheIssuerRegardlessOfCasing(string declinedMethod)
    {
        var authorizationResult = await AuthorizeAsync(amount: 10.00m, method: declinedMethod);

        authorizationResult.IsApproved.Should().BeFalse();
        authorizationResult.DeclineReason.Should().Be(SimulatedCardPaymentProvider.IssuerDeclineReason);
        authorizationResult.ProviderReference.Should().BeNull();
    }

    [Fact]
    public async Task AmountAboveTheAuthorizationCeilingIsDeclinedAsOverLimit()
    {
        var authorizationResult = await AuthorizeAsync(
            SimulatedCardPaymentProvider.AuthorizationCeiling + 0.01m,
            CardMethod);

        authorizationResult.IsApproved.Should().BeFalse();
        authorizationResult.DeclineReason.Should().Be(SimulatedCardPaymentProvider.OverLimitDeclineReason);
    }

    [Fact]
    public async Task AmountExactlyAtTheAuthorizationCeilingIsApproved()
    {
        var authorizationResult = await AuthorizeAsync(
            SimulatedCardPaymentProvider.AuthorizationCeiling,
            CardMethod);

        authorizationResult.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task OrdinaryAuthorizationIsApprovedWithADeterministicProviderReference()
    {
        var authorizationResult = await AuthorizeAsync(amount: 49.99m, CardMethod);

        authorizationResult.IsApproved.Should().BeTrue();
        authorizationResult.DeclineReason.Should().BeNull();
        authorizationResult.ProviderReference.Should().Be($"SIM-{PaymentId:N}");
    }

    [Fact]
    public async Task CaptureOfAnAuthorizedPaymentIsAlwaysApproved()
    {
        var captureResult = await _paymentProvider.CaptureAsync(
            $"SIM-{PaymentId:N}", 49.99m, CurrencyCode, CancellationToken.None);

        captureResult.IsApproved.Should().BeTrue();
        captureResult.DeclineReason.Should().BeNull();
    }

    [Fact]
    public async Task RefundOfACapturedPaymentIsAlwaysApproved()
    {
        var refundResult = await _paymentProvider.RefundAsync(
            $"SIM-{PaymentId:N}", 20.00m, CurrencyCode, CancellationToken.None);

        refundResult.IsApproved.Should().BeTrue();
        refundResult.DeclineReason.Should().BeNull();
    }

    [Fact]
    public Task CaptureWithoutAProviderReferenceIsRejected()
    {
        var captureWithoutReference = () => _paymentProvider
            .CaptureAsync(" ", 10.00m, CurrencyCode, CancellationToken.None);

        return captureWithoutReference.Should().ThrowAsync<ArgumentException>();
    }

    private Task<ProviderAuthorizationResult> AuthorizeAsync(decimal amount, string method) =>
        _paymentProvider.AuthorizeAsync(
            new ProviderAuthorizationRequest(PaymentId, OrderId, amount, CurrencyCode, method),
            CancellationToken.None);
}
