using Cis.Domain.Common;

namespace Cis.Domain.Dealing;

public sealed class DealingValidationResult : Entity
{
    private DealingValidationResult()
    {
    }

    private DealingValidationResult(Guid dealingInstructionId, string ruleCode, string message, ValidationSeverity severity, bool passed, DateTime createdAtUtc)
    {
        DealingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        DealingInstructionId = dealingInstructionId;
        RuleCode = DealingValidation.Required(ruleCode, nameof(ruleCode), 100);
        Message = DealingValidation.Required(message, nameof(message), 1000);
        Severity = severity;
        Passed = passed;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public string RuleCode { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public ValidationSeverity Severity { get; private set; }

    public bool Passed { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static DealingValidationResult Create(Guid dealingInstructionId, string ruleCode, string message, ValidationSeverity severity, bool passed, DateTime createdAtUtc)
    {
        return new DealingValidationResult(dealingInstructionId, ruleCode, message, severity, passed, createdAtUtc);
    }
}

public sealed class CutOffBreach : Entity
{
    private CutOffBreach()
    {
    }

    private CutOffBreach(Guid dealingInstructionId, DateTime receivedAtUtc, TimeOnly cutOffTime, string reason)
    {
        DealingValidation.EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        DealingInstructionId = dealingInstructionId;
        ReceivedAtUtc = receivedAtUtc;
        CutOffTime = cutOffTime;
        Reason = DealingValidation.Required(reason, nameof(reason), 1000);
        RequiresApproval = true;
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public DateTime ReceivedAtUtc { get; private set; }

    public TimeOnly CutOffTime { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public bool RequiresApproval { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public static CutOffBreach Create(Guid dealingInstructionId, DateTime receivedAtUtc, TimeOnly cutOffTime, string reason)
    {
        return new CutOffBreach(dealingInstructionId, receivedAtUtc, cutOffTime, reason);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        DealingValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        ApprovedByUserId = DealingValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        ApprovedAtUtc = approvedAtUtc;
        RequiresApproval = false;
    }
}

public sealed class InstructionStatusHistory : Entity
{
    private InstructionStatusHistory()
    {
    }

    private InstructionStatusHistory(Guid dealingInstructionId, DealingInstructionStatus status, string changedByUserId, DateTime changedAtUtc, string? comment)
    {
        DealingValidation.EnsureUtc(changedAtUtc, nameof(changedAtUtc));
        DealingInstructionId = dealingInstructionId;
        Status = status;
        ChangedByUserId = DealingValidation.Required(changedByUserId, nameof(changedByUserId), 200);
        ChangedAtUtc = changedAtUtc;
        Comment = DealingValidation.Optional(comment, 1000);
    }

    public Guid DealingInstructionId { get; private set; }

    public DealingInstruction DealingInstruction { get; private set; } = null!;

    public DealingInstructionStatus Status { get; private set; }

    public string ChangedByUserId { get; private set; } = string.Empty;

    public DateTime ChangedAtUtc { get; private set; }

    public string? Comment { get; private set; }

    public static InstructionStatusHistory Create(Guid dealingInstructionId, DealingInstructionStatus status, string changedByUserId, DateTime changedAtUtc, string? comment)
    {
        return new InstructionStatusHistory(dealingInstructionId, status, changedByUserId, changedAtUtc, comment);
    }
}

public sealed class ApprovalThreshold : Entity
{
    private ApprovalThreshold()
    {
    }

    private ApprovalThreshold(DealingInstructionType instructionType, string currency, decimal thresholdAmount, bool isActive)
    {
        InstructionType = instructionType;
        Currency = DealingValidation.Currency(currency, nameof(currency));
        ThresholdAmount = DealingValidation.Positive(thresholdAmount, nameof(thresholdAmount));
        IsActive = isActive;
    }

    public DealingInstructionType InstructionType { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal ThresholdAmount { get; private set; }

    public bool IsActive { get; private set; }

    public static ApprovalThreshold Create(DealingInstructionType instructionType, string currency, decimal thresholdAmount, bool isActive = true)
    {
        return new ApprovalThreshold(instructionType, currency, thresholdAmount, isActive);
    }
}

public sealed class DealingBatch : AuditableAggregateRoot
{
    private DealingBatch()
    {
    }

    private DealingBatch(string batchNumber, DealingChannel channel, BusinessDate businessDate, string createdByUserId, DateTime createdAtUtc)
    {
        DealingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        BatchNumber = DealingValidation.Required(batchNumber, nameof(batchNumber), 100);
        Channel = channel;
        BusinessDate = businessDate;
        Status = DealingBatchStatus.Open;
        InstructionCount = 0;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string BatchNumber { get; private set; } = string.Empty;

    public DealingChannel Channel { get; private set; }

    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public DealingBatchStatus Status { get; private set; }

    public int InstructionCount { get; private set; }

    public static DealingBatch Create(string batchNumber, DealingChannel channel, BusinessDate businessDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new DealingBatch(batchNumber, channel, businessDate, createdByUserId, createdAtUtc);
    }
}
