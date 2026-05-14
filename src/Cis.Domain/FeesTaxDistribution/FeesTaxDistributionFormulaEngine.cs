namespace Cis.Domain.FeesTaxDistribution;

public static class FeesTaxDistributionFormulaEngine
{
    public const int InternalPrecision = 6;

    public static decimal DailyManagementFee(decimal applicableFeeBase, decimal annualManagementFeeRate, FeeDayCountBasis dayCountBasis)
    {
        return Round(applicableFeeBase * (annualManagementFeeRate / 100m) / DayCountDenominator(dayCountBasis));
    }

    public static decimal PeriodManagementFeeFromDailyAccruals(IEnumerable<decimal> dailyManagementFeeAccruals)
    {
        return Round(dailyManagementFeeAccruals.Sum());
    }

    public static decimal PeriodManagementFeeFromAverageNav(decimal averageNav, decimal annualRate, int periodDays, FeeDayCountBasis dayCountBasis)
    {
        FeesTaxDistributionValidation.Positive(periodDays, nameof(periodDays));
        return Round(averageNav * (annualRate / 100m) * periodDays / DayCountDenominator(dayCountBasis));
    }

    public static decimal DailyCustodyFee(decimal applicableFeeBase, decimal annualCustodyFeeRate, FeeDayCountBasis dayCountBasis)
    {
        return Round(applicableFeeBase * (annualCustodyFeeRate / 100m) / DayCountDenominator(dayCountBasis));
    }

    public static decimal DailyTrusteeFee(decimal applicableFeeBase, decimal annualTrusteeFeeRate, FeeDayCountBasis dayCountBasis)
    {
        return Round(applicableFeeBase * (annualTrusteeFeeRate / 100m) / DayCountDenominator(dayCountBasis));
    }

    public static decimal AdminFee(decimal applicableFeeBase, decimal adminFeeRate, int periodDays, FeeDayCountBasis dayCountBasis, decimal? fixedFeePerPeriod)
    {
        return Round(fixedFeePerPeriod ?? applicableFeeBase * (adminFeeRate / 100m) * periodDays / DayCountDenominator(dayCountBasis));
    }

    public static decimal EntryFee(decimal grossContribution, decimal? entryFeeRate, decimal? fixedAmount, IReadOnlyCollection<FeeTierInput>? tieredRules = null)
    {
        return TierOrRateFee(grossContribution, entryFeeRate, fixedAmount, tieredRules, nameof(grossContribution));
    }

    public static decimal ExitFee(decimal grossRedemptionValue, decimal? exitFeeRate, decimal? fixedAmount, IReadOnlyCollection<FeeTierInput>? tieredRules = null)
    {
        return TierOrRateFee(grossRedemptionValue, exitFeeRate, fixedAmount, tieredRules, nameof(grossRedemptionValue));
    }

    public static decimal SwitchFee(decimal switchOutValue, decimal? switchFeeRate, decimal? fixedAmount)
    {
        return TierOrRateFee(switchOutValue, switchFeeRate, fixedAmount, null, nameof(switchOutValue));
    }

    public static decimal PerformanceFee(decimal performanceBase, decimal hurdleBase, decimal highWaterMarkBase, decimal performanceFeeRate)
    {
        FeesTaxDistributionValidation.Percentage(performanceFeeRate, nameof(performanceFeeRate));
        var threshold = Math.Max(hurdleBase, highWaterMarkBase);
        return Round(Math.Max(0m, performanceBase - threshold) * (performanceFeeRate / 100m));
    }

    public static decimal Vat(decimal taxableFeeAmount, decimal vatRate)
    {
        FeesTaxDistributionValidation.Percentage(vatRate, nameof(vatRate));
        return Round(taxableFeeAmount * (vatRate / 100m));
    }

    public static decimal WithholdingTax(decimal taxablePaymentBase, decimal withholdingTaxRate)
    {
        FeesTaxDistributionValidation.Percentage(withholdingTaxRate, nameof(withholdingTaxRate));
        return Round(taxablePaymentBase * (withholdingTaxRate / 100m));
    }

    public static decimal NetPayable(decimal feeAmount, decimal vat, decimal withholdingTax)
    {
        return Round(feeAmount + vat - withholdingTax);
    }

    public static decimal TotalExpenseRatio(decimal totalOperatingExpensesForPeriod, decimal averageNav)
    {
        FeesTaxDistributionValidation.Positive(averageNav, nameof(averageNav));
        return Round(totalOperatingExpensesForPeriod / averageNav);
    }

    public static decimal GrossDistributableIncome(decimal investmentIncome, decimal otherIncome, decimal realizedGainsLosses, decimal priorPeriodAdjustments)
    {
        return Round(investmentIncome + otherIncome + realizedGainsLosses + priorPeriodAdjustments);
    }

    public static decimal NetDistributableIncome(decimal grossDistributableIncome, decimal fundExpenses, decimal fees, decimal taxes, decimal reserveTransfers)
    {
        return Round(grossDistributableIncome - fundExpenses - fees - taxes - reserveTransfers);
    }

    public static decimal DistributionPerUnit(decimal netDistributableIncome, decimal eligibleUnits)
    {
        FeesTaxDistributionValidation.Positive(eligibleUnits, nameof(eligibleUnits));
        return Round(netDistributableIncome / eligibleUnits);
    }

    public static decimal InvestorGrossDistribution(decimal eligibleInvestorUnits, decimal distributionPerUnit)
    {
        return Round(eligibleInvestorUnits * distributionPerUnit);
    }

    public static decimal InvestorNetDistribution(decimal investorGrossDistribution, decimal investorLevelTax)
    {
        return Round(investorGrossDistribution - investorLevelTax);
    }

    public static decimal ReinvestmentUnits(decimal investorNetDistribution, decimal reinvestmentPrice)
    {
        FeesTaxDistributionValidation.Positive(reinvestmentPrice, nameof(reinvestmentPrice));
        return Round(investorNetDistribution / reinvestmentPrice);
    }

    public static decimal DistributionCoverage(decimal availableCashForDistribution, decimal plannedDistributionAmount)
    {
        FeesTaxDistributionValidation.Positive(plannedDistributionAmount, nameof(plannedDistributionAmount));
        return Round(availableCashForDistribution / plannedDistributionAmount);
    }

    public static decimal InvestorReturnForPeriod(decimal closingValue, decimal openingValue, decimal cashDistributions, decimal netContributions)
    {
        FeesTaxDistributionValidation.Positive(openingValue, nameof(openingValue));
        return Round((closingValue - openingValue + cashDistributions - netContributions) / openingValue);
    }

    public static decimal Round(decimal value, int precision = InternalPrecision)
    {
        return decimal.Round(value, precision, MidpointRounding.AwayFromZero);
    }

    public static int DayCountDenominator(FeeDayCountBasis basis)
    {
        return basis switch
        {
            FeeDayCountBasis.Actual365 => 365,
            FeeDayCountBasis.Actual366 => 366,
            FeeDayCountBasis.Actual360 => 360,
            _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "Unsupported day-count basis.")
        };
    }

    private static decimal TierOrRateFee(decimal amount, decimal? rate, decimal? fixedAmount, IReadOnlyCollection<FeeTierInput>? tieredRules, string parameterName)
    {
        FeesTaxDistributionValidation.NonNegative(amount, parameterName);

        if (tieredRules is { Count: > 0 })
        {
            var matchingRule = tieredRules
                .OrderBy(rule => rule.FromAmount ?? decimal.MinValue)
                .FirstOrDefault(rule =>
                {
                    var from = rule.FromAmount ?? decimal.MinValue;
                    var to = rule.ToAmount ?? decimal.MaxValue;
                    return amount >= from && amount < to;
                });

            if (matchingRule is null)
            {
                throw new ArgumentException("No fee tier matches the calculation amount.", nameof(tieredRules));
            }

            return Round(matchingRule.FixedAmount ?? amount * ((matchingRule.Rate ?? 0m) / 100m));
        }

        if (fixedAmount.HasValue)
        {
            return Round(fixedAmount.Value);
        }

        if (rate.HasValue)
        {
            FeesTaxDistributionValidation.Percentage(rate.Value, nameof(rate));
            return Round(amount * (rate.Value / 100m));
        }

        return 0m;
    }
}

public sealed record FeeTierInput(decimal? FromAmount, decimal? ToAmount, decimal? Rate, decimal? FixedAmount);

public sealed record FeeAccrualFormulaOutput(
    decimal DailyManagementFee,
    decimal PeriodManagementFee,
    decimal DailyCustodyFee,
    decimal DailyTrusteeFee,
    decimal AdminFee,
    decimal EntryFee,
    decimal ExitFee,
    decimal SwitchFee,
    decimal PerformanceFee,
    decimal Vat,
    decimal WithholdingTax,
    decimal NetPayable,
    decimal TotalExpenseRatio);

public sealed record DistributionFormulaOutput(
    decimal GrossDistributableIncome,
    decimal NetDistributableIncome,
    decimal DistributionPerUnit,
    decimal DistributionCoverage);
