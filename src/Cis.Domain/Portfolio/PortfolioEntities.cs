using Cis.Domain.Common;

namespace Cis.Domain.Portfolio;

public sealed class Instrument : AuditableAggregateRoot
{
    private Instrument()
    {
    }

    private Instrument(
        string isin,
        string name,
        InstrumentType instrumentType,
        string currency,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        PortfolioValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Isin = PortfolioValidation.Required(isin, nameof(isin), 20).ToUpperInvariant();
        Name = PortfolioValidation.Required(name, nameof(name), 200);
        InstrumentType = instrumentType;
        Currency = PortfolioValidation.Currency(currency, nameof(currency));
        Status = PlacementStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Isin { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public InstrumentType InstrumentType { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public PlacementStatus Status { get; private set; }
    public Guid? CounterpartyId { get; private set; }
    public Guid? IssuerId { get; private set; }
    public decimal? YieldRate { get; private set; }
    public decimal? CouponRate { get; private set; }

    public static Instrument Create(
        string isin,
        string name,
        InstrumentType instrumentType,
        string currency,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Instrument(isin, name, instrumentType, currency, createdByUserId, createdAtUtc);
    }

    public void AttachCounterparty(Guid counterpartyId)
    {
        CounterpartyId = counterpartyId != Guid.Empty ? counterpartyId : throw new ArgumentException("Counterparty id cannot be empty.", nameof(counterpartyId));
    }

    public void AttachIssuer(Guid issuerId)
    {
        IssuerId = issuerId != Guid.Empty ? issuerId : throw new ArgumentException("Issuer id cannot be empty.", nameof(issuerId));
    }

    public void SetYieldAndCoupon(decimal? yieldRate, decimal? couponRate)
    {
        YieldRate = yieldRate.HasValue ? PortfolioValidation.NonNegative(yieldRate.Value, nameof(yieldRate)) : null;
        CouponRate = couponRate.HasValue ? PortfolioValidation.NonNegative(couponRate.Value, nameof(couponRate)) : null;
    }

    public void Activate()
    {
        if (Status != PlacementStatus.Draft)
        {
            throw new InvalidOperationException("Only draft instruments can be activated.");
        }
        Status = PlacementStatus.Active;
    }
}

public sealed class Counterparty : AuditableAggregateRoot
{
    private Counterparty()
    {
    }

    private Counterparty(
        string code,
        string name,
        string? contact,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        PortfolioValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Code = PortfolioValidation.Required(code, nameof(code), 50).ToUpperInvariant();
        Name = PortfolioValidation.Required(name, nameof(name), 200);
        Contact = PortfolioValidation.Optional(contact, 200);
        Status = PlacementStatus.Active;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Contact { get; private set; }
    public PlacementStatus Status { get; private set; }

    public static Counterparty Create(
        string code,
        string name,
        string? contact,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Counterparty(code, name, contact, createdByUserId, createdAtUtc);
    }

    public void Deactivate()
    {
        Status = PlacementStatus.Cancelled;
    }
}

public sealed class Issuer : AuditableAggregateRoot
{
    private Issuer()
    {
    }

    private Issuer(
        string code,
        string name,
        string? contact,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        PortfolioValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Code = PortfolioValidation.Required(code, nameof(code), 50).ToUpperInvariant();
        Name = PortfolioValidation.Required(name, nameof(name), 200);
        Contact = PortfolioValidation.Optional(contact, 200);
        Status = PlacementStatus.Active;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Contact { get; private set; }
    public PlacementStatus Status { get; private set; }

    public static Issuer Create(
        string code,
        string name,
        string? contact,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Issuer(code, name, contact, createdByUserId, createdAtUtc);
    }

    public void Deactivate()
    {
        Status = PlacementStatus.Cancelled;
    }
}

public sealed class Placement : AuditableAggregateRoot
{
    private readonly List<IncomeSchedule> _incomeSchedules = [];

    private Placement()
    {
    }

    private Placement(
        Guid schemeId,
        Guid schemeClassId,
        Guid instrumentId,
        Guid? counterpartyId,
        Guid? issuerId,
        decimal principal,
        string currency,
        BusinessDate acquisitionDate,
        BusinessDate maturityDate,
        decimal yield,
        decimal accruedIncome,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        PortfolioValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        CounterpartyId = counterpartyId;
        IssuerId = issuerId;
        Principal = PortfolioValidation.Positive(principal, nameof(principal));
        Currency = PortfolioValidation.Currency(currency, nameof(currency));
        AcquisitionDate = acquisitionDate;
        MaturityDate = maturityDate;
        if (maturityDate.Value < acquisitionDate.Value)
        {
            throw new ArgumentException("Maturity date cannot be before acquisition date.", nameof(maturityDate));
        }

        Yield = PortfolioValidation.NonNegative(yield, nameof(yield));
        AccruedIncome = PortfolioValidation.NonNegative(accruedIncome, nameof(accruedIncome));
        Status = PlacementStatus.Draft;
        SettlementStatus = SettlementStatus.Pending;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public Guid? CounterpartyId { get; private set; }
    public Guid? IssuerId { get; private set; }
    public decimal Principal { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BusinessDate AcquisitionDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate MaturityDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal Yield { get; private set; }
    public decimal AccruedIncome { get; private set; }
    public PlacementStatus Status { get; private set; }
    public SettlementStatus SettlementStatus { get; private set; }
    public string? SubmittedByUserId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectedByUserId { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<IncomeSchedule> IncomeSchedules => _incomeSchedules.AsReadOnly();

    public static Placement Create(
        Guid schemeId,
        Guid schemeClassId,
        Guid instrumentId,
        Guid? counterpartyId,
        Guid? issuerId,
        decimal principal,
        string currency,
        BusinessDate acquisitionDate,
        BusinessDate maturityDate,
        decimal yield,
        decimal accruedIncome,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Placement(
            schemeId, schemeClassId, instrumentId, counterpartyId, issuerId,
            principal, currency, acquisitionDate, maturityDate, yield, accruedIncome,
            createdByUserId, createdAtUtc);
    }

    public void Submit(string submittedByUserId, DateTime submittedAtUtc)
    {
        PortfolioValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status != PlacementStatus.Draft)
        {
            throw new InvalidOperationException("Only draft placements can be submitted.");
        }

        SubmittedByUserId = PortfolioValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = submittedAtUtc;
        Status = PlacementStatus.PendingApproval;
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        PortfolioValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != PlacementStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending placements can be approved.");
        }

        if (string.Equals(approvedByUserId, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot approve the same placement.");
        }

        ApprovedByUserId = PortfolioValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        ApprovedAtUtc = approvedAtUtc;
        Status = PlacementStatus.Approved;
        SettlementStatus = SettlementStatus.Settled;
    }

    public void Reject(string rejectedByUserId, DateTime rejectedAtUtc, string reason)
    {
        PortfolioValidation.EnsureUtc(rejectedAtUtc, nameof(rejectedAtUtc));
        if (Status != PlacementStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending placements can be rejected.");
        }

        RejectedByUserId = PortfolioValidation.Required(rejectedByUserId, nameof(rejectedByUserId), 200);
        RejectedAtUtc = rejectedAtUtc;
        RejectionReason = PortfolioValidation.Required(reason, nameof(reason), 1000);
        Status = PlacementStatus.Rejected;
    }

    public void MarkMatured()
    {
        if (Status == PlacementStatus.Active || Status == PlacementStatus.Approved)
        {
            Status = PlacementStatus.Matured;
        }
    }

    public void MarkLiquidated()
    {
        Status = PlacementStatus.Liquidated;
    }

    public void AddIncomeSchedule(IncomeSchedule schedule)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        _incomeSchedules.Add(schedule);
    }
}

public sealed class IncomeSchedule : Entity
{
    private IncomeSchedule()
    {
    }

    private IncomeSchedule(
        Guid placementId,
        string incomeType,
        BusinessDate dueDate,
        decimal amount,
        bool received,
        DateTime createdAtUtc)
    {
        PlacementId = placementId != Guid.Empty ? placementId : throw new ArgumentException("Placement id cannot be empty.", nameof(placementId));
        IncomeType = PortfolioValidation.Required(incomeType, nameof(incomeType), 50);
        DueDate = dueDate;
        Amount = PortfolioValidation.NonNegative(amount, nameof(amount));
        Received = received;
    }

    public Guid PlacementId { get; private set; }
    public string IncomeType { get; private set; } = string.Empty;
    public BusinessDate DueDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal Amount { get; private set; }
    public bool Received { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public string? ReceivedByUserId { get; private set; }

    public static IncomeSchedule Create(
        Guid placementId,
        string incomeType,
        BusinessDate dueDate,
        decimal amount,
        bool received,
        DateTime createdAtUtc)
    {
        return new IncomeSchedule(placementId, incomeType, dueDate, amount, received, createdAtUtc);
    }

    public void MarkReceived(string receivedByUserId, DateTime receivedAtUtc)
    {
        PortfolioValidation.EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        if (Received)
        {
            throw new InvalidOperationException("Income already marked as received.");
        }

        ReceivedByUserId = PortfolioValidation.Required(receivedByUserId, nameof(receivedByUserId), 200);
        ReceivedAtUtc = receivedAtUtc;
        Received = true;
    }
}

public sealed class PortfolioHolding : Entity
{
    private PortfolioHolding()
    {
    }

    private PortfolioHolding(
        Guid schemeId,
        Guid schemeClassId,
        Guid instrumentId,
        decimal quantity,
        decimal marketValue,
        string currency,
        DateTime createdAtUtc)
    {
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        Quantity = PortfolioValidation.Positive(quantity, nameof(quantity));
        MarketValue = PortfolioValidation.NonNegative(marketValue, nameof(marketValue));
        Currency = PortfolioValidation.Currency(currency, nameof(currency));
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal MarketValue { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime LastUpdatedAtUtc { get; private set; }

    public static PortfolioHolding Create(
        Guid schemeId,
        Guid schemeClassId,
        Guid instrumentId,
        decimal quantity,
        decimal marketValue,
        string currency,
        DateTime createdAtUtc)
    {
        return new PortfolioHolding(schemeId, schemeClassId, instrumentId, quantity, marketValue, currency, createdAtUtc);
    }

    public void UpdateQuantity(decimal newQuantity, DateTime updatedAtUtc)
    {
        PortfolioValidation.EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        Quantity = PortfolioValidation.Positive(newQuantity, nameof(newQuantity));
        LastUpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateMarketValue(decimal newValue, DateTime updatedAtUtc)
    {
        PortfolioValidation.EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        MarketValue = PortfolioValidation.NonNegative(newValue, nameof(newValue));
        LastUpdatedAtUtc = updatedAtUtc;
    }
}

public sealed class InvestmentTransaction : AuditableAggregateRoot
{
    private InvestmentTransaction()
    {
    }

    private InvestmentTransaction(
        Guid placementId,
        InvestmentTransactionType transactionType,
        decimal amount,
        string currency,
        BusinessDate transactionDate,
        SettlementStatus settlementStatus,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        PortfolioValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        PlacementId = placementId != Guid.Empty ? placementId : throw new ArgumentException("Placement id cannot be empty.", nameof(placementId));
        TransactionType = transactionType;
        Amount = PortfolioValidation.Positive(amount, nameof(amount));
        Currency = PortfolioValidation.Currency(currency, nameof(currency));
        TransactionDate = transactionDate;
        SettlementStatus = settlementStatus;
        Reference = $"TXN-{Guid.NewGuid():N}"[..50];
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid PlacementId { get; private set; }
    public InvestmentTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BusinessDate TransactionDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public SettlementStatus SettlementStatus { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public static InvestmentTransaction Create(
        Guid placementId,
        InvestmentTransactionType transactionType,
        decimal amount,
        string currency,
        BusinessDate transactionDate,
        SettlementStatus settlementStatus,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new InvestmentTransaction(placementId, transactionType, amount, currency, transactionDate, settlementStatus, createdByUserId, createdAtUtc);
    }

    public void Settle()
    {
        if (SettlementStatus != SettlementStatus.Pending)
        {
            throw new InvalidOperationException("Only pending transactions can be settled.");
        }

        SettlementStatus = SettlementStatus.Settled;
    }

    public void MarkFailed(string reason)
    {
        if (SettlementStatus == SettlementStatus.Settled)
        {
            throw new InvalidOperationException("Cannot mark settled transactions as failed.");
        }

        SettlementStatus = SettlementStatus.Failed;
        Notes = PortfolioValidation.Optional(reason, 1000);
    }
}

public sealed class MandateValidationResult : Entity
{
    private MandateValidationResult()
    {
    }

    public Guid PlacementId { get; private set; }
    public MandateValidationStatus Status { get; private set; }
    public string ValidatedField { get; private set; } = string.Empty;
    public decimal? LimitValue { get; private set; }
    public decimal? ActualValue { get; private set; }
    public string? ValidationMessage { get; private set; }
    public DateTime ValidatedAtUtc { get; private set; }

    public static MandateValidationResult Create(
        Guid placementId,
        MandateValidationStatus status,
        string validatedField,
        decimal? limitValue,
        decimal? actualValue,
        string? validationMessage,
        DateTime validatedAtUtc)
    {
        return new MandateValidationResult
        {
            PlacementId = placementId != Guid.Empty ? placementId : throw new ArgumentException("Placement id cannot be empty.", nameof(placementId)),
            Status = status,
            ValidatedField = PortfolioValidation.Required(validatedField, nameof(validatedField), 100),
            LimitValue = limitValue,
            ActualValue = actualValue,
            ValidationMessage = PortfolioValidation.Optional(validationMessage, 500),
            ValidatedAtUtc = validatedAtUtc
        };
    }
}

public sealed class CounterpartyExposure : Entity
{
    private CounterpartyExposure()
    {
    }

    public Guid SchemeId { get; private set; }
    public Guid CounterpartyId { get; private set; }
    public decimal TotalExposure { get; private set; }
    public decimal ExposurePercentage { get; private set; }
    public decimal LimitPercentage { get; private set; }
    public CounterpartyExposureStatus Status { get; private set; }
    public DateTime LastCalculatedAtUtc { get; private set; }

    public static CounterpartyExposure Create(
        Guid schemeId,
        Guid counterpartyId,
        decimal totalExposure,
        decimal exposurePercentage,
        decimal limitPercentage,
        DateTime calculatedAtUtc)
    {
        PortfolioValidation.EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        var status = exposurePercentage <= limitPercentage
            ? CounterpartyExposureStatus.Compliant
            : exposurePercentage <= limitPercentage * 1.1m
                ? CounterpartyExposureStatus.Warning
                : CounterpartyExposureStatus.Breach;

        return new CounterpartyExposure
        {
            SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)),
            CounterpartyId = counterpartyId != Guid.Empty ? counterpartyId : throw new ArgumentException("Counterparty id cannot be empty.", nameof(counterpartyId)),
            TotalExposure = PortfolioValidation.NonNegative(totalExposure, nameof(totalExposure)),
            ExposurePercentage = PortfolioValidation.Percentage(exposurePercentage, nameof(exposurePercentage)),
            LimitPercentage = PortfolioValidation.Percentage(limitPercentage, nameof(limitPercentage)),
            Status = status,
            LastCalculatedAtUtc = calculatedAtUtc
        };
    }
}
