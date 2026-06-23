using FluentAssertions;
using FreshCart.Reporting.Domain.Invoices;

namespace FreshCart.Reporting.Tests.Domain;

public sealed class InvoiceNumberTests
{
    [Theory]
    [InlineData(InvoiceKind.Sale,        2026, 1L,      "INV-2026-000001")]
    [InlineData(InvoiceKind.CreditNote,  2026, 18L,     "CR-2026-000018")]
    [InlineData(InvoiceKind.ProForma,    2026, 999_999L, "PF-2026-999999")]
    public void AllocateProducesTheExpectedFormattedValue(InvoiceKind kind, int year, long sequence, string expected)
    {
        InvoiceNumber.Allocate(kind, year, sequence).Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("INV-2026-000123", InvoiceKind.Sale,        2026, 123L)]
    [InlineData("CR-2026-000018",  InvoiceKind.CreditNote,  2026, 18L)]
    [InlineData("PF-2026-000009",  InvoiceKind.ProForma,    2026, 9L)]
    public void TryParseRecognisesEveryKindPrefix(string raw, InvoiceKind expectedKind, int expectedYear, long expectedSequence)
    {
        InvoiceNumber.TryParse(raw, out var parsed).Should().BeTrue();
        parsed.Kind.Should().Be(expectedKind);
        parsed.Year.Should().Be(expectedYear);
        parsed.Sequence.Should().Be(expectedSequence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nothing-like-an-invoice")]
    [InlineData("INV-2026")]
    [InlineData("UNKNOWN-2026-000001")]
    [InlineData("INV-abcd-000001")]
    [InlineData("INV-2026-xx")]
    public void TryParseReturnsFalseForMalformedInput(string raw)
    {
        InvoiceNumber.TryParse(raw, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(1999, 1L)]
    [InlineData(10_000, 1L)]
    public void AllocateRejectsAYearOutsideTheFourDigitRange(int year, long sequence)
    {
        Action act = () => InvoiceNumber.Allocate(InvoiceKind.Sale, year, sequence);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AllocateRejectsASequenceLessThanOne()
    {
        Action act = () => InvoiceNumber.Allocate(InvoiceKind.Sale, 2026, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
