namespace Cis.Domain.NAV;

public static class NavFormulaEngine
{
    public const int InternalPrecision = 6;

    public static decimal GrossAssetValue(
        decimal investmentValues,
        decimal cashAndBank,
        decimal accruedIncome,
        decimal otherReceivables,
        decimal prepayments)
    {
        return Round(investmentValues + cashAndBank + accruedIncome + otherReceivables + prepayments);
    }

    public static decimal InvestmentValue(
        decimal quantity,
        decimal marketPrice,
        decimal accruedIncome,
        bool useAmortizedCost,
        decimal? amortizedCost)
    {
        var value = useAmortizedCost
            ? amortizedCost ?? throw new ArgumentException("Amortized cost is required when policy permits amortized valuation.", nameof(amortizedCost))
            : quantity * marketPrice + accruedIncome;

        return Round(value);
    }

    public static decimal AccruedIncome(decimal principal, decimal annualRate, int accrualDays, DayCountBasis basis)
    {
        if (accrualDays < 0)
        {
            throw new ArgumentException("Accrual days cannot be negative.", nameof(accrualDays));
        }

        var value = principal * (annualRate / 100m) * accrualDays / DayCountDenominator(basis);
        return Round(value);
    }

    public static decimal DailyAccretion(decimal maturityValue, decimal purchaseCost, int totalDaysToMaturity)
    {
        NavValidation.Positive(totalDaysToMaturity, nameof(totalDaysToMaturity));
        return Round((maturityValue - purchaseCost) / totalDaysToMaturity);
    }

    public static decimal NetAssetValue(decimal grossAssetValue, decimal totalLiabilities, decimal accruedExpenses)
    {
        return Round(grossAssetValue - totalLiabilities - accruedExpenses);
    }

    public static decimal ClosingUnits(
        decimal openingUnits,
        decimal unitsIssued,
        decimal unitsRedeemed,
        decimal approvedUnitAdjustments)
    {
        return Round(openingUnits + unitsIssued - unitsRedeemed + approvedUnitAdjustments);
    }

    public static decimal UnitPrice(decimal nav, decimal unitsInIssue)
    {
        NavValidation.Positive(unitsInIssue, nameof(unitsInIssue));
        return Round(nav / unitsInIssue);
    }

    public static decimal NetSubscriptionAmount(decimal grossContribution, decimal entryFee, decimal subscriptionTaxCharges)
    {
        return Round(grossContribution - entryFee - subscriptionTaxCharges);
    }

    public static decimal UnitsAllocated(decimal netSubscriptionAmount, decimal applicableUnitPrice)
    {
        NavValidation.Positive(applicableUnitPrice, nameof(applicableUnitPrice));
        return Round(netSubscriptionAmount / applicableUnitPrice);
    }

    public static decimal GrossRedemptionValue(decimal unitsRedeemed, decimal applicableRedemptionPrice)
    {
        return Round(unitsRedeemed * applicableRedemptionPrice);
    }

    public static decimal NetRedemptionPayable(decimal grossRedemptionValue, decimal exitFee, decimal redemptionTaxCharges)
    {
        return Round(grossRedemptionValue - exitFee - redemptionTaxCharges);
    }

    public static decimal SwitchOutValue(decimal unitsSwitchedOut, decimal sourceFundPrice)
    {
        return Round(unitsSwitchedOut * sourceFundPrice);
    }

    public static decimal SwitchInUnits(decimal netSwitchAmount, decimal targetFundPrice)
    {
        NavValidation.Positive(targetFundPrice, nameof(targetFundPrice));
        return Round(netSwitchAmount / targetFundPrice);
    }

    public static decimal RedeemableAmount(
        decimal availableUnits,
        decimal applicablePrice,
        decimal estimatedExitCharges,
        decimal applicableTax)
    {
        return Round((availableUnits * applicablePrice) - estimatedExitCharges - applicableTax);
    }

    public static decimal PriceVariancePercentage(decimal currentPrice, decimal priorPrice)
    {
        NavValidation.Positive(priorPrice, nameof(priorPrice));
        return Round(Math.Abs((currentPrice - priorPrice) / priorPrice * 100m));
    }

    public static decimal Round(decimal value, int precision = InternalPrecision)
    {
        return decimal.Round(value, precision, MidpointRounding.AwayFromZero);
    }

    public static int DayCountDenominator(DayCountBasis basis)
    {
        return basis switch
        {
            DayCountBasis.Actual365 => 365,
            DayCountBasis.Actual366 => 366,
            DayCountBasis.Actual360 => 360,
            _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "Unsupported day-count basis.")
        };
    }
}

public sealed record NavFormulaOutput(
    decimal InvestmentValue,
    decimal InstrumentAccruedIncome,
    decimal DailyAccretion,
    decimal CashAndBank,
    decimal OtherReceivables,
    decimal Prepayments,
    decimal GrossAssetValue,
    decimal TotalLiabilities,
    decimal AccruedExpenses,
    decimal NetAssetValue,
    decimal OpeningUnits,
    decimal UnitsIssued,
    decimal UnitsRedeemed,
    decimal ApprovedUnitAdjustments,
    decimal ClosingUnits,
    decimal UnitPrice,
    decimal NetSubscriptionAmount,
    decimal UnitsAllocated,
    decimal GrossRedemptionValue,
    decimal NetRedemptionPayable,
    decimal SwitchOutValue,
    decimal SwitchInUnits,
    decimal RedeemableAmount);
