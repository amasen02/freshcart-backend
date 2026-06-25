using FluentAssertions;
using FreshCart.Delivery.Domain.Slots;
using FreshCart.Delivery.Infrastructure.Persistence.Repositories;

namespace FreshCart.Delivery.Tests.Infrastructure;

[Collection(MongoIntegrationCollection.Name)]
public sealed class MongoSlotRepositoryTests(MongoIntegrationFixture fixture)
{
    private readonly MongoSlotRepository repository = new(fixture.Context);

    [Fact]
    public async Task ListsOnlyOpenSlotsForTheZoneOmittingFullOnes()
    {
        var zoneId = Guid.NewGuid();
        var dayStart = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

        var openSlot = DeliverySlot.Create(zoneId, dayStart, dayStart.AddHours(3), capacity: 5);
        var fullSlot = DeliverySlot.Create(zoneId, dayStart.AddHours(3), dayStart.AddHours(6), capacity: 1);
        fullSlot.Book();

        await repository.AddAsync(openSlot, CancellationToken.None);
        await repository.AddAsync(fullSlot, CancellationToken.None);

        var openSlots = await repository.ListOpenSlotsForZoneAsync(zoneId, CancellationToken.None);

        openSlots.Should().ContainSingle();
        openSlots[0].Id.Should().Be(openSlot.Id);
    }

    [Fact]
    public async Task ListsOpenSlotsOnTheRequestedDateOnly()
    {
        var zoneId = Guid.NewGuid();
        var targetDay = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var nextDay = targetDay.AddDays(1);

        var onTargetDay = DeliverySlot.Create(zoneId, targetDay, targetDay.AddHours(3), capacity: 4);
        var onNextDay = DeliverySlot.Create(zoneId, nextDay, nextDay.AddHours(3), capacity: 4);

        await repository.AddAsync(onTargetDay, CancellationToken.None);
        await repository.AddAsync(onNextDay, CancellationToken.None);

        var openSlots = await repository.ListOpenSlotsOnDateAsync(
            DateOnly.FromDateTime(targetDay.UtcDateTime),
            CancellationToken.None);

        openSlots.Should().Contain(slot => slot.Id == onTargetDay.Id);
        openSlots.Should().NotContain(slot => slot.Id == onNextDay.Id);
    }

    [Fact]
    public async Task BooksAFreeUnitAndPersistsTheCountSoTheInvariantSurvivesReload()
    {
        var zoneId = Guid.NewGuid();
        var slotStart = new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero);
        var slot = DeliverySlot.Create(zoneId, slotStart, slotStart.AddHours(3), capacity: 2);
        await repository.AddAsync(slot, CancellationToken.None);

        var booked = await repository.TryBookSlotAsync(slot, CancellationToken.None);

        booked.Should().BeTrue();
        var reloaded = await repository.ListOpenSlotsForZoneAsync(zoneId, CancellationToken.None);
        var persistedSlot = reloaded.Single(stored => stored.Id == slot.Id);
        persistedSlot.BookedCount.Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentBookingsNeverOversubscribeTheLastUnitOfCapacity()
    {
        const int ConcurrentSchedulers = 20;
        var zoneId = Guid.NewGuid();
        var slotStart = new DateTimeOffset(2026, 7, 5, 9, 0, 0, TimeSpan.Zero);
        var slot = DeliverySlot.Create(zoneId, slotStart, slotStart.AddHours(3), capacity: 1);
        await repository.AddAsync(slot, CancellationToken.None);

        var bookingAttempts = Enumerable
            .Range(0, ConcurrentSchedulers)
            .Select(_ => Task.Run(() => repository.TryBookSlotAsync(slot, CancellationToken.None)));
        var outcomes = await Task.WhenAll(bookingAttempts);

        outcomes.Count(succeeded => succeeded).Should().Be(1);
        var reloaded = await repository.ListOpenSlotsForZoneAsync(zoneId, CancellationToken.None);
        reloaded.Should().NotContain(stored => stored.Id == slot.Id);
    }

    [Fact]
    public async Task ReportsWhenSlotsExist()
    {
        var zoneId = Guid.NewGuid();
        var slotStart = new DateTimeOffset(2026, 7, 4, 9, 0, 0, TimeSpan.Zero);
        await repository.AddAsync(
            DeliverySlot.Create(zoneId, slotStart, slotStart.AddHours(3), capacity: 5),
            CancellationToken.None);

        var hasSlots = await repository.HasAnySlotsAsync(CancellationToken.None);

        hasSlots.Should().BeTrue();
    }
}
