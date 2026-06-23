using System.Globalization;
using FluentAssertions;
using FreshCart.Reviews.Api.Domain;
using FreshCart.Reviews.Api.Persistence;
using FreshCart.Reviews.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace FreshCart.Reviews.Tests.Persistence;

[Collection(MongoFixture.CollectionName)]
public sealed class MongoPurchaseRecordRepositoryTests : IDisposable
{
    private const string ProductSku = "FC-PRD-0001";
    private static readonly DateTimeOffset PurchasedOnUtc = new(2026, 6, 18, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("0a000000-0000-0000-0000-000000000001");

    private readonly MongoClient _mongoClient;
    private readonly ReviewsMongoContext _context;
    private readonly MongoPurchaseRecordRepository _repository;

    public MongoPurchaseRecordRepositoryTests(MongoFixture mongoFixture)
    {
        ArgumentNullException.ThrowIfNull(mongoFixture);

        _mongoClient = new MongoClient(mongoFixture.ConnectionString);

        var isolatedDatabaseName = $"reviews_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";
        _context = new ReviewsMongoContext(_mongoClient.GetDatabase(isolatedDatabaseName));
        _repository = new MongoPurchaseRecordRepository(_context);
    }

    public void Dispose() => _mongoClient.Dispose();

    [Fact]
    public async Task RecordingTheSameEntitlementTwiceWritesOnceAndSwallowsTheDuplicate()
    {
        await EnsureIndexesAsync();

        var firstWrite = await _repository.TryRecordAsync(Entitlement(), CancellationToken.None);
        var secondWrite = await _repository.TryRecordAsync(Entitlement(), CancellationToken.None);

        firstWrite.Should().BeTrue("the entitlement did not exist yet");
        secondWrite.Should().BeFalse("the unique index rejects the redelivered confirmation");

        var storedCount = await _context.Purchases.CountDocumentsAsync(
            FilterDefinition<PurchaseRecord>.Empty, options: null, CancellationToken.None);
        storedCount.Should().Be(1);
    }

    [Fact]
    public async Task HasPurchasedReflectsAnEntitlementRegardlessOfWhichOrderRecordedIt()
    {
        await EnsureIndexesAsync();
        await _repository.TryRecordAsync(Entitlement(), CancellationToken.None);

        (await _repository.HasPurchasedAsync(CustomerId, ProductSku, CancellationToken.None)).Should().BeTrue();
        (await _repository.HasPurchasedAsync(CustomerId, "FC-PRD-9999", CancellationToken.None)).Should().BeFalse();
        (await _repository.HasPurchasedAsync(Guid.CreateVersion7(), ProductSku, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ASecondOrderForTheSameProductIsADistinctEntitlementRow()
    {
        await EnsureIndexesAsync();

        await _repository.TryRecordAsync(Entitlement(), CancellationToken.None);
        var secondOrderWrite = await _repository.TryRecordAsync(
            PurchaseRecord.Record(Guid.CreateVersion7(), CustomerId, ProductSku, Guid.CreateVersion7(), PurchasedOnUtc),
            CancellationToken.None);

        secondOrderWrite.Should().BeTrue("a different order is a different entitlement key");
        var storedCount = await _context.Purchases.CountDocumentsAsync(
            FilterDefinition<PurchaseRecord>.Empty, options: null, CancellationToken.None);
        storedCount.Should().Be(2);
    }

    private Task EnsureIndexesAsync() =>
        new ReviewsPersistenceInitializer(_context, NullLogger<ReviewsPersistenceInitializer>.Instance)
            .StartAsync(CancellationToken.None);

    private static PurchaseRecord Entitlement() =>
        PurchaseRecord.Record(Guid.CreateVersion7(), CustomerId, ProductSku, OrderId, PurchasedOnUtc);
}
