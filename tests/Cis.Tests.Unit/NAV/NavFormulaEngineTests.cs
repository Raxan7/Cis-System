using Cis.Domain.NAV;
using FluentAssertions;

namespace Cis.Tests.Unit.NAV;

public sealed class NavFormulaEngineTests
{
    [Fact]
    public void Formulas_DFml001_To_DFml013_ReturnExpectedDecimalResults()
    {
        var accruedIncome = NavFormulaEngine.AccruedIncome(1_000m, 12m, 30, DayCountBasis.Actual365);
        var investmentValue = NavFormulaEngine.InvestmentValue(100m, 10m, 5m, false, null);
        var amortizedInvestmentValue = NavFormulaEngine.InvestmentValue(100m, 0m, 0m, true, 1_010m);
        var dailyAccretion = NavFormulaEngine.DailyAccretion(1_100m, 1_000m, 100);
        var grossAssetValue = NavFormulaEngine.GrossAssetValue(investmentValue, 200m, 9m, 10m, 5m);
        var nav = NavFormulaEngine.NetAssetValue(grossAssetValue, 100m, 20m);
        var closingUnits = NavFormulaEngine.ClosingUnits(1_000m, 50m, 20m, 5m);
        var unitPrice = NavFormulaEngine.UnitPrice(nav, closingUnits);
        var netSubscriptionAmount = NavFormulaEngine.NetSubscriptionAmount(1_000m, 10m, 5m);
        var unitsAllocated = NavFormulaEngine.UnitsAllocated(netSubscriptionAmount, 1.25m);
        var grossRedemptionValue = NavFormulaEngine.GrossRedemptionValue(20m, 1.3m);
        var netRedemptionPayable = NavFormulaEngine.NetRedemptionPayable(grossRedemptionValue, 1m, 0.5m);
        var switchOutValue = NavFormulaEngine.SwitchOutValue(10m, 1.2m);
        var switchInUnits = NavFormulaEngine.SwitchInUnits(11m, 1.1m);
        var redeemableAmount = NavFormulaEngine.RedeemableAmount(100m, 1.5m, 2m, 1m);

        accruedIncome.Should().Be(9.863014m);
        investmentValue.Should().Be(1_005m);
        amortizedInvestmentValue.Should().Be(1_010m);
        dailyAccretion.Should().Be(1m);
        grossAssetValue.Should().Be(1_229m);
        nav.Should().Be(1_109m);
        closingUnits.Should().Be(1_035m);
        unitPrice.Should().Be(1.071498m);
        netSubscriptionAmount.Should().Be(985m);
        unitsAllocated.Should().Be(788m);
        grossRedemptionValue.Should().Be(26m);
        netRedemptionPayable.Should().Be(24.5m);
        switchOutValue.Should().Be(12m);
        switchInUnits.Should().Be(10m);
        redeemableAmount.Should().Be(147m);
    }

    [Theory]
    [InlineData(DayCountBasis.Actual365, 365)]
    [InlineData(DayCountBasis.Actual366, 366)]
    [InlineData(DayCountBasis.Actual360, 360)]
    public void AccruedIncome_SupportsConfiguredDayCountBasis(DayCountBasis basis, int denominator)
    {
        var result = NavFormulaEngine.AccruedIncome(10_000m, 10m, 30, basis);

        result.Should().Be(NavFormulaEngine.Round(10_000m * 0.10m * 30 / denominator));
    }

    [Fact]
    public void UnitPrice_WithZeroUnits_ThrowsValidationException()
    {
        var act = () => NavFormulaEngine.UnitPrice(1_000m, 0m);

        act.Should().Throw<ArgumentException>().WithMessage("*unitsInIssue*");
    }
}
