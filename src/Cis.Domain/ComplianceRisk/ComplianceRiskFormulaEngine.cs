namespace Cis.Domain.ComplianceRisk;

public static class ComplianceRiskFormulaEngine
{
    public const int InternalPrecision = 6;

    public static decimal AssetsUnderManagement(IEnumerable<decimal> closingNavs)
    {
        return Round(closingNavs.Sum());
    }

    public static decimal NetFlow(decimal totalSubscriptions, decimal transferIn, decimal totalRedemptions, decimal transferOut)
    {
        return Round(totalSubscriptions + transferIn - totalRedemptions - transferOut);
    }

    public static decimal AverageNav(IEnumerable<decimal> dailyNavs)
    {
        var values = dailyNavs.ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one daily NAV is required.", nameof(dailyNavs));
        }

        return Round(values.Sum() / values.Length);
    }

    public static decimal ExpenseToAumRatio(decimal totalExpensesForPeriod, decimal averageNav)
    {
        ComplianceRiskValidation.Positive(averageNav, nameof(averageNav));
        return Round(totalExpensesForPeriod / averageNav);
    }

    public static decimal LiquidityCoverageRatio(decimal availableLiquidAssets, decimal projectedShortTermRedemptions)
    {
        ComplianceRiskValidation.Positive(projectedShortTermRedemptions, nameof(projectedShortTermRedemptions));
        return Round(availableLiquidAssets / projectedShortTermRedemptions);
    }

    public static decimal StressCoverage(decimal availableLiquidAssets, decimal stressRedemptionScenarioAmount)
    {
        ComplianceRiskValidation.Positive(stressRedemptionScenarioAmount, nameof(stressRedemptionScenarioAmount));
        return Round(availableLiquidAssets / stressRedemptionScenarioAmount);
    }

    public static decimal WeightedAverageMaturity(IEnumerable<WeightedMaturityInput> inputs)
    {
        var values = inputs.ToArray();
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one instrument maturity input is required.", nameof(inputs));
        }

        var totalValue = values.Sum(input => input.InstrumentValue);
        ComplianceRiskValidation.Positive(totalValue, nameof(inputs));
        return Round(values.Sum(input => (input.InstrumentValue / totalValue) * input.DaysToMaturityOrReset));
    }

    public static decimal AnnualizedYield(decimal periodNetInvestmentIncome, decimal averageNav, decimal annualizationFactor)
    {
        ComplianceRiskValidation.Positive(averageNav, nameof(averageNav));
        return Round(periodNetInvestmentIncome / averageNav * annualizationFactor);
    }

    public static decimal Round(decimal value, int precision = InternalPrecision)
    {
        return decimal.Round(value, precision, MidpointRounding.AwayFromZero);
    }
}

public sealed record WeightedMaturityInput(decimal InstrumentValue, int DaysToMaturityOrReset);
