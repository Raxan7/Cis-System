using Cis.Domain.FeesTaxDistribution;
using FluentAssertions;

namespace Cis.Tests.Unit.FeesTaxDistribution;

public sealed class FeesTaxDistributionFormulaEngineTests
{
    [Fact]
    public void Formulas_DFml014_To_DFml034_ReturnExpectedDecimalResults()
    {
        var dailyManagementFee = FeesTaxDistributionFormulaEngine.DailyManagementFee(100_000m, 2m, FeeDayCountBasis.Actual365);
        var periodManagementFee = FeesTaxDistributionFormulaEngine.PeriodManagementFeeFromAverageNav(100_000m, 2m, 30, FeeDayCountBasis.Actual365);
        var dailyCustodyFee = FeesTaxDistributionFormulaEngine.DailyCustodyFee(100_000m, 0.5m, FeeDayCountBasis.Actual365);
        var dailyTrusteeFee = FeesTaxDistributionFormulaEngine.DailyTrusteeFee(100_000m, 0.25m, FeeDayCountBasis.Actual365);
        var adminFee = FeesTaxDistributionFormulaEngine.AdminFee(100_000m, 1m, 30, FeeDayCountBasis.Actual365, null);
        var entryFee = FeesTaxDistributionFormulaEngine.EntryFee(1_000m, 1m, null);
        var exitFee = FeesTaxDistributionFormulaEngine.ExitFee(900m, 2m, null);
        var switchFee = FeesTaxDistributionFormulaEngine.SwitchFee(500m, 0.5m, null);
        var performanceFee = FeesTaxDistributionFormulaEngine.PerformanceFee(1_200m, 1_000m, 900m, 20m);
        var vat = FeesTaxDistributionFormulaEngine.Vat(100m, 16m);
        var withholdingTax = FeesTaxDistributionFormulaEngine.WithholdingTax(100m, 5m);
        var netPayable = FeesTaxDistributionFormulaEngine.NetPayable(100m, vat, withholdingTax);
        var totalExpenseRatio = FeesTaxDistributionFormulaEngine.TotalExpenseRatio(2_500m, 100_000m);
        var grossDistributableIncome = FeesTaxDistributionFormulaEngine.GrossDistributableIncome(1_000m, 200m, -50m, 25m);
        var netDistributableIncome = FeesTaxDistributionFormulaEngine.NetDistributableIncome(grossDistributableIncome, 100m, 50m, 25m, 75m);
        var distributionPerUnit = FeesTaxDistributionFormulaEngine.DistributionPerUnit(netDistributableIncome, 1_000m);
        var investorGrossDistribution = FeesTaxDistributionFormulaEngine.InvestorGrossDistribution(100m, distributionPerUnit);
        var investorNetDistribution = FeesTaxDistributionFormulaEngine.InvestorNetDistribution(investorGrossDistribution, 10m);
        var reinvestmentUnits = FeesTaxDistributionFormulaEngine.ReinvestmentUnits(investorNetDistribution, 1.25m);
        var distributionCoverage = FeesTaxDistributionFormulaEngine.DistributionCoverage(1_000m, 800m);
        var investorReturnForPeriod = FeesTaxDistributionFormulaEngine.InvestorReturnForPeriod(1_200m, 1_000m, 50m, 100m);

        dailyManagementFee.Should().Be(5.479452m);
        periodManagementFee.Should().Be(164.383562m);
        dailyCustodyFee.Should().Be(1.369863m);
        dailyTrusteeFee.Should().Be(0.684932m);
        adminFee.Should().Be(82.191781m);
        entryFee.Should().Be(10m);
        exitFee.Should().Be(18m);
        switchFee.Should().Be(2.5m);
        performanceFee.Should().Be(40m);
        vat.Should().Be(16m);
        withholdingTax.Should().Be(5m);
        netPayable.Should().Be(111m);
        totalExpenseRatio.Should().Be(0.025m);
        grossDistributableIncome.Should().Be(1_175m);
        netDistributableIncome.Should().Be(925m);
        distributionPerUnit.Should().Be(0.925m);
        investorGrossDistribution.Should().Be(92.5m);
        investorNetDistribution.Should().Be(82.5m);
        reinvestmentUnits.Should().Be(66m);
        distributionCoverage.Should().Be(1.25m);
        investorReturnForPeriod.Should().Be(0.15m);
    }

    [Theory]
    [InlineData(FeeDayCountBasis.Actual365, 365)]
    [InlineData(FeeDayCountBasis.Actual366, 366)]
    [InlineData(FeeDayCountBasis.Actual360, 360)]
    public void DayCountBasis_UsesConfiguredDenominator(FeeDayCountBasis basis, int denominator)
    {
        var result = FeesTaxDistributionFormulaEngine.DailyManagementFee(10_000m, 10m, basis);

        result.Should().Be(FeesTaxDistributionFormulaEngine.Round(10_000m * 0.10m / denominator));
    }
}
