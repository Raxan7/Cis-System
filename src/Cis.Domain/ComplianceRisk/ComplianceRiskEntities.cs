using Cis.Domain.Common;

namespace Cis.Domain.ComplianceRisk;

public sealed class StatutoryLimit : AuditableAggregateRoot
{
    private StatutoryLimit() { }

    private StatutoryLimit(Guid? schemeId, Guid? schemeClassId, LimitType limitType, string reference, decimal limitValue, BusinessDate effectiveDate, string createdByUserId, DateTime createdAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        LimitType = limitType;
        Reference = ComplianceRiskValidation.Required(reference, nameof(reference), 200);
        LimitValue = ComplianceRiskValidation.NonNegative(limitValue, nameof(limitValue));
        EffectiveDate = effectiveDate;
        CreatedByUserId = ComplianceRiskValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ComplianceRiskValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public LimitType LimitType { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal LimitValue { get; private set; }
    public BusinessDate EffectiveDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static StatutoryLimit Create(Guid? schemeId, Guid? schemeClassId, LimitType limitType, string reference, decimal limitValue, BusinessDate effectiveDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new StatutoryLimit(schemeId, schemeClassId, limitType, reference, limitValue, effectiveDate, createdByUserId, createdAtUtc);
    }
}

public sealed class InternalPolicyLimit : AuditableAggregateRoot
{
    private InternalPolicyLimit() { }

    private InternalPolicyLimit(Guid? schemeId, Guid? schemeClassId, LimitType limitType, string reference, decimal limitValue, BusinessDate effectiveDate, string createdByUserId, DateTime createdAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        LimitType = limitType;
        Reference = ComplianceRiskValidation.Required(reference, nameof(reference), 200);
        LimitValue = ComplianceRiskValidation.NonNegative(limitValue, nameof(limitValue));
        EffectiveDate = effectiveDate;
        CreatedByUserId = ComplianceRiskValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ComplianceRiskValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public LimitType LimitType { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal LimitValue { get; private set; }
    public BusinessDate EffectiveDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static InternalPolicyLimit Create(Guid? schemeId, Guid? schemeClassId, LimitType limitType, string reference, decimal limitValue, BusinessDate effectiveDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new InternalPolicyLimit(schemeId, schemeClassId, limitType, reference, limitValue, effectiveDate, createdByUserId, createdAtUtc);
    }
}

public sealed class LimitCheckRun : AuditableAggregateRoot
{
    private readonly List<LimitBreach> _breaches = [];
    private readonly List<RelatedPartyExposure> _relatedPartyExposures = [];
    private readonly List<CounterpartyLimitUsage> _counterpartyLimitUsages = [];

    private LimitCheckRun() { }

    private LimitCheckRun(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal aum, decimal netFlow, decimal averageNav, decimal expenseToAumRatio, decimal annualizedYield, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        BusinessDate = businessDate;
        RunNumber = $"LIM-{businessDate.Value:yyyyMMdd}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        FormulaVersion = ComplianceRiskValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        AssetsUnderManagement = ComplianceRiskValidation.NonNegative(aum, nameof(aum));
        NetFlow = netFlow;
        AverageNav = ComplianceRiskValidation.Positive(averageNav, nameof(averageNav));
        ExpenseToAumRatio = ComplianceRiskValidation.NonNegative(expenseToAumRatio, nameof(expenseToAumRatio));
        AnnualizedYield = annualizedYield;
        SourceDataJson = ComplianceRiskValidation.Required(sourceDataJson, nameof(sourceDataJson), 12000);
        Status = LimitCheckRunStatus.Completed;
        RunByUserId = ComplianceRiskValidation.Required(runByUserId, nameof(runByUserId), 200);
        RunAtUtc = ComplianceRiskValidation.EnsureUtc(runAtUtc, nameof(runAtUtc));
        MarkCreated(RunByUserId, RunAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string RunNumber { get; private set; } = string.Empty;
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal AssetsUnderManagement { get; private set; }
    public decimal NetFlow { get; private set; }
    public decimal AverageNav { get; private set; }
    public decimal ExpenseToAumRatio { get; private set; }
    public decimal AnnualizedYield { get; private set; }
    public string SourceDataJson { get; private set; } = "{}";
    public LimitCheckRunStatus Status { get; private set; }
    public string RunByUserId { get; private set; } = string.Empty;
    public DateTime RunAtUtc { get; private set; }
    public IReadOnlyCollection<LimitBreach> Breaches => _breaches.AsReadOnly();
    public IReadOnlyCollection<RelatedPartyExposure> RelatedPartyExposures => _relatedPartyExposures.AsReadOnly();
    public IReadOnlyCollection<CounterpartyLimitUsage> CounterpartyLimitUsages => _counterpartyLimitUsages.AsReadOnly();

    public static LimitCheckRun Create(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal aum, decimal netFlow, decimal averageNav, decimal expenseToAumRatio, decimal annualizedYield, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        return new LimitCheckRun(schemeId, schemeClassId, businessDate, formulaVersion, aum, netFlow, averageNav, expenseToAumRatio, annualizedYield, sourceDataJson, runByUserId, runAtUtc);
    }

    public void AddBreach(LimitBreach breach) => _breaches.Add(breach ?? throw new ArgumentNullException(nameof(breach)));

    public void AddRelatedPartyExposure(RelatedPartyExposure exposure) => _relatedPartyExposures.Add(exposure ?? throw new ArgumentNullException(nameof(exposure)));

    public void AddCounterpartyLimitUsage(CounterpartyLimitUsage usage) => _counterpartyLimitUsages.Add(usage ?? throw new ArgumentNullException(nameof(usage)));
}

public sealed class LimitBreach : AuditableAggregateRoot
{
    private LimitBreach() { }

    private LimitBreach(Guid limitCheckRunId, Guid? statutoryLimitId, Guid? internalPolicyLimitId, LimitRuleScope ruleScope, LimitType limitType, string reference, decimal limitValue, decimal actualValue, BreachSeverity severity, string description, string createdByUserId, DateTime createdAtUtc)
    {
        LimitCheckRunId = limitCheckRunId;
        StatutoryLimitId = statutoryLimitId;
        InternalPolicyLimitId = internalPolicyLimitId;
        RuleScope = ruleScope;
        LimitType = limitType;
        Reference = ComplianceRiskValidation.Required(reference, nameof(reference), 200);
        LimitValue = ComplianceRiskValidation.NonNegative(limitValue, nameof(limitValue));
        ActualValue = ComplianceRiskValidation.NonNegative(actualValue, nameof(actualValue));
        Severity = severity;
        Description = ComplianceRiskValidation.Required(description, nameof(description), 1000);
        Status = BreachStatus.Open;
        CreatedByUserId = ComplianceRiskValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ComplianceRiskValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid LimitCheckRunId { get; private set; }
    public Guid? StatutoryLimitId { get; private set; }
    public Guid? InternalPolicyLimitId { get; private set; }
    public LimitRuleScope RuleScope { get; private set; }
    public LimitType LimitType { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public decimal LimitValue { get; private set; }
    public decimal ActualValue { get; private set; }
    public BreachSeverity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BreachStatus Status { get; private set; }
    public string? OwnerUserId { get; private set; }
    public string? AssignmentComment { get; private set; }
    public string? RemediationPlan { get; private set; }
    public string? ClosureEvidenceReference { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? RemediatedByUserId { get; private set; }
    public DateTime? RemediatedAtUtc { get; private set; }
    public string? ClosedByUserId { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static LimitBreach Create(Guid limitCheckRunId, Guid? statutoryLimitId, Guid? internalPolicyLimitId, LimitRuleScope ruleScope, LimitType limitType, string reference, decimal limitValue, decimal actualValue, BreachSeverity severity, string description, string createdByUserId, DateTime createdAtUtc)
    {
        return new LimitBreach(limitCheckRunId, statutoryLimitId, internalPolicyLimitId, ruleScope, limitType, reference, limitValue, actualValue, severity, description, createdByUserId, createdAtUtc);
    }

    public void Assign(string ownerUserId, string comment)
    {
        OwnerUserId = ComplianceRiskValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        AssignmentComment = ComplianceRiskValidation.Required(comment, nameof(comment), 1000);
        Status = BreachStatus.Assigned;
    }

    public void Remediate(string remediationPlan, string evidenceReference, string remediatedByUserId, DateTime remediatedAtUtc)
    {
        if (Status is BreachStatus.Closed)
        {
            throw new InvalidOperationException("Closed breaches cannot be remediated.");
        }

        RemediationPlan = ComplianceRiskValidation.Required(remediationPlan, nameof(remediationPlan), 1000);
        ClosureEvidenceReference = ComplianceRiskValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        RemediatedByUserId = ComplianceRiskValidation.Required(remediatedByUserId, nameof(remediatedByUserId), 200);
        RemediatedAtUtc = ComplianceRiskValidation.EnsureUtc(remediatedAtUtc, nameof(remediatedAtUtc));
        Status = BreachStatus.PendingClosureApproval;
    }

    public void Close(string approvedByUserId, DateTime closedAtUtc, string evidenceReference)
    {
        if (Status != BreachStatus.PendingClosureApproval)
        {
            throw new InvalidOperationException("Breach closure requires submitted remediation first.");
        }

        var actor = ComplianceRiskValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, RemediatedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: remediator cannot approve breach closure.");
        }

        ClosureEvidenceReference = ComplianceRiskValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        ClosedByUserId = actor;
        ClosedAtUtc = ComplianceRiskValidation.EnsureUtc(closedAtUtc, nameof(closedAtUtc));
        Status = BreachStatus.Closed;
    }
}

public sealed class BreachExceptionRegister : AuditableAggregateRoot
{
    private BreachExceptionRegister() { }

    private BreachExceptionRegister(Guid limitBreachId, string exceptionReason, string evidenceReference, string registeredByUserId, DateTime registeredAtUtc)
    {
        LimitBreachId = limitBreachId;
        ExceptionReason = ComplianceRiskValidation.Required(exceptionReason, nameof(exceptionReason), 1000);
        EvidenceReference = ComplianceRiskValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        RegisteredByUserId = ComplianceRiskValidation.Required(registeredByUserId, nameof(registeredByUserId), 200);
        RegisteredAtUtc = ComplianceRiskValidation.EnsureUtc(registeredAtUtc, nameof(registeredAtUtc));
        MarkCreated(RegisteredByUserId, RegisteredAtUtc);
    }

    public Guid LimitBreachId { get; private set; }
    public string ExceptionReason { get; private set; } = string.Empty;
    public string EvidenceReference { get; private set; } = string.Empty;
    public string RegisteredByUserId { get; private set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; private set; }

    public static BreachExceptionRegister Create(Guid limitBreachId, string exceptionReason, string evidenceReference, string registeredByUserId, DateTime registeredAtUtc)
    {
        return new BreachExceptionRegister(limitBreachId, exceptionReason, evidenceReference, registeredByUserId, registeredAtUtc);
    }
}

public sealed class RemediationAction : AuditableAggregateRoot
{
    private RemediationAction() { }

    private RemediationAction(Guid limitBreachId, string actionDescription, string evidenceReference, string ownerUserId, DateTime submittedAtUtc)
    {
        LimitBreachId = limitBreachId;
        ActionDescription = ComplianceRiskValidation.Required(actionDescription, nameof(actionDescription), 1000);
        EvidenceReference = ComplianceRiskValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        OwnerUserId = ComplianceRiskValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        SubmittedAtUtc = ComplianceRiskValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        Status = RemediationActionStatus.Submitted;
        MarkCreated(OwnerUserId, SubmittedAtUtc);
    }

    public Guid LimitBreachId { get; private set; }
    public string ActionDescription { get; private set; } = string.Empty;
    public string EvidenceReference { get; private set; } = string.Empty;
    public string OwnerUserId { get; private set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; private set; }
    public RemediationActionStatus Status { get; private set; }

    public static RemediationAction Create(Guid limitBreachId, string actionDescription, string evidenceReference, string ownerUserId, DateTime submittedAtUtc)
    {
        return new RemediationAction(limitBreachId, actionDescription, evidenceReference, ownerUserId, submittedAtUtc);
    }
}

public sealed class LiquidityCoverageRun : AuditableAggregateRoot
{
    private LiquidityCoverageRun() { }

    private LiquidityCoverageRun(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal availableLiquidAssets, decimal projectedShortTermRedemptions, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        BusinessDate = businessDate;
        FormulaVersion = ComplianceRiskValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        AvailableLiquidAssets = ComplianceRiskValidation.NonNegative(availableLiquidAssets, nameof(availableLiquidAssets));
        ProjectedShortTermRedemptions = ComplianceRiskValidation.Positive(projectedShortTermRedemptions, nameof(projectedShortTermRedemptions));
        LiquidityCoverageRatio = ComplianceRiskFormulaEngine.LiquidityCoverageRatio(availableLiquidAssets, projectedShortTermRedemptions);
        SourceDataJson = ComplianceRiskValidation.Required(sourceDataJson, nameof(sourceDataJson), 12000);
        RunByUserId = ComplianceRiskValidation.Required(runByUserId, nameof(runByUserId), 200);
        RunAtUtc = ComplianceRiskValidation.EnsureUtc(runAtUtc, nameof(runAtUtc));
        MarkCreated(RunByUserId, RunAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal AvailableLiquidAssets { get; private set; }
    public decimal ProjectedShortTermRedemptions { get; private set; }
    public decimal LiquidityCoverageRatio { get; private set; }
    public string SourceDataJson { get; private set; } = "{}";
    public string RunByUserId { get; private set; } = string.Empty;
    public DateTime RunAtUtc { get; private set; }

    public static LiquidityCoverageRun Create(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal availableLiquidAssets, decimal projectedShortTermRedemptions, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        return new LiquidityCoverageRun(schemeId, schemeClassId, businessDate, formulaVersion, availableLiquidAssets, projectedShortTermRedemptions, sourceDataJson, runByUserId, runAtUtc);
    }
}

public sealed class RedemptionStressScenario : AuditableAggregateRoot
{
    private RedemptionStressScenario() { }

    private RedemptionStressScenario(Guid? schemeId, string name, decimal stressRedemptionAmount, string assumptionsJson, string createdByUserId, DateTime createdAtUtc)
    {
        SchemeId = schemeId;
        Name = ComplianceRiskValidation.Required(name, nameof(name), 200);
        StressRedemptionAmount = ComplianceRiskValidation.Positive(stressRedemptionAmount, nameof(stressRedemptionAmount));
        AssumptionsJson = ComplianceRiskValidation.Required(assumptionsJson, nameof(assumptionsJson), 12000);
        Status = StressScenarioStatus.Active;
        CreatedByUserId = ComplianceRiskValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ComplianceRiskValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal StressRedemptionAmount { get; private set; }
    public string AssumptionsJson { get; private set; } = "{}";
    public StressScenarioStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static RedemptionStressScenario Create(Guid? schemeId, string name, decimal stressRedemptionAmount, string assumptionsJson, string createdByUserId, DateTime createdAtUtc)
    {
        return new RedemptionStressScenario(schemeId, name, stressRedemptionAmount, assumptionsJson, createdByUserId, createdAtUtc);
    }
}

public sealed class RedemptionStressTestRun : AuditableAggregateRoot
{
    private RedemptionStressTestRun() { }

    private RedemptionStressTestRun(Guid scenarioId, Guid? schemeId, BusinessDate businessDate, string formulaVersion, decimal availableLiquidAssets, decimal stressRedemptionAmount, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        ScenarioId = scenarioId;
        SchemeId = schemeId;
        BusinessDate = businessDate;
        FormulaVersion = ComplianceRiskValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        AvailableLiquidAssets = ComplianceRiskValidation.NonNegative(availableLiquidAssets, nameof(availableLiquidAssets));
        StressRedemptionAmount = ComplianceRiskValidation.Positive(stressRedemptionAmount, nameof(stressRedemptionAmount));
        StressCoverage = ComplianceRiskFormulaEngine.StressCoverage(availableLiquidAssets, stressRedemptionAmount);
        SourceDataJson = ComplianceRiskValidation.Required(sourceDataJson, nameof(sourceDataJson), 12000);
        RunByUserId = ComplianceRiskValidation.Required(runByUserId, nameof(runByUserId), 200);
        RunAtUtc = ComplianceRiskValidation.EnsureUtc(runAtUtc, nameof(runAtUtc));
        MarkCreated(RunByUserId, RunAtUtc);
    }

    public Guid ScenarioId { get; private set; }
    public Guid? SchemeId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal AvailableLiquidAssets { get; private set; }
    public decimal StressRedemptionAmount { get; private set; }
    public decimal StressCoverage { get; private set; }
    public string SourceDataJson { get; private set; } = "{}";
    public string RunByUserId { get; private set; } = string.Empty;
    public DateTime RunAtUtc { get; private set; }

    public static RedemptionStressTestRun Create(Guid scenarioId, Guid? schemeId, BusinessDate businessDate, string formulaVersion, decimal availableLiquidAssets, decimal stressRedemptionAmount, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        return new RedemptionStressTestRun(scenarioId, schemeId, businessDate, formulaVersion, availableLiquidAssets, stressRedemptionAmount, sourceDataJson, runByUserId, runAtUtc);
    }
}

public sealed class LiquidationTimeAnalysisRun : AuditableAggregateRoot
{
    private LiquidationTimeAnalysisRun() { }

    private LiquidationTimeAnalysisRun(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal weightedAverageMaturityDays, string assumptionsJson, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        BusinessDate = businessDate;
        FormulaVersion = ComplianceRiskValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        WeightedAverageMaturityDays = ComplianceRiskValidation.NonNegative(weightedAverageMaturityDays, nameof(weightedAverageMaturityDays));
        AssumptionsJson = ComplianceRiskValidation.Required(assumptionsJson, nameof(assumptionsJson), 12000);
        SourceDataJson = ComplianceRiskValidation.Required(sourceDataJson, nameof(sourceDataJson), 12000);
        RunByUserId = ComplianceRiskValidation.Required(runByUserId, nameof(runByUserId), 200);
        RunAtUtc = ComplianceRiskValidation.EnsureUtc(runAtUtc, nameof(runAtUtc));
        MarkCreated(RunByUserId, RunAtUtc);
    }

    public Guid? SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal WeightedAverageMaturityDays { get; private set; }
    public string AssumptionsJson { get; private set; } = "{}";
    public string SourceDataJson { get; private set; } = "{}";
    public string RunByUserId { get; private set; } = string.Empty;
    public DateTime RunAtUtc { get; private set; }

    public static LiquidationTimeAnalysisRun Create(Guid? schemeId, Guid? schemeClassId, BusinessDate businessDate, string formulaVersion, decimal weightedAverageMaturityDays, string assumptionsJson, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        return new LiquidationTimeAnalysisRun(schemeId, schemeClassId, businessDate, formulaVersion, weightedAverageMaturityDays, assumptionsJson, sourceDataJson, runByUserId, runAtUtc);
    }
}

public sealed class RelatedPartyExposure : Entity
{
    private RelatedPartyExposure() { }

    private RelatedPartyExposure(Guid limitCheckRunId, string relatedPartyName, decimal exposureAmount, decimal exposurePercentOfAum, string sourceReference)
    {
        LimitCheckRunId = limitCheckRunId;
        RelatedPartyName = ComplianceRiskValidation.Required(relatedPartyName, nameof(relatedPartyName), 200);
        ExposureAmount = ComplianceRiskValidation.NonNegative(exposureAmount, nameof(exposureAmount));
        ExposurePercentOfAum = ComplianceRiskValidation.NonNegative(exposurePercentOfAum, nameof(exposurePercentOfAum));
        SourceReference = ComplianceRiskValidation.Required(sourceReference, nameof(sourceReference), 200);
    }

    public Guid LimitCheckRunId { get; private set; }
    public string RelatedPartyName { get; private set; } = string.Empty;
    public decimal ExposureAmount { get; private set; }
    public decimal ExposurePercentOfAum { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;

    public static RelatedPartyExposure Create(Guid limitCheckRunId, string relatedPartyName, decimal exposureAmount, decimal aum, string sourceReference)
    {
        var percent = aum > 0m ? ComplianceRiskFormulaEngine.Round(exposureAmount / aum * 100m) : 0m;
        return new RelatedPartyExposure(limitCheckRunId, relatedPartyName, exposureAmount, percent, sourceReference);
    }
}

public sealed class CounterpartyLimitUsage : Entity
{
    private CounterpartyLimitUsage() { }

    private CounterpartyLimitUsage(Guid limitCheckRunId, string counterpartyName, decimal exposureAmount, decimal limitAmount, decimal usagePercent, string sourceReference)
    {
        LimitCheckRunId = limitCheckRunId;
        CounterpartyName = ComplianceRiskValidation.Required(counterpartyName, nameof(counterpartyName), 200);
        ExposureAmount = ComplianceRiskValidation.NonNegative(exposureAmount, nameof(exposureAmount));
        LimitAmount = ComplianceRiskValidation.NonNegative(limitAmount, nameof(limitAmount));
        UsagePercent = ComplianceRiskValidation.NonNegative(usagePercent, nameof(usagePercent));
        SourceReference = ComplianceRiskValidation.Required(sourceReference, nameof(sourceReference), 200);
    }

    public Guid LimitCheckRunId { get; private set; }
    public string CounterpartyName { get; private set; } = string.Empty;
    public decimal ExposureAmount { get; private set; }
    public decimal LimitAmount { get; private set; }
    public decimal UsagePercent { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;

    public static CounterpartyLimitUsage Create(Guid limitCheckRunId, string counterpartyName, decimal exposureAmount, decimal limitAmount, string sourceReference)
    {
        var usagePercent = limitAmount > 0m ? ComplianceRiskFormulaEngine.Round(exposureAmount / limitAmount * 100m) : 0m;
        return new CounterpartyLimitUsage(limitCheckRunId, counterpartyName, exposureAmount, limitAmount, usagePercent, sourceReference);
    }
}

public sealed class RiskDashboardSnapshot : AuditableAggregateRoot
{
    private RiskDashboardSnapshot() { }

    private RiskDashboardSnapshot(BusinessDate businessDate, decimal assetsUnderManagement, decimal netFlow, decimal averageNav, decimal expenseToAumRatio, decimal latestLiquidityCoverageRatio, decimal latestStressCoverage, int openBreaches, int criticalBreaches, decimal relatedPartyExposurePercent, string generatedByUserId, DateTime generatedAtUtc)
    {
        BusinessDate = businessDate;
        AssetsUnderManagement = ComplianceRiskValidation.NonNegative(assetsUnderManagement, nameof(assetsUnderManagement));
        NetFlow = netFlow;
        AverageNav = ComplianceRiskValidation.NonNegative(averageNav, nameof(averageNav));
        ExpenseToAumRatio = ComplianceRiskValidation.NonNegative(expenseToAumRatio, nameof(expenseToAumRatio));
        LatestLiquidityCoverageRatio = ComplianceRiskValidation.NonNegative(latestLiquidityCoverageRatio, nameof(latestLiquidityCoverageRatio));
        LatestStressCoverage = ComplianceRiskValidation.NonNegative(latestStressCoverage, nameof(latestStressCoverage));
        OpenBreaches = ComplianceRiskValidation.NonNegative(openBreaches, nameof(openBreaches));
        CriticalBreaches = ComplianceRiskValidation.NonNegative(criticalBreaches, nameof(criticalBreaches));
        RelatedPartyExposurePercent = ComplianceRiskValidation.NonNegative(relatedPartyExposurePercent, nameof(relatedPartyExposurePercent));
        GeneratedByUserId = ComplianceRiskValidation.Required(generatedByUserId, nameof(generatedByUserId), 200);
        GeneratedAtUtc = ComplianceRiskValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        MarkCreated(GeneratedByUserId, GeneratedAtUtc);
    }

    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal AssetsUnderManagement { get; private set; }
    public decimal NetFlow { get; private set; }
    public decimal AverageNav { get; private set; }
    public decimal ExpenseToAumRatio { get; private set; }
    public decimal LatestLiquidityCoverageRatio { get; private set; }
    public decimal LatestStressCoverage { get; private set; }
    public int OpenBreaches { get; private set; }
    public int CriticalBreaches { get; private set; }
    public decimal RelatedPartyExposurePercent { get; private set; }
    public string GeneratedByUserId { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }

    public static RiskDashboardSnapshot Create(BusinessDate businessDate, decimal assetsUnderManagement, decimal netFlow, decimal averageNav, decimal expenseToAumRatio, decimal latestLiquidityCoverageRatio, decimal latestStressCoverage, int openBreaches, int criticalBreaches, decimal relatedPartyExposurePercent, string generatedByUserId, DateTime generatedAtUtc)
    {
        return new RiskDashboardSnapshot(businessDate, assetsUnderManagement, netFlow, averageNav, expenseToAumRatio, latestLiquidityCoverageRatio, latestStressCoverage, openBreaches, criticalBreaches, relatedPartyExposurePercent, generatedByUserId, generatedAtUtc);
    }
}
