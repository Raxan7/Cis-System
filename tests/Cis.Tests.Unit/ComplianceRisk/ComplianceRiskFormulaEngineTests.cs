using Cis.Domain.ComplianceRisk;
using FluentAssertions;

namespace Cis.Tests.Unit.ComplianceRisk;

public sealed class ComplianceRiskFormulaEngineTests
{
    [Fact]
    public void Formulas_DFml035_To_DFml042_ReturnExpectedDecimalResults()
    {
        var aum = ComplianceRiskFormulaEngine.AssetsUnderManagement([1_000m, 2_500m, 1_500m]);
        var netFlow = ComplianceRiskFormulaEngine.NetFlow(500m, 50m, 200m, 25m);
        var averageNav = ComplianceRiskFormulaEngine.AverageNav([1_000m, 1_100m, 1_200m]);
        var expenseToAumRatio = ComplianceRiskFormulaEngine.ExpenseToAumRatio(22m, averageNav);
        var liquidityCoverageRatio = ComplianceRiskFormulaEngine.LiquidityCoverageRatio(300m, 200m);
        var stressCoverage = ComplianceRiskFormulaEngine.StressCoverage(300m, 600m);
        var wam = ComplianceRiskFormulaEngine.WeightedAverageMaturity([
            new WeightedMaturityInput(600m, 30),
            new WeightedMaturityInput(400m, 90)
        ]);
        var annualizedYield = ComplianceRiskFormulaEngine.AnnualizedYield(25m, averageNav, 12m);

        aum.Should().Be(5_000m);
        netFlow.Should().Be(325m);
        averageNav.Should().Be(1_100m);
        expenseToAumRatio.Should().Be(0.02m);
        liquidityCoverageRatio.Should().Be(1.5m);
        stressCoverage.Should().Be(0.5m);
        wam.Should().Be(54m);
        annualizedYield.Should().Be(0.272727m);
    }

    [Fact]
    public void AverageNav_WithoutValuationDays_Throws()
    {
        var act = () => ComplianceRiskFormulaEngine.AverageNav([]);

        act.Should().Throw<ArgumentException>().WithMessage("*daily NAV*");
    }
}
