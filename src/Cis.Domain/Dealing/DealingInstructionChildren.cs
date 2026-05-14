using Cis.Domain.Common;

namespace Cis.Domain.Dealing;

public sealed class SubscriptionInstruction : Entity
{
    private SubscriptionInstruction()
    {
    }

    private SubscriptionInstruction(
        Guid dealingInstructionId,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        string currency,
        bool fundsCleared,
        bool approvedNavAvailable,
        decimal? approvedNavPrice,
        BusinessDate? approvedNavDate)
    {
        DealingInstructionId = dealingInstructionId;
        Mode = mode;
        Amount = amount.HasValue ? DealingValidation.Positive(amount.Value, nameof(amount)) : null;
        Units = units.HasValue ? DealingValidation.Positive(units.Value, nameof(units)) : null;
        Currency = DealingValidation.Currency(currency, nameof(currency));
        FundsCleared = fundsCleared;
        ApprovedNavAvailable = approvedNavAvailable;
        ApprovedNavPrice = approvedNavPrice.HasValue ? DealingValidation.Positive(approvedNavPrice.Value, nameof(approvedNavPrice)) : null;
        ApprovedNavDate = approvedNavDate;
        if (mode == DealingInstructionMode.Amount && !Amount.HasValue)
        {
            throw new ArgumentException("Amount is required for amount-based subscriptions.", nameof(amount));
        }

        if (mode == DealingInstructionMode.Units && !Units.HasValue)
        {
            throw new ArgumentException("Units are required for unit-based subscriptions.", nameof(units));
        }
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public DealingInstructionMode Mode { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? Units { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool FundsCleared { get; private set; }

    public bool ApprovedNavAvailable { get; private set; }

    public decimal? ApprovedNavPrice { get; private set; }

    public BusinessDate? ApprovedNavDate { get; private set; }

    public decimal? AllocatedUnits { get; private set; }

    public string? ConfirmationNumber { get; private set; }

    public static SubscriptionInstruction Create(
        Guid dealingInstructionId,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        string currency,
        bool fundsCleared,
        bool approvedNavAvailable,
        decimal? approvedNavPrice,
        BusinessDate? approvedNavDate)
    {
        return new SubscriptionInstruction(dealingInstructionId, mode, amount, units, currency, fundsCleared, approvedNavAvailable, approvedNavPrice, approvedNavDate);
    }

    public void Allocate(string confirmationNumber)
    {
        if (!FundsCleared)
        {
            throw new InvalidOperationException("Subscription cannot be allocated until funds are cleared.");
        }

        if (!ApprovedNavAvailable || !ApprovedNavPrice.HasValue)
        {
            throw new InvalidOperationException("Subscription cannot be allocated without an approved NAV.");
        }

        AllocatedUnits = Mode == DealingInstructionMode.Amount
            ? decimal.Round(Amount!.Value / ApprovedNavPrice.Value, 12, MidpointRounding.AwayFromZero)
            : Units!.Value;
        ConfirmationNumber = DealingValidation.Required(confirmationNumber, nameof(confirmationNumber), 100);
    }

    public void MarkFundsCleared()
    {
        FundsCleared = true;
    }
}

public sealed class RedemptionInstruction : Entity
{
    private RedemptionInstruction()
    {
    }

    private RedemptionInstruction(
        Guid dealingInstructionId,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        bool fullRedemption,
        decimal availableUnits,
        decimal lienUnits,
        decimal lockedUnits,
        decimal minimumBalanceUnits,
        int lockInDays,
        int noticePeriodDays,
        bool approvedNavAvailable,
        decimal approvedNavPrice,
        BusinessDate approvedNavDate,
        decimal exitFeeRate,
        decimal taxRate)
    {
        DealingInstructionId = dealingInstructionId;
        Mode = mode;
        Amount = amount.HasValue ? DealingValidation.Positive(amount.Value, nameof(amount)) : null;
        Units = units.HasValue ? DealingValidation.Positive(units.Value, nameof(units)) : null;
        FullRedemption = fullRedemption;
        AvailableUnits = DealingValidation.NonNegative(availableUnits, nameof(availableUnits));
        LienUnits = DealingValidation.NonNegative(lienUnits, nameof(lienUnits));
        LockedUnits = DealingValidation.NonNegative(lockedUnits, nameof(lockedUnits));
        MinimumBalanceUnits = DealingValidation.NonNegative(minimumBalanceUnits, nameof(minimumBalanceUnits));
        LockInDays = lockInDays < 0 ? throw new ArgumentException("lockInDays cannot be negative.", nameof(lockInDays)) : lockInDays;
        NoticePeriodDays = noticePeriodDays < 0 ? throw new ArgumentException("noticePeriodDays cannot be negative.", nameof(noticePeriodDays)) : noticePeriodDays;
        ApprovedNavAvailable = approvedNavAvailable;
        ApprovedNavPrice = DealingValidation.Positive(approvedNavPrice, nameof(approvedNavPrice));
        ApprovedNavDate = approvedNavDate;
        ExitFeeRate = DealingValidation.Percentage(exitFeeRate, nameof(exitFeeRate));
        TaxRate = DealingValidation.Percentage(taxRate, nameof(taxRate));

        if (!fullRedemption && mode == DealingInstructionMode.Amount && !Amount.HasValue)
        {
            throw new ArgumentException("Amount is required for amount-based redemptions.", nameof(amount));
        }

        if (!fullRedemption && mode == DealingInstructionMode.Units && !Units.HasValue)
        {
            throw new ArgumentException("Units are required for unit-based redemptions.", nameof(units));
        }

        ComputeAmounts();
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public DealingInstructionMode Mode { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? Units { get; private set; }

    public bool FullRedemption { get; private set; }

    public decimal AvailableUnits { get; private set; }

    public decimal LienUnits { get; private set; }

    public decimal LockedUnits { get; private set; }

    public decimal MinimumBalanceUnits { get; private set; }

    public int LockInDays { get; private set; }

    public int NoticePeriodDays { get; private set; }

    public bool ApprovedNavAvailable { get; private set; }

    public decimal ApprovedNavPrice { get; private set; }

    public BusinessDate ApprovedNavDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public decimal ExitFeeRate { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal RequestedUnits { get; private set; }

    public decimal GrossAmount { get; private set; }

    public decimal ExitFeeAmount { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal NetPayoutAmount { get; private set; }

    public bool RequiresApprovalThreshold { get; private set; }

    public bool PayoutAuthorized { get; private set; }

    public string? RedemptionAdviceNumber { get; private set; }

    public static RedemptionInstruction Create(
        Guid dealingInstructionId,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        bool fullRedemption,
        decimal availableUnits,
        decimal lienUnits,
        decimal lockedUnits,
        decimal minimumBalanceUnits,
        int lockInDays,
        int noticePeriodDays,
        bool approvedNavAvailable,
        decimal approvedNavPrice,
        BusinessDate approvedNavDate,
        decimal exitFeeRate,
        decimal taxRate)
    {
        return new RedemptionInstruction(dealingInstructionId, mode, amount, units, fullRedemption, availableUnits, lienUnits, lockedUnits, minimumBalanceUnits, lockInDays, noticePeriodDays, approvedNavAvailable, approvedNavPrice, approvedNavDate, exitFeeRate, taxRate);
    }

    public decimal RedeemableUnits => Math.Max(0m, AvailableUnits - LienUnits - LockedUnits);

    public void MarkLargeRedemption()
    {
        RequiresApprovalThreshold = true;
    }

    public void AuthorizePayout(string adviceNumber)
    {
        if (!ApprovedNavAvailable)
        {
            throw new InvalidOperationException("Redemption cannot be priced without an approved NAV.");
        }

        PayoutAuthorized = true;
        RedemptionAdviceNumber = DealingValidation.Required(adviceNumber, nameof(adviceNumber), 100);
    }

    private void ComputeAmounts()
    {
        RequestedUnits = FullRedemption
            ? RedeemableUnits
            : Mode == DealingInstructionMode.Amount
                ? decimal.Round(Amount!.Value / ApprovedNavPrice, 12, MidpointRounding.AwayFromZero)
                : Units!.Value;

        if (RequestedUnits <= 0m)
        {
            throw new InvalidOperationException("Redemption requested units must be greater than zero.");
        }

        if (RequestedUnits > RedeemableUnits)
        {
            throw new InvalidOperationException("Redemption exceeds available redeemable balance after liens and locks.");
        }

        if (!FullRedemption && AvailableUnits - RequestedUnits < MinimumBalanceUnits)
        {
            throw new InvalidOperationException("Redemption would breach the minimum balance requirement.");
        }

        GrossAmount = decimal.Round(RequestedUnits * ApprovedNavPrice, 12, MidpointRounding.AwayFromZero);
        ExitFeeAmount = decimal.Round(GrossAmount * ExitFeeRate / 100m, 12, MidpointRounding.AwayFromZero);
        TaxAmount = decimal.Round((GrossAmount - ExitFeeAmount) * TaxRate / 100m, 12, MidpointRounding.AwayFromZero);
        NetPayoutAmount = GrossAmount - ExitFeeAmount - TaxAmount;
    }
}

public sealed class SwitchInstruction : Entity
{
    private SwitchInstruction()
    {
    }

    private SwitchInstruction(
        Guid dealingInstructionId,
        Guid targetSchemeId,
        Guid targetSchemeClassId,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        decimal feeAmount,
        string ownershipHistoryJson)
    {
        DealingInstructionId = dealingInstructionId;
        TargetSchemeId = targetSchemeId;
        TargetSchemeClassId = targetSchemeClassId;
        Mode = mode;
        Amount = amount.HasValue ? DealingValidation.Positive(amount.Value, nameof(amount)) : null;
        Units = units.HasValue ? DealingValidation.Positive(units.Value, nameof(units)) : null;
        FeeAmount = DealingValidation.NonNegative(feeAmount, nameof(feeAmount));
        OwnershipHistoryJson = string.IsNullOrWhiteSpace(ownershipHistoryJson) ? "{}" : ownershipHistoryJson;
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public Guid TargetSchemeId { get; private set; }

    public Guid TargetSchemeClassId { get; private set; }

    public DealingInstructionMode Mode { get; private set; }

    public decimal? Amount { get; private set; }

    public decimal? Units { get; private set; }

    public decimal FeeAmount { get; private set; }

    public string OwnershipHistoryJson { get; private set; } = "{}";

    public static SwitchInstruction Create(Guid dealingInstructionId, Guid targetSchemeId, Guid targetSchemeClassId, DealingInstructionMode mode, decimal? amount, decimal? units, decimal feeAmount, string ownershipHistoryJson)
    {
        return new SwitchInstruction(dealingInstructionId, targetSchemeId, targetSchemeClassId, mode, amount, units, feeAmount, ownershipHistoryJson);
    }
}

public sealed class TransferInstruction : Entity
{
    private TransferInstruction()
    {
    }

    private TransferInstruction(Guid dealingInstructionId, Guid toInvestorId, decimal units, string ownershipHistoryJson)
    {
        DealingInstructionId = dealingInstructionId;
        ToInvestorId = toInvestorId;
        Units = DealingValidation.Positive(units, nameof(units));
        OwnershipHistoryJson = string.IsNullOrWhiteSpace(ownershipHistoryJson) ? "{}" : ownershipHistoryJson;
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public Guid ToInvestorId { get; private set; }

    public decimal Units { get; private set; }

    public string OwnershipHistoryJson { get; private set; } = "{}";

    public static TransferInstruction Create(Guid dealingInstructionId, Guid toInvestorId, decimal units, string ownershipHistoryJson)
    {
        return new TransferInstruction(dealingInstructionId, toInvestorId, units, ownershipHistoryJson);
    }
}
