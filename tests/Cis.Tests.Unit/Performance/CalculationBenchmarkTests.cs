using System.Diagnostics;
using Cis.Domain.ComplianceRisk;
using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.NAV;
using FluentAssertions;

namespace Cis.Tests.Unit.Performance;

public sealed class CalculationBenchmarkTests
{
    [Fact]
    public void NavFormulaEngine_BatchCalculation_CompletesWithinLocalBaseline()
    {
        var stopwatch = Stopwatch.StartNew();
        decimal aggregate = 0m;

        for (var index = 1; index <= 250_000; index++)
        {
            aggregate += NavFormulaEngine.GrossAssetValue(index, index / 2m, index / 4m, index / 5m, index / 10m);
            aggregate += NavFormulaEngine.NetAssetValue(index * 2m, index / 3m, index / 7m);
            aggregate += NavFormulaEngine.UnitPrice(index * 10m, index + 1m);
        }

        stopwatch.Stop();
        aggregate.Should().BeGreaterThan(0m);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FeesTaxDistributionFormulaEngine_BatchCalculation_CompletesWithinLocalBaseline()
    {
        var stopwatch = Stopwatch.StartNew();
        decimal aggregate = 0m;

        for (var index = 1; index <= 250_000; index++)
        {
            aggregate += FeesTaxDistributionFormulaEngine.DailyManagementFee(1_000_000m + index, 1.75m, FeeDayCountBasis.Actual365);
            aggregate += FeesTaxDistributionFormulaEngine.Vat(10_000m + index, 16m);
            aggregate += FeesTaxDistributionFormulaEngine.InvestorNetDistribution(1000m + index, 150m);
        }

        stopwatch.Stop();
        aggregate.Should().BeGreaterThan(0m);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ComplianceRiskFormulaEngine_BatchCalculation_CompletesWithinLocalBaseline()
    {
        var inputs = Enumerable.Range(1, 250_000)
            .Select(index => new WeightedMaturityInput(index, (index % 365) + 1))
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        var aum = ComplianceRiskFormulaEngine.AssetsUnderManagement(inputs.Select(input => input.InstrumentValue));
        var wam = ComplianceRiskFormulaEngine.WeightedAverageMaturity(inputs);
        var liquidityCoverage = ComplianceRiskFormulaEngine.LiquidityCoverageRatio(500_000_000m, 125_000_000m);
        stopwatch.Stop();

        aum.Should().BeGreaterThan(0m);
        wam.Should().BeGreaterThan(0m);
        liquidityCoverage.Should().BeGreaterThan(0m);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
