using Cis.Domain.Common;

namespace Cis.Domain.Dealing;

public sealed class DealingInstruction : AuditableAggregateRoot
{
    private readonly List<SubscriptionInstruction> _subscriptionInstructions = [];
    private readonly List<RedemptionInstruction> _redemptionInstructions = [];
    private readonly List<SwitchInstruction> _switchInstructions = [];
    private readonly List<TransferInstruction> _transferInstructions = [];
    private readonly List<DealingValidationResult> _validationResults = [];
    private readonly List<CutOffBreach> _cutOffBreaches = [];
    private readonly List<InstructionStatusHistory> _statusHistory = [];

    private DealingInstruction()
    {
    }

    private DealingInstruction(
        string instructionNumber,
        DealingInstructionType instructionType,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        DealingChannel channel,
        BusinessDate businessDate,
        DateTime receivedAtUtc,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        DealingValidation.EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        DealingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        InstructionNumber = DealingValidation.Required(instructionNumber, nameof(instructionNumber), 100).ToUpperInvariant();
        InstructionType = instructionType;
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        Channel = channel;
        BusinessDate = businessDate;
        ReceivedAtUtc = receivedAtUtc;
        Status = DealingInstructionStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
        AddStatusHistory(DealingInstructionStatus.Draft, createdByUserId, createdAtUtc, "Instruction captured.");
    }

    public string InstructionNumber { get; private set; } = string.Empty;

    public DealingInstructionType InstructionType { get; private set; }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public DealingChannel Channel { get; private set; }

    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public DateTime ReceivedAtUtc { get; private set; }

    public DealingInstructionStatus Status { get; private set; }

    public string? SubmittedByUserId { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? RejectedByUserId { get; private set; }

    public DateTime? RejectedAtUtc { get; private set; }

    public string? CancelledByUserId { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public IReadOnlyCollection<SubscriptionInstruction> SubscriptionInstructions => _subscriptionInstructions.AsReadOnly();

    public IReadOnlyCollection<RedemptionInstruction> RedemptionInstructions => _redemptionInstructions.AsReadOnly();

    public IReadOnlyCollection<SwitchInstruction> SwitchInstructions => _switchInstructions.AsReadOnly();

    public IReadOnlyCollection<TransferInstruction> TransferInstructions => _transferInstructions.AsReadOnly();

    public IReadOnlyCollection<DealingValidationResult> ValidationResults => _validationResults.AsReadOnly();

    public IReadOnlyCollection<CutOffBreach> CutOffBreaches => _cutOffBreaches.AsReadOnly();

    public IReadOnlyCollection<InstructionStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static DealingInstruction CreateSubscription(
        string instructionNumber,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        DealingChannel channel,
        BusinessDate businessDate,
        DateTime receivedAtUtc,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        string currency,
        bool fundsCleared,
        bool approvedNavAvailable,
        decimal? approvedNavPrice,
        BusinessDate? approvedNavDate,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        var instruction = new DealingInstruction(instructionNumber, DealingInstructionType.Subscription, investorId, schemeId, schemeClassId, channel, businessDate, receivedAtUtc, createdByUserId, createdAtUtc);
        instruction._subscriptionInstructions.Add(SubscriptionInstruction.Create(instruction.Id, mode, amount, units, currency, fundsCleared, approvedNavAvailable, approvedNavPrice, approvedNavDate));
        return instruction;
    }

    public static DealingInstruction CreateRedemption(
        string instructionNumber,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        DealingChannel channel,
        BusinessDate businessDate,
        DateTime receivedAtUtc,
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
        decimal taxRate,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        var instruction = new DealingInstruction(instructionNumber, DealingInstructionType.Redemption, investorId, schemeId, schemeClassId, channel, businessDate, receivedAtUtc, createdByUserId, createdAtUtc);
        instruction._redemptionInstructions.Add(RedemptionInstruction.Create(instruction.Id, mode, amount, units, fullRedemption, availableUnits, lienUnits, lockedUnits, minimumBalanceUnits, lockInDays, noticePeriodDays, approvedNavAvailable, approvedNavPrice, approvedNavDate, exitFeeRate, taxRate));
        return instruction;
    }

    public static DealingInstruction CreateSwitch(
        string instructionNumber,
        Guid investorId,
        Guid sourceSchemeId,
        Guid sourceSchemeClassId,
        Guid targetSchemeId,
        Guid targetSchemeClassId,
        DealingChannel channel,
        BusinessDate businessDate,
        DateTime receivedAtUtc,
        DealingInstructionMode mode,
        decimal? amount,
        decimal? units,
        decimal feeAmount,
        string ownershipHistoryJson,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        var instruction = new DealingInstruction(instructionNumber, DealingInstructionType.Switch, investorId, sourceSchemeId, sourceSchemeClassId, channel, businessDate, receivedAtUtc, createdByUserId, createdAtUtc);
        instruction._switchInstructions.Add(SwitchInstruction.Create(instruction.Id, targetSchemeId, targetSchemeClassId, mode, amount, units, feeAmount, ownershipHistoryJson));
        return instruction;
    }

    public static DealingInstruction CreateTransfer(
        string instructionNumber,
        Guid fromInvestorId,
        Guid toInvestorId,
        Guid schemeId,
        Guid schemeClassId,
        DealingChannel channel,
        BusinessDate businessDate,
        DateTime receivedAtUtc,
        decimal units,
        string ownershipHistoryJson,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        var instruction = new DealingInstruction(instructionNumber, DealingInstructionType.Transfer, fromInvestorId, schemeId, schemeClassId, channel, businessDate, receivedAtUtc, createdByUserId, createdAtUtc);
        instruction._transferInstructions.Add(TransferInstruction.Create(instruction.Id, toInvestorId, units, ownershipHistoryJson));
        return instruction;
    }

    public void AddValidationResult(string ruleCode, string message, ValidationSeverity severity, bool passed, DateTime createdAtUtc)
    {
        _validationResults.Add(DealingValidationResult.Create(Id, ruleCode, message, severity, passed, createdAtUtc));
    }

    public void AddCutOffBreach(DateTime receivedAtUtc, TimeOnly cutOffTime)
    {
        _cutOffBreaches.Add(CutOffBreach.Create(Id, receivedAtUtc, cutOffTime, "Instruction was received after the scheme class cut-off time."));
        AddValidationResult("CutOffBreach", "Instruction was received after cut-off and requires approval.", ValidationSeverity.Warning, false, receivedAtUtc);
    }

    public void Submit(string submittedByUserId, DateTime submittedAtUtc)
    {
        DealingValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status != DealingInstructionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft dealing instructions can be submitted.");
        }

        SubmittedByUserId = DealingValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = submittedAtUtc;
        Status = InstructionType == DealingInstructionType.Subscription && _subscriptionInstructions.Single().FundsCleared == false
            ? DealingInstructionStatus.PendingFunds
            : DealingInstructionStatus.PendingApproval;
        AddStatusHistory(DealingInstructionStatus.Submitted, submittedByUserId, submittedAtUtc, "Instruction submitted.");
        AddStatusHistory(Status, submittedByUserId, submittedAtUtc, Status == DealingInstructionStatus.PendingFunds ? "Awaiting cleared funds." : "Awaiting approval.");
    }

    public void MarkCashVerified(string verifiedByUserId, DateTime verifiedAtUtc)
    {
        DealingValidation.EnsureUtc(verifiedAtUtc, nameof(verifiedAtUtc));
        if (Status != DealingInstructionStatus.PendingFunds)
        {
            throw new InvalidOperationException("Only instructions pending funds can be cash verified.");
        }

        var actor = DealingValidation.Required(verifiedByUserId, nameof(verifiedByUserId), 200);
        _subscriptionInstructions.Single().MarkFundsCleared();
        Status = DealingInstructionStatus.CashVerified;
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        DealingValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status is not (DealingInstructionStatus.PendingApproval or DealingInstructionStatus.PendingFunds or DealingInstructionStatus.CashVerified))
        {
            throw new InvalidOperationException("Only instructions pending approval, funds, or cash verification can be approved.");
        }

        var actor = DealingValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot approve the same dealing instruction.");
        }

        foreach (var breach in _cutOffBreaches.Where(breach => breach.RequiresApproval))
        {
            breach.Approve(actor, approvedAtUtc);
        }

        if (InstructionType == DealingInstructionType.Subscription)
        {
            var subscription = _subscriptionInstructions.Single();
            subscription.Allocate($"SUB-CONF-{InstructionNumber}");
            Status = DealingInstructionStatus.Allocated;
        }
        else if (InstructionType == DealingInstructionType.Redemption)
        {
            var redemption = _redemptionInstructions.Single();
            redemption.AuthorizePayout($"RED-ADV-{InstructionNumber}");
            Status = DealingInstructionStatus.Settled;
        }
        else
        {
            Status = DealingInstructionStatus.Approved;
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        AddStatusHistory(DealingInstructionStatus.Approved, actor, approvedAtUtc, "Instruction approved.");
        AddStatusHistory(Status, actor, approvedAtUtc, "Instruction processed after approval.");
    }

    public void Reject(string rejectedByUserId, DateTime rejectedAtUtc, string? comment)
    {
        DealingValidation.EnsureUtc(rejectedAtUtc, nameof(rejectedAtUtc));
        if (Status is DealingInstructionStatus.Allocated or DealingInstructionStatus.Settled or DealingInstructionStatus.Cancelled or DealingInstructionStatus.Reversed)
        {
            throw new InvalidOperationException("Processed, cancelled, or reversed instructions cannot be rejected.");
        }

        RejectedByUserId = DealingValidation.Required(rejectedByUserId, nameof(rejectedByUserId), 200);
        RejectedAtUtc = rejectedAtUtc;
        DecisionComment = DealingValidation.Optional(comment, 1000);
        Status = DealingInstructionStatus.Rejected;
        AddStatusHistory(Status, rejectedByUserId, rejectedAtUtc, comment ?? "Instruction rejected.");
    }

    public void Cancel(string cancelledByUserId, DateTime cancelledAtUtc, string? comment)
    {
        DealingValidation.EnsureUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        if (Status is DealingInstructionStatus.Allocated or DealingInstructionStatus.Settled or DealingInstructionStatus.Reversed)
        {
            throw new InvalidOperationException("Processed or reversed instructions cannot be cancelled.");
        }

        CancelledByUserId = DealingValidation.Required(cancelledByUserId, nameof(cancelledByUserId), 200);
        CancelledAtUtc = cancelledAtUtc;
        DecisionComment = DealingValidation.Optional(comment, 1000);
        Status = DealingInstructionStatus.Cancelled;
        AddStatusHistory(Status, cancelledByUserId, cancelledAtUtc, comment ?? "Instruction cancelled.");
    }

    private void AddStatusHistory(DealingInstructionStatus status, string changedByUserId, DateTime changedAtUtc, string? comment)
    {
        _statusHistory.Add(InstructionStatusHistory.Create(Id, status, changedByUserId, changedAtUtc, comment));
    }
}
