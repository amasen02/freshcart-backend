using FluentAssertions;
using FreshCart.Reporting.Domain.Kpis;

namespace FreshCart.Reporting.Tests.Domain;

public sealed class KpiMetricTests
{
    [Theory]
    [InlineData(100, 80,  25)]
    [InlineData(80,  100, -20)]
    [InlineData(100, 100, 0)]
    public void DeltaPercentageReflectsTheDirectionalChange(decimal current, decimal previous, decimal expectedPercentage)
    {
        var metric = new KpiMetric("kpi.test", "Test", current, previous, KpiUnit.Currency);

        metric.DeltaPercentage.Should().Be(expectedPercentage);
    }

    [Fact]
    public void DeltaPercentageIsNullWhenPreviousIsNull()
    {
        new KpiMetric("kpi.test", "Test", 100m, null, KpiUnit.Count)
            .DeltaPercentage.Should().BeNull();
    }

    [Fact]
    public void DeltaPercentageIsNullWhenPreviousIsZeroToAvoidDivisionByZero()
    {
        new KpiMetric("kpi.test", "Test", 100m, 0m, KpiUnit.Count)
            .DeltaPercentage.Should().BeNull();
    }

    [Theory]
    [InlineData(100, 80,  KpiTrend.Up)]
    [InlineData(80,  100, KpiTrend.Down)]
    [InlineData(100, 100, KpiTrend.Flat)]
    public void TrendMatchesTheDirectionalChange(decimal current, decimal previous, KpiTrend expected)
    {
        new KpiMetric("kpi.test", "Test", current, previous, KpiUnit.Count)
            .Trend.Should().Be(expected);
    }

    [Fact]
    public void TrendIsFlatWhenPreviousIsNull()
    {
        new KpiMetric("kpi.test", "Test", 100m, null, KpiUnit.Count)
            .Trend.Should().Be(KpiTrend.Flat);
    }
}
