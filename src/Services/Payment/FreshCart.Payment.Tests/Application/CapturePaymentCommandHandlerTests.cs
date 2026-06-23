using FluentAssertions;
using FreshCart.Payment.Application.Abstractions;
using FreshCart.Payment.Application.Payments.Commands.CapturePayment;
using FreshCart.Payment.Application.Payments.Models;
using FreshCart.Payment.Domain;
using FreshCart.Payment.Domain.Events;
using FreshCart.Payment.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FreshCart.Payment.Tests.Application;

public sealed class CapturePaymentCommandHandlerTests
{
    private static readonly Guid OrderId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid CustomerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTimeOffset CaptureInstant = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private const decimal Amount = 49.99m;
    private const string CurrencyCode = "USD";
    private const string CardMethod = "card";
    private const string IdempotencyKey = "order-44444444-attempt-1";
    private const string ProviderReference = "SIM-TEST-REFERENCE";
    private const string IssuerDeclineReason = "The card was declined by the issuing bank.";
    private const string CaptureDeclineReason = "The authorization expired before capture.";

    private readonly IPaymentEventStore _paymentEventStore = Substitute.For<IPaymentEventStore>();
    private readonly IPaymentReadModelWriter _paymentReadModelWriter = Substitute.For<IPaymentReadModelWriter>();
    private readonly IPaymentReadQueries _paymentReadQueries = Substitute.For<IPaymentReadQueries>();
    private readonly IPaymentProvider _paymentProvider = Substitute.For<IPaymentProvider>();

    private readonly List<IReadOnlyList<IPaymentEvent>> _appendedBatches = [];
    private readonly List<PaymentReadModel> _projectedModels = [];

    private readonly CapturePaymentCommandHandler _commandHandler;

    public CapturePaymentCommandHandlerTests()
    {
        _paymentEventStore
            .AppendAsync(
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Do<IReadOnlyList<IPaymentEvent>>(_appendedBatches.Add),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _paymentReadModelWriter
            .UpsertAsync(Arg.Do<PaymentReadModel>(_projectedModels.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _paymentReadQueries
            .FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns((PaymentReadModel?)null);

        _commandHandler = new CapturePaymentCommandHandler(
            _paymentEventStore,
            _paymentReadModelWriter,
            _paymentReadQueries,
            _paymentProvider,
            new FixedTimeProvider(CaptureInstant),
            NullLogger<CapturePaymentCommandHandler>.Instance);
    }

    [Fact]
    public async Task ApprovedPaymentRunsInitiateAuthorizeCaptureAndReturnsCaptured()
    {
        ProviderAuthorizes();
        ProviderCaptures();

        var captureResult = await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        captureResult.OrderId.Should().Be(OrderId);
        captureResult.Status.Should().Be(PaymentStatus.Captured);
        captureResult.FailureReason.Should().BeNull();

        _appendedBatches.Should().HaveCount(3);
        _appendedBatches.SelectMany(batch => batch).Select(paymentEvent => paymentEvent.GetType())
            .Should().ContainInOrder(typeof(PaymentInitiated), typeof(PaymentAuthorized), typeof(PaymentCaptured));
        _appendedBatches.SelectMany(batch => batch).Select(paymentEvent => paymentEvent.Version)
            .Should().ContainInOrder(1, 2, 3);

        _projectedModels.Select(model => model.Status).Should().ContainInOrder(
            PaymentStatus.Initiated, PaymentStatus.Authorized, PaymentStatus.Captured);
        _projectedModels[^1].ProviderReference.Should().Be(ProviderReference);
        _projectedModels[^1].Amount.Should().Be(Amount);
    }

    [Fact]
    public async Task EveryAppendUsesTheVersionObservedBeforeTheNewEventsWereRaised()
    {
        ProviderAuthorizes();
        ProviderCaptures();
        var observedExpectedVersions = new List<int>();
        _paymentEventStore
            .AppendAsync(
                Arg.Any<Guid>(),
                Arg.Do<int>(observedExpectedVersions.Add),
                Arg.Any<IReadOnlyList<IPaymentEvent>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        observedExpectedVersions.Should().ContainInOrder(0, 1, 2);
    }

    [Fact]
    public async Task SecondCaptureForTheSameOrderReplaysTheRecordedOutcomeWithoutNewEvents()
    {
        var existingPaymentId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        _paymentReadQueries
            .FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(ExistingReadModel(existingPaymentId));
        _paymentEventStore
            .LoadStreamAsync(existingPaymentId, Arg.Any<CancellationToken>())
            .Returns(CapturedStream(existingPaymentId));

        var captureResult = await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        captureResult.PaymentId.Should().Be(existingPaymentId);
        captureResult.Status.Should().Be(PaymentStatus.Captured);
        captureResult.FailureReason.Should().BeNull();

        _appendedBatches.Should().BeEmpty();
        _projectedModels.Should().BeEmpty();
        await _paymentProvider.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
        await _paymentProvider.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task IdempotentReplayOfADeclinedPaymentPreservesTheOriginalFailureReason()
    {
        var existingPaymentId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        _paymentReadQueries
            .FindByOrderIdAsync(OrderId, Arg.Any<CancellationToken>())
            .Returns(ExistingReadModel(existingPaymentId));
        _paymentEventStore
            .LoadStreamAsync(existingPaymentId, Arg.Any<CancellationToken>())
            .Returns(new IPaymentEvent[]
            {
                InitiatedEvent(existingPaymentId),
                new PaymentDeclined(existingPaymentId, 2, CaptureInstant.AddSeconds(1), IssuerDeclineReason),
            });

        var captureResult = await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        captureResult.PaymentId.Should().Be(existingPaymentId);
        captureResult.Status.Should().Be(PaymentStatus.Declined);
        captureResult.FailureReason.Should().Be(IssuerDeclineReason);
        _appendedBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task DeclinedAuthorizationAppendsPaymentDeclinedAndReturnsTheReasonWithoutThrowing()
    {
        _paymentProvider
            .AuthorizeAsync(Arg.Any<ProviderAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ProviderAuthorizationResult.Declined(IssuerDeclineReason));

        var captureResult = await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        captureResult.Status.Should().Be(PaymentStatus.Declined);
        captureResult.FailureReason.Should().Be(IssuerDeclineReason);

        _appendedBatches.Should().HaveCount(2);
        _appendedBatches[1].Should().ContainSingle()
            .Which.Should().BeOfType<PaymentDeclined>()
            .Which.Reason.Should().Be(IssuerDeclineReason);

        _projectedModels[^1].Status.Should().Be(PaymentStatus.Declined);
        await _paymentProvider.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task DeclinedCaptureAfterSuccessfulAuthorizationEndsTheStreamDeclined()
    {
        ProviderAuthorizes();
        _paymentProvider
            .CaptureAsync(ProviderReference, Amount, CurrencyCode, Arg.Any<CancellationToken>())
            .Returns(ProviderCaptureResult.Declined(CaptureDeclineReason));

        var captureResult = await _commandHandler.Handle(CaptureCommand(), CancellationToken.None);

        captureResult.Status.Should().Be(PaymentStatus.Declined);
        captureResult.FailureReason.Should().Be(CaptureDeclineReason);

        _appendedBatches.SelectMany(batch => batch).Select(paymentEvent => paymentEvent.GetType())
            .Should().ContainInOrder(typeof(PaymentInitiated), typeof(PaymentAuthorized), typeof(PaymentDeclined));
        _projectedModels[^1].Status.Should().Be(PaymentStatus.Declined);
    }

    private static CapturePaymentCommand CaptureCommand() =>
        new(OrderId, CustomerId, Amount, CurrencyCode, CardMethod, IdempotencyKey);

    private static PaymentReadModel ExistingReadModel(Guid paymentId) => new(
        paymentId,
        OrderId,
        CustomerId,
        Amount,
        RefundedAmount: 0m,
        CurrencyCode,
        CardMethod,
        PaymentStatus.Captured,
        ProviderReference,
        CaptureInstant,
        CaptureInstant);

    private static PaymentInitiated InitiatedEvent(Guid paymentId) => new(
        paymentId, 1, CaptureInstant, OrderId, CustomerId, Amount, CurrencyCode, CardMethod);

    private static IReadOnlyList<IPaymentEvent> CapturedStream(Guid paymentId) =>
    [
        InitiatedEvent(paymentId),
        new PaymentAuthorized(paymentId, 2, CaptureInstant.AddSeconds(1), ProviderReference),
        new PaymentCaptured(paymentId, 3, CaptureInstant.AddSeconds(2)),
    ];

    private void ProviderAuthorizes() =>
        _paymentProvider
            .AuthorizeAsync(Arg.Any<ProviderAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ProviderAuthorizationResult.Approved(ProviderReference));

    private void ProviderCaptures() =>
        _paymentProvider
            .CaptureAsync(ProviderReference, Amount, CurrencyCode, Arg.Any<CancellationToken>())
            .Returns(ProviderCaptureResult.Approved());
}
