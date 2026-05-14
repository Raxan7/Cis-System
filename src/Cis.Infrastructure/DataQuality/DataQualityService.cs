using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.DataQuality;
using Cis.Domain.Accounting;
using Cis.Domain.Audit;
using Cis.Domain.Cash;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.DataQuality;
using Cis.Domain.Dealing;
using Cis.Domain.Investors;
using Cis.Domain.NAV;
using Cis.Domain.Reports;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.DataQuality;

internal sealed class DataQualityService : IDataQualityService
{
    private const string ModuleName = "DataQuality";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DataQualityService(CisDbContext dbContext, IAuditWriter auditWriter, ICurrentUserContext currentUserContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DataQualityRuleDto> CreateRuleAsync(CreateDataQualityRuleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var code = ParseEnum<DataQualityRuleCode>(request.RuleCode, nameof(request.RuleCode));
        var severity = ParseEnum<DataQualitySeverity>(request.Severity, nameof(request.Severity));
        if (await _dbContext.DataQualityRules.AnyAsync(rule => rule.Code == code, cancellationToken))
        {
            throw new ConflictException("A data quality rule with the same code already exists.");
        }

        var rule = DataQualityRule.Create(code, request.Name, request.Description, severity, request.ThresholdDays, actor, _dateTimeProvider.UtcNow);
        _dbContext.DataQualityRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("DataQualityRuleCreated", "DataQualityRule", rule.Id, AuditEventType.Created, null, ToJson(ToDto(rule)), "Data quality rule created.", cancellationToken);
        return ToDto(rule);
    }

    public async Task<DataQualityCheckRunDto> CreateCheckRunAsync(CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = DataQualityCheckRun.Start($"DQ-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40], actor, now);
        _dbContext.DataQualityCheckRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var rules = await _dbContext.DataQualityRules.AsNoTracking().Where(rule => rule.Status == DataQualityRuleStatus.Active).ToListAsync(cancellationToken);
        var exceptions = new List<DataQualityException>();
        foreach (var rule in rules)
        {
            exceptions.AddRange(await EvaluateRuleAsync(rule, run.Id, now, cancellationToken));
        }

        _dbContext.DataQualityExceptions.AddRange(exceptions);
        _dbContext.ExceptionQueue.AddRange(exceptions.Select(exception => ExceptionQueue.Create(exception.Id, exception.Severity, now)));
        run.Complete(rules.Count, exceptions.Count, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await PersistDashboardSnapshotAsync(now, cancellationToken);
        await AuditAsync("DataQualityCheckRunCompleted", "DataQualityCheckRun", run.Id, AuditEventType.Created, null, ToJson(ToDto(run)), $"Generated {exceptions.Count} data quality exceptions.", cancellationToken);
        return ToDto(run);
    }

    public async Task<IReadOnlyCollection<DataQualityExceptionDto>> GetExceptionsAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        return await _dbContext.DataQualityExceptions.AsNoTracking()
            .OrderByDescending(exception => exception.DetectedAtUtc)
            .Select(exception => ToDto(exception, now))
            .ToListAsync(cancellationToken);
    }

    public async Task<DataQualityExceptionDto> AssignExceptionAsync(Guid id, AssignDataQualityExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var exception = await LoadExceptionAsync(id, cancellationToken);
        var queue = await _dbContext.ExceptionQueue.SingleAsync(item => item.DataQualityExceptionId == id, cancellationToken);
        var before = ToJson(ToDto(exception, _dateTimeProvider.UtcNow));
        exception.Assign(request.OwnerUserId, _dateTimeProvider.UtcNow);
        queue.MarkAssigned();
        _dbContext.ExceptionAssignments.Add(ExceptionAssignment.Create(exception.Id, request.OwnerUserId, actor, _dateTimeProvider.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("DataQualityExceptionAssigned", "DataQualityException", exception.Id, AuditEventType.Updated, before, ToJson(ToDto(exception, _dateTimeProvider.UtcNow)), $"Assigned to {request.OwnerUserId}.", cancellationToken);
        return ToDto(exception, _dateTimeProvider.UtcNow);
    }

    public async Task<DataQualityExceptionDto> ResolveExceptionAsync(Guid id, ResolveDataQualityExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var exception = await LoadExceptionAsync(id, cancellationToken);
        var queue = await _dbContext.ExceptionQueue.SingleAsync(item => item.DataQualityExceptionId == id, cancellationToken);
        var before = ToJson(ToDto(exception, _dateTimeProvider.UtcNow));
        var now = _dateTimeProvider.UtcNow;
        exception.Resolve(actor, now, request.EvidenceReference, request.Comment);
        queue.MarkResolved(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await PersistDashboardSnapshotAsync(now, cancellationToken);
        await AuditAsync("DataQualityExceptionResolved", "DataQualityException", exception.Id, AuditEventType.Updated, before, ToJson(ToDto(exception, now)), "Data quality exception resolved.", cancellationToken);
        return ToDto(exception, now);
    }

    public async Task<DataQualityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await PersistDashboardSnapshotAsync(_dateTimeProvider.UtcNow, cancellationToken);
        return new DataQualityDashboardDto(snapshot.OpenExceptions, snapshot.AssignedExceptions, snapshot.OverdueExceptions, snapshot.ResolvedExceptions, snapshot.GeneratedAtUtc);
    }

    private async Task<IReadOnlyCollection<DataQualityException>> EvaluateRuleAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        return rule.Code switch
        {
            DataQualityRuleCode.MissingRequiredInvestorFields => await MissingRequiredInvestorFieldsAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.ExpiredKycDocuments => await ExpiredKycDocumentsAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.DuplicateInvestorIdentifiers => await DuplicateInvestorIdentifiersAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.UnmatchedBankReceipts => await UnmatchedBankReceiptsAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.NegativeHoldings => await NegativeHoldingsAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.UnapprovedNavUsedForDealing => await UnapprovedNavUsedForDealingAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.StalePriceInputs => await StalePriceInputsAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.UnbalancedJournal => await UnbalancedJournalAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.UnresolvedReconciliationBreakOlderThanThreshold => await AgedReconciliationBreaksAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.ReportPublicationWithoutApproval => await ReportPublicationWithoutApprovalAsync(rule, runId, now, cancellationToken),
            DataQualityRuleCode.ConfigurationEffectiveDateOverlap => await ConfigurationEffectiveDateOverlapAsync(rule, runId, now, cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyCollection<DataQualityException>> MissingRequiredInvestorFieldsAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var investors = await _dbContext.Investors
            .AsNoTracking()
            .Include(investor => investor.IndividualProfiles)
            .Include(investor => investor.CorporateProfiles)
            .Include(investor => investor.JointProfiles)
            .Include(investor => investor.GroupProfiles)
            .ToListAsync(cancellationToken);
        return investors
            .Where(investor => string.IsNullOrWhiteSpace(investor.DisplayName)
                || string.IsNullOrWhiteSpace(investor.Email)
                || string.IsNullOrWhiteSpace(investor.PhoneNumber)
                || investor.InvestorType == InvestorType.Individual && investor.IndividualProfiles.Count == 0
                || investor.InvestorType == InvestorType.Corporate && investor.CorporateProfiles.Count == 0
                || investor.InvestorType == InvestorType.Joint && investor.JointProfiles.Count == 0
                || investor.InvestorType == InvestorType.Group && investor.GroupProfiles.Count == 0)
            .Select(investor => CreateException(rule, runId, "Investor", investor.Id.ToString(), $"Investor {investor.InvestorNumber} is missing required profile or contact data.", now))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> ExpiredKycDocumentsAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(now);
        var documents = (await _dbContext.KycDocuments.AsNoTracking().ToListAsync(cancellationToken))
            .Where(document => document.Status == KycDocumentStatus.Expired || document.ExpiryDate is not null && document.ExpiryDate.Value < asOf)
            .ToList();
        return documents.Select(document => CreateException(rule, runId, "KycDocument", document.Id.ToString(), $"KYC document {document.DocumentType} is expired.", now)).ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> DuplicateInvestorIdentifiersAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var duplicates = await _dbContext.DuplicateDetectionResults.AsNoTracking()
            .Where(result => result.Status == DuplicateDetectionStatus.Warning)
            .ToListAsync(cancellationToken);
        return duplicates.Select(result => CreateException(rule, runId, "DuplicateDetectionResult", result.Id.ToString(), $"Duplicate {result.MatchType} detected against investor {result.MatchedInvestorNumber}.", now)).ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> UnmatchedBankReceiptsAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var lines = await _dbContext.BankStatementLines.AsNoTracking()
            .Where(line => line.Direction == BankStatementLineDirection.Credit && line.MatchStatus != BankStatementMatchStatus.Matched)
            .ToListAsync(cancellationToken);
        var suspense = await _dbContext.SuspenseItems.AsNoTracking()
            .Where(item => item.Status == SuspenseItemStatus.Open)
            .ToListAsync(cancellationToken);

        return lines.Select(line => CreateException(rule, runId, "BankStatementLine", line.Id.ToString(), $"Bank receipt {line.Reference} is not fully matched.", now))
            .Concat(suspense.Select(item => CreateException(rule, runId, "SuspenseItem", item.Id.ToString(), $"Suspense receipt {item.Reference} remains unresolved.", now)))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> NegativeHoldingsAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var holdings = await _dbContext.UnitHoldings.AsNoTracking()
            .Where(holding => holding.Units < 0m || holding.LienedUnits < 0m || holding.Units - holding.LienedUnits < 0m)
            .ToListAsync(cancellationToken);
        return holdings.Select(holding => CreateException(rule, runId, "UnitHolding", holding.Id.ToString(), "Unit holding has negative units or negative redeemable units.", now)).ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> UnapprovedNavUsedForDealingAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var navKeys = (await _dbContext.NavPublications.AsNoTracking().ToListAsync(cancellationToken))
            .Select(nav => (nav.SchemeId, nav.SchemeClassId, Date: nav.ValuationDate.Value))
            .ToHashSet();
        var instructions = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Include(instruction => instruction.SubscriptionInstructions)
            .Include(instruction => instruction.RedemptionInstructions)
            .ToListAsync(cancellationToken);

        var subscriptionRows = instructions
            .Where(instruction => instruction.SubscriptionInstructions.Any(subscription =>
                !subscription.ApprovedNavAvailable
                || subscription.ApprovedNavDate is null
                || !navKeys.Contains((instruction.SchemeId, instruction.SchemeClassId, subscription.ApprovedNavDate.Value))))
            .Select(instruction => new { instruction.Id, instruction.InstructionNumber });
        var redemptionRows = instructions
            .Where(instruction => instruction.RedemptionInstructions.Any(redemption =>
                !redemption.ApprovedNavAvailable
                || !navKeys.Contains((instruction.SchemeId, instruction.SchemeClassId, redemption.ApprovedNavDate.Value))))
            .Select(instruction => new { instruction.Id, instruction.InstructionNumber });

        return subscriptionRows.Concat(redemptionRows)
            .Select(row => CreateException(rule, runId, "DealingInstruction", row.Id.ToString(), $"Dealing instruction {row.InstructionNumber} references missing or unpublished NAV.", now))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> StalePriceInputsAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var stale = await _dbContext.StalePriceExceptions.AsNoTracking().Where(exception => exception.Status == PriceExceptionStatus.Open).ToListAsync(cancellationToken);
        return stale.Select(exception => CreateException(rule, runId, "StalePriceException", exception.Id.ToString(), exception.Message, now)).ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> UnbalancedJournalAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var journals = await _dbContext.Journals.AsNoTracking().Include(journal => journal.Lines).ToListAsync(cancellationToken);
        return journals
            .Where(journal => journal.Lines.Count < 2 || journal.Lines.Sum(line => line.Debit) != journal.Lines.Sum(line => line.Credit))
            .Select(journal => CreateException(rule, runId, "Journal", journal.Id.ToString(), $"Journal {journal.JournalNumber} is unbalanced.", now))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> AgedReconciliationBreaksAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var threshold = now.AddDays(-rule.ThresholdDays);
        var holdingBreaks = await _dbContext.HoldingsReconciliationBreaks.AsNoTracking()
            .Where(item => item.Status != ReconciliationBreakStatus.Resolved && item.CreatedAtUtc < threshold)
            .ToListAsync(cancellationToken);
        var cashBreaks = await _dbContext.CashReconciliationBreaks.AsNoTracking()
            .Where(item => item.Status != ReconciliationBreakStatus.Resolved && item.CreatedAtUtc < threshold)
            .ToListAsync(cancellationToken);

        return holdingBreaks.Select(item => CreateException(rule, runId, "HoldingsReconciliationBreak", item.Id.ToString(), $"Holdings reconciliation break {item.InstrumentCode} is older than {rule.ThresholdDays} days.", now))
            .Concat(cashBreaks.Select(item => CreateException(rule, runId, "CashReconciliationBreak", item.Id.ToString(), $"Cash reconciliation break {item.AccountNumber} is older than {rule.ThresholdDays} days.", now)))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> ReportPublicationWithoutApprovalAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var reportRuns = await _dbContext.ReportRuns.AsNoTracking()
            .Include(report => report.Approvals)
            .Where(report => report.Status == ReportRunStatus.Published && (report.ApprovedByUserId == null || !report.Approvals.Any(approval => approval.Decision == ReportApprovalDecision.Approved)))
            .ToListAsync(cancellationToken);
        return reportRuns.Select(report => CreateException(rule, runId, "ReportRun", report.Id.ToString(), $"Report run {report.ReportCode} is published without approval evidence.", now)).ToArray();
    }

    private async Task<IReadOnlyCollection<DataQualityException>> ConfigurationEffectiveDateOverlapAsync(DataQualityRule rule, Guid runId, DateTime now, CancellationToken cancellationToken)
    {
        var fees = (await _dbContext.FeeSchedules.AsNoTracking().Include(fee => fee.Rules).ToListAsync(cancellationToken)).Where(fee => !fee.IsTiered).ToList();
        var exceptions = new List<DataQualityException>();
        foreach (var group in fees.GroupBy(fee => new { fee.SchemeId, fee.SchemeClassId, FeeType = fee.FeeType.ToUpperInvariant() }))
        {
            var ordered = group.OrderBy(fee => fee.EffectiveFrom.Value).ToArray();
            for (var i = 0; i < ordered.Length; i++)
            {
                for (var j = i + 1; j < ordered.Length; j++)
                {
                    if (ordered[i].EffectiveDatesOverlap(ordered[j]))
                    {
                        exceptions.Add(CreateException(rule, runId, "FeeSchedule", ordered[j].Id.ToString(), $"Fee schedule {ordered[j].FeeType} has overlapping effective dates.", now));
                    }
                }
            }
        }

        return exceptions;
    }

    private DataQualityException CreateException(DataQualityRule rule, Guid runId, string entityType, string entityId, string message, DateTime now)
    {
        return DataQualityException.Create(runId, rule.Id, rule.Code, rule.Severity, entityType, entityId, message, now, now.AddDays(rule.ThresholdDays));
    }

    private async Task<DataQualityDashboardSnapshot> PersistDashboardSnapshotAsync(DateTime now, CancellationToken cancellationToken)
    {
        var exceptions = await _dbContext.DataQualityExceptions.ToListAsync(cancellationToken);
        var snapshot = DataQualityDashboardSnapshot.Create(
            exceptions.Count(exception => exception.Status == DataQualityExceptionStatus.Open),
            exceptions.Count(exception => exception.Status == DataQualityExceptionStatus.Assigned),
            exceptions.Count(exception => exception.Status != DataQualityExceptionStatus.Resolved && exception.DueAtUtc < now),
            exceptions.Count(exception => exception.Status == DataQualityExceptionStatus.Resolved),
            now);
        _dbContext.DataQualityDashboardSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    private async Task<DataQualityException> LoadExceptionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.DataQualityExceptions.SingleOrDefaultAsync(exception => exception.Id == id, cancellationToken)
            ?? throw new NotFoundException("Data quality exception was not found.");
    }

    private static DataQualityRuleDto ToDto(DataQualityRule rule)
    {
        return new DataQualityRuleDto(rule.Id, rule.Code.ToString(), rule.Name, rule.Description, rule.Severity.ToString(), rule.ThresholdDays, rule.Status.ToString());
    }

    private static DataQualityCheckRunDto ToDto(DataQualityCheckRun run)
    {
        return new DataQualityCheckRunDto(run.Id, run.RunNumber, run.Status.ToString(), run.RequestedByUserId, run.StartedAtUtc, run.CompletedAtUtc, run.RulesEvaluated, run.ExceptionsGenerated);
    }

    private static DataQualityExceptionDto ToDto(DataQualityException exception, DateTime now)
    {
        return new DataQualityExceptionDto(exception.Id, exception.CheckRunId, exception.RuleCode.ToString(), exception.Severity.ToString(), exception.EntityType, exception.EntityId, exception.Message, exception.Status.ToString(), exception.DetectedAtUtc, exception.DueAtUtc, exception.Status != DataQualityExceptionStatus.Resolved && exception.DueAtUtc < now, exception.OwnerUserId, exception.AssignedAtUtc, exception.ResolvedByUserId, exception.ResolvedAtUtc, exception.ResolutionEvidenceReference, exception.ResolutionComment);
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed)
            ? parsed
            : throw new ValidationException(new Dictionary<string, string[]> { [fieldName] = [$"{fieldName} has an invalid value."] });
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user is required.");
    }

    private Task AuditAsync(string action, string entityName, Guid entityId, AuditEventType eventType, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        var role = _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles);
        return _auditWriter.WriteAsync(new AuditLogEntry(ModuleName, action, entityName, entityId.ToString(), eventType, _currentUserContext.UserId, _currentUserContext.DisplayName, role, _currentUserContext.CorrelationId, _currentUserContext.IpAddress, _currentUserContext.UserAgent, reason, null, beforeJson, afterJson, reason), cancellationToken);
    }

    private static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
