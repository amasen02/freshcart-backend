using FluentAssertions;
using FreshCart.Reporting.Domain.Invoices;
using FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

namespace FreshCart.Reporting.Tests.Persistence;

[Collection(WarehouseIntegrationCollection.Name)]
public sealed class InvoiceRepositoryTests(WarehouseIntegrationFixture fixture)
{
    [Fact]
    public async Task AllocatesNumbersSequentiallyFromOnePerYearAndKind()
    {
        const int Year = 2030;
        var repository = new InvoiceRepository(WarehouseIntegrationFixture.CreateWarehouseDbContext(), fixture.ConnectionFactory);

        var first = await repository.AllocateNextNumberAsync(InvoiceKind.Sale, Year, CancellationToken.None);
        var second = await repository.AllocateNextNumberAsync(InvoiceKind.Sale, Year, CancellationToken.None);

        first.Sequence.Should().Be(1);
        second.Sequence.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentAllocationsHandOutDistinctGapFreeNumbers()
    {
        const int ConcurrentAllocators = 25;
        const int Year = 2031;
        var repository = new InvoiceRepository(WarehouseIntegrationFixture.CreateWarehouseDbContext(), fixture.ConnectionFactory);

        var allocationTasks = Enumerable
            .Range(0, ConcurrentAllocators)
            .Select(_ => Task.Run(() => repository.AllocateNextNumberAsync(InvoiceKind.Sale, Year, CancellationToken.None)));
        var allocatedNumbers = await Task.WhenAll(allocationTasks);

        var sequences = allocatedNumbers.Select(number => number.Sequence).ToArray();
        sequences.Should().OnlyHaveUniqueItems();
        sequences.Should().BeEquivalentTo(Enumerable.Range(1, ConcurrentAllocators).Select(value => (long)value));
    }

    [Fact]
    public async Task DifferentKindsAndYearsKeepIndependentSequences()
    {
        const int Year = 2032;
        var repository = new InvoiceRepository(WarehouseIntegrationFixture.CreateWarehouseDbContext(), fixture.ConnectionFactory);

        var sale = await repository.AllocateNextNumberAsync(InvoiceKind.Sale, Year, CancellationToken.None);
        var creditNote = await repository.AllocateNextNumberAsync(InvoiceKind.CreditNote, Year, CancellationToken.None);

        sale.Sequence.Should().Be(1);
        creditNote.Sequence.Should().Be(1);
    }
}
