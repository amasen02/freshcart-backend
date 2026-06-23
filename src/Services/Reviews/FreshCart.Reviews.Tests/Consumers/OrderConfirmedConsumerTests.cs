using FluentAssertions;
using FreshCart.BuildingBlocks.Messaging.IntegrationEvents;
using FreshCart.Reviews.Api.Consumers;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;
using FreshCart.Reviews.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FreshCart.Reviews.Tests.Consumers;

public sealed class OrderConfirmedConsumerTests
{
    private static readonly DateTimeOffset KnownInstantUtc = new(2026, 6, 18, 11, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("0a000000-0000-0000-0000-000000000001");

    private readonly IPurchaseRecordRepository purchaseRecordRepository = Substitute.For<IPurchaseRecordRepository>();
    private readonly OrderConfirmedConsumer consumer;

    public OrderConfirmedConsumerTests()
    {
        consumer = new OrderConfirmedConsumer(
            purchaseRecordRepository,
            new FixedTimeProvider(KnownInstantUtc),
            NullLogger<OrderConfirmedConsumer>.Instance);
    }

    [Fact]
    public async Task RecordsOnePurchaseEntitlementPerLineSkuForTheConfirmedCustomerAndOrder()
    {
        purchaseRecordRepository
            .TryRecordAsync(Arg.Any<PurchaseRecord>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var recordedSkus = new List<string>();
        await purchaseRecordRepository.TryRecordAsync(
            Arg.Do<PurchaseRecord>(record => recordedSkus.Add(record.ProductSku)),
            Arg.Any<CancellationToken>());

        await consumer.Consume(ConsumeContextFactory.For(OrderConfirmedWithTwoLines(), CancellationToken.None));

        recordedSkus.Should().Equal("SKU-APPLES-1KG", "SKU-MILK-2L");
        await purchaseRecordRepository.Received(2).TryRecordAsync(
            Arg.Is<PurchaseRecord>(record =>
                record.CustomerId == CustomerId
                && record.OrderId == OrderId
                && record.PurchasedOnUtc == KnownInstantUtc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RedeliveryIsANoOpBecauseTheRepositorySwallowsTheDuplicateKeyWrite()
    {
        // The repository reports false on the second delivery (the unique index rejects the duplicate),
        // and the consumer neither throws nor takes any further action on that line.
        purchaseRecordRepository
            .TryRecordAsync(Arg.Any<PurchaseRecord>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var consuming = () => consumer.Consume(
            ConsumeContextFactory.For(OrderConfirmedWithTwoLines(), CancellationToken.None));

        await consuming.Should().NotThrowAsync();
        await purchaseRecordRepository.Received(2)
            .TryRecordAsync(Arg.Any<PurchaseRecord>(), Arg.Any<CancellationToken>());
    }

    private static OrderConfirmedIntegrationEvent OrderConfirmedWithTwoLines() => new()
    {
        OrderId = OrderId,
        CustomerId = CustomerId,
        GrandTotal = 12.80m,
        DiscountTotal = 0m,
        TaxTotal = 0.80m,
        ShippingTotal = 0m,
        CurrencyCode = "USD",
        PaymentMethod = "Card",
        Lines =
        [
            new OrderConfirmedLine("SKU-APPLES-1KG", "Royal Gala Apples 1kg", "Produce", 2, 4.50m),
            new OrderConfirmedLine("SKU-MILK-2L", "Full Cream Milk 2L", "Dairy", 1, 3.80m),
        ],
    };
}
