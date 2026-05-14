using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.ComplianceRisk;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.ComplianceRisk;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.ComplianceRisk;

internal sealed class ComplianceRiskService : IComplianceRiskService
{
    private const string ModuleName = "ComplianceRisk";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ComplianceRiskService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LimitCheckRunDto> CreateLimitCheckRunAsync(CreateLimitCheckRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureSchemeReferencesAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        if (request.ActiveClassNavs.Count == 0)
        {
            throw Validation("activeClassNavs", "At least one active scheme/class NAV source is required.");
        }

        if (request.LimitRules.Count == 0)
        {
            throw Validation("limitRules", "At least one limit rule is required.");
        }

        var aum = ComplianceRiskFormulaEngine.AssetsUnderManagement(request.ActiveClassNavs.Select(nav => nav.ClosingNav));
        var netFlow = ComplianceRiskFormulaEngine.NetFlow(request.TotalSubscriptions, request.TransferIn, request.TotalRedemptions, request.TransferOut);
        var averageNav = ComplianceRiskFormulaEngine.AverageNav(request.DailyNavs);
        var expenseToAum = ComplianceRiskFormulaEngine.ExpenseToAumRatio(request.TotalExpensesForPeriod, averageNav);
        var annualizedYield = ComplianceRiskFormulaEngine.AnnualizedYield(request.PeriodNetInvestmentIncome, averageNav, request.AnnualizationFactor);
        var sourceDataJson = Snapshot(new
        {
            request.ActiveClassNavs,
            request.TotalSubscriptions,
            request.TransferIn,
            request.TotalRedemptions,
            request.TransferOut,
            request.DailyNavs,
            request.TotalExpensesForPeriod,
            request.PeriodNetInvestmentIncome,
            request.AnnualizationFactor,
            request.LimitRules,
            request.RelatedPartyExposures,
            request.CounterpartyUsages
        });

        var run = CreateValidated(() => LimitCheckRun.Create(
            request.SchemeId,
            request.SchemeClassId,
            BusinessDate.From(request.BusinessDate),
            request.FormulaVersion,
            aum,
            netFlow,
            averageNav,
            expenseToAum,
            annualizedYield,
            sourceDataJson,
            actor,
            now));

        foreach (var exposure in request.RelatedPartyExposures)
        {
            run.AddRelatedPartyExposure(CreateValidated(() => RelatedPartyExposure.Create(run.Id, exposure.RelatedPartyName, exposure.ExposureAmount, aum, exposure.SourceReference)));
        }

        foreach (var usage in request.CounterpartyUsages)
        {
            run.AddCounterpartyLimitUsage(CreateValidated(() => CounterpartyLimitUsage.Create(run.Id, usage.CounterpartyName, usage.ExposureAmount, usage.LimitAmount, usage.SourceReference)));
        }

        foreach (var rule in request.LimitRules)
        {
            var scope = ParseEnum<LimitRuleScope>(rule.Scope, "scope");
            var limitType = ParseEnum<LimitType>(rule.LimitType, "limitType");
            Guid? statutoryLimitId = null;
            Guid? internalPolicyLimitId = null;
            if (scope == LimitRuleScope.Statutory)
            {
                var statutoryLimit = StatutoryLimit.Create(request.SchemeId, request.SchemeClassId, limitType, rule.Reference, rule.LimitValue, BusinessDate.From(request.BusinessDate), actor, now);
                statutoryLimitId = statutoryLimit.Id;
                _dbContext.StatutoryLimits.Add(statutoryLimit);
            }
            else
            {
                var internalLimit = InternalPolicyLimit.Create(request.SchemeId, request.SchemeClassId, limitType, rule.Reference, rule.LimitValue, BusinessDate.From(request.BusinessDate), actor, now);
                internalPolicyLimitId = internalLimit.Id;
                _dbContext.InternalPolicyLimits.Add(internalLimit);
            }

            if (rule.ActualValue > rule.LimitValue)
            {
                var severity = DetermineSeverity(rule.ActualValue, rule.LimitValue);
                var breach = LimitBreach.Create(
                    run.Id,
                    statutoryLimitId,
                    internalPolicyLimitId,
                    scope,
                    limitType,
                    rule.Reference,
                    rule.LimitValue,
                    rule.ActualValue,
                    severity,
                    rule.Description,
                    actor,
                    now);
                run.AddBreach(breach);
            }
        }

        _dbContext.LimitCheckRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);
        await CreateDashboardSnapshotAsync(run.BusinessDate, actor, now, cancellationToken);

        var dto = MapLimitCheckRun(await LoadLimitRunAsync(run.Id, asTracking: false, cancellationToken));
        await WriteAuditAsync(AuditEventType.Created, "LimitCheckRunCreated", "LimitCheckRun", run.Id.ToString(), null, Snapshot(dto), "Daily limit check run completed.", cancellationToken);
        foreach (var breach in dto.Breaches)
        {
            await WriteAuditAsync(AuditEventType.Created, "LimitBreachLogged", "LimitBreach", breach.Id.ToString(), null, Snapshot(breach), "Limit breach automatically logged.", cancellationToken);
        }

        return dto;
    }

    public async Task<IReadOnlyCollection<LimitBreachDto>> GetBreachesAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LimitBreaches.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = ParseEnum<BreachStatus>(status, "status");
            query = query.Where(breach => breach.Status == parsed);
        }

        var breaches = await query.OrderByDescending(breach => breach.CreatedAtUtc).ToListAsync(cancellationToken);
        return breaches.Select(MapBreach).ToArray();
    }

    public async Task<LimitBreachDto> AssignBreachAsync(Guid id, AssignBreachRequest request, CancellationToken cancellationToken = default)
    {
        var breach = await LoadBreachAsync(id, cancellationToken);
        var before = Snapshot(MapBreach(breach));
        breach.Assign(request.OwnerUserId, request.Comment);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapBreach(breach);
        await WriteAuditAsync(AuditEventType.Updated, "LimitBreachAssigned", "LimitBreach", breach.Id.ToString(), before, Snapshot(dto), request.Comment, cancellationToken);
        return dto;
    }

    public async Task<LimitBreachDto> RemediateBreachAsync(Guid id, RemediateBreachRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var breach = await LoadBreachAsync(id, cancellationToken);
        var before = Snapshot(MapBreach(breach));

        try
        {
            breach.Remediate(request.RemediationPlan, request.EvidenceReference, actor, now);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("breach", exception.Message);
        }

        _dbContext.RemediationActions.Add(RemediationAction.Create(breach.Id, request.RemediationPlan, request.EvidenceReference, actor, now));
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapBreach(breach);
        await WriteAuditAsync(AuditEventType.Updated, "LimitBreachRemediationSubmitted", "LimitBreach", breach.Id.ToString(), before, Snapshot(dto), "Limit breach remediation submitted.", cancellationToken);
        return dto;
    }

    public async Task<LimitBreachDto> CloseBreachAsync(Guid id, CloseBreachRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var breach = await LoadBreachAsync(id, cancellationToken);
        var before = Snapshot(MapBreach(breach));

        try
        {
            breach.Close(actor, now, request.EvidenceReference);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("breach", exception.Message);
        }

        _dbContext.BreachExceptionRegisters.Add(BreachExceptionRegister.Create(breach.Id, "Breach remediation closure approved.", request.EvidenceReference, actor, now));
        await SaveHandlingValidationAsync(cancellationToken);
        await CreateDashboardSnapshotAsync(BusinessDate.From(DateOnly.FromDateTime(now)), actor, now, cancellationToken);
        var dto = MapBreach(breach);
        await WriteAuditAsync(AuditEventType.Approved, "LimitBreachClosed", "LimitBreach", breach.Id.ToString(), before, Snapshot(dto), "Limit breach closure approved with evidence.", cancellationToken);
        return dto;
    }

    public async Task<LiquidityCoverageRunDto> CreateLiquidityCoverageRunAsync(CreateLiquidityCoverageRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureSchemeReferencesAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var sourceJson = string.IsNullOrWhiteSpace(request.SourceDataJson)
            ? Snapshot(new { request.AvailableLiquidAssets, request.ProjectedShortTermRedemptions })
            : request.SourceDataJson!;
        EnsureJson(sourceJson, "sourceDataJson");

        var run = CreateValidated(() => LiquidityCoverageRun.Create(request.SchemeId, request.SchemeClassId, BusinessDate.From(request.BusinessDate), request.FormulaVersion, request.AvailableLiquidAssets, request.ProjectedShortTermRedemptions, sourceJson, actor, now));
        _dbContext.LiquidityCoverageRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);
        await CreateDashboardSnapshotAsync(run.BusinessDate, actor, now, cancellationToken);

        var dto = MapLiquidityCoverageRun(run);
        await WriteAuditAsync(AuditEventType.Created, "LiquidityCoverageRunCreated", "LiquidityCoverageRun", run.Id.ToString(), null, Snapshot(dto), "Liquidity coverage run created.", cancellationToken);
        return dto;
    }

    public async Task<RedemptionStressScenarioDto> CreateStressScenarioAsync(CreateStressScenarioRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureSchemeReferencesAsync(request.SchemeId, null, cancellationToken);
        EnsureJson(request.AssumptionsJson, "assumptionsJson");

        var scenario = CreateValidated(() => RedemptionStressScenario.Create(request.SchemeId, request.Name, request.StressRedemptionAmount, request.AssumptionsJson, actor, now));
        _dbContext.RedemptionStressScenarios.Add(scenario);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapStressScenario(scenario);
        await WriteAuditAsync(AuditEventType.Created, "RedemptionStressScenarioCreated", "RedemptionStressScenario", scenario.Id.ToString(), null, Snapshot(dto), "Redemption stress scenario created.", cancellationToken);
        return dto;
    }

    public async Task<RedemptionStressTestRunDto> CreateStressTestRunAsync(CreateStressTestRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var scenario = await _dbContext.RedemptionStressScenarios.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == request.ScenarioId, cancellationToken)
            ?? throw new NotFoundException("Redemption stress scenario was not found.");
        if (scenario.Status != StressScenarioStatus.Active)
        {
            throw Validation("scenarioId", "Only active stress scenarios can be run.");
        }

        var sourceJson = string.IsNullOrWhiteSpace(request.SourceDataJson)
            ? Snapshot(new { request.AvailableLiquidAssets, scenario.StressRedemptionAmount })
            : request.SourceDataJson!;
        EnsureJson(sourceJson, "sourceDataJson");
        var run = CreateValidated(() => RedemptionStressTestRun.Create(scenario.Id, scenario.SchemeId, BusinessDate.From(request.BusinessDate), request.FormulaVersion, request.AvailableLiquidAssets, scenario.StressRedemptionAmount, sourceJson, actor, now));
        _dbContext.RedemptionStressTestRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);
        await CreateDashboardSnapshotAsync(run.BusinessDate, actor, now, cancellationToken);

        var dto = MapStressTestRun(run);
        await WriteAuditAsync(AuditEventType.Created, "RedemptionStressTestRunCreated", "RedemptionStressTestRun", run.Id.ToString(), null, Snapshot(dto), "Redemption stress test run created.", cancellationToken);
        return dto;
    }

    public async Task<LiquidationTimeAnalysisRunDto> CreateLiquidationTimeAnalysisRunAsync(CreateLiquidationTimeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureSchemeReferencesAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        EnsureJson(request.AssumptionsJson, "assumptionsJson");
        var wam = ComplianceRiskFormulaEngine.WeightedAverageMaturity(request.Instruments.Select(input => new WeightedMaturityInput(input.InstrumentValue, input.DaysToMaturityOrReset)));
        var sourceJson = Snapshot(request.Instruments);

        var run = CreateValidated(() => LiquidationTimeAnalysisRun.Create(request.SchemeId, request.SchemeClassId, BusinessDate.From(request.BusinessDate), request.FormulaVersion, wam, request.AssumptionsJson, sourceJson, actor, now));
        _dbContext.LiquidationTimeAnalysisRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapLiquidationTimeAnalysisRun(run);
        await WriteAuditAsync(AuditEventType.Created, "LiquidationTimeAnalysisRunCreated", "LiquidationTimeAnalysisRun", run.Id.ToString(), null, Snapshot(dto), "Liquidation time analysis run created.", cancellationToken);
        return dto;
    }

    public async Task<RiskDashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.RiskDashboardSnapshots.AsNoTracking().OrderByDescending(candidate => candidate.GeneratedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (snapshot is null)
        {
            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            snapshot = await CreateDashboardSnapshotAsync(BusinessDate.From(DateOnly.FromDateTime(now)), actor, now, cancellationToken);
        }

        return MapDashboard(snapshot);
    }

    private async Task<RiskDashboardSnapshot> CreateDashboardSnapshotAsync(BusinessDate businessDate, string actor, DateTime now, CancellationToken cancellationToken)
    {
        var latestRun = await _dbContext.LimitCheckRuns.AsNoTracking().OrderByDescending(run => run.RunAtUtc).FirstOrDefaultAsync(cancellationToken);
        var latestLiquidity = await _dbContext.LiquidityCoverageRuns.AsNoTracking().OrderByDescending(run => run.RunAtUtc).FirstOrDefaultAsync(cancellationToken);
        var latestStress = await _dbContext.RedemptionStressTestRuns.AsNoTracking().OrderByDescending(run => run.RunAtUtc).FirstOrDefaultAsync(cancellationToken);
        var openBreaches = await _dbContext.LimitBreaches.AsNoTracking().CountAsync(breach => breach.Status != BreachStatus.Closed, cancellationToken);
        var criticalBreaches = await _dbContext.LimitBreaches.AsNoTracking().CountAsync(breach => breach.Status != BreachStatus.Closed && breach.Severity == BreachSeverity.Critical, cancellationToken);
        var relatedExposurePercent = latestRun is null
            ? 0m
            : await _dbContext.RelatedPartyExposures.AsNoTracking()
                .Where(exposure => exposure.LimitCheckRunId == latestRun.Id)
                .SumAsync(exposure => exposure.ExposurePercentOfAum, cancellationToken);

        var snapshot = RiskDashboardSnapshot.Create(
            businessDate,
            latestRun?.AssetsUnderManagement ?? 0m,
            latestRun?.NetFlow ?? 0m,
            latestRun?.AverageNav ?? 0m,
            latestRun?.ExpenseToAumRatio ?? 0m,
            latestLiquidity?.LiquidityCoverageRatio ?? 0m,
            latestStress?.StressCoverage ?? 0m,
            openBreaches,
            criticalBreaches,
            relatedExposurePercent,
            actor,
            now);
        _dbContext.RiskDashboardSnapshots.Add(snapshot);
        await SaveHandlingValidationAsync(cancellationToken);
        return snapshot;
    }

    private async Task EnsureSchemeReferencesAsync(Guid? schemeId, Guid? schemeClassId, CancellationToken cancellationToken)
    {
        if (schemeId.HasValue)
        {
            var schemeExists = await _dbContext.Schemes.AnyAsync(scheme => scheme.Id == schemeId.Value, cancellationToken);
            if (!schemeExists)
            {
                throw new NotFoundException("Scheme was not found.");
            }
        }

        if (schemeClassId.HasValue)
        {
            if (!schemeId.HasValue)
            {
                throw Validation("schemeId", "Scheme id is required when scheme class id is supplied.");
            }

            var classExists = await _dbContext.SchemeClasses.AnyAsync(schemeClass => schemeClass.Id == schemeClassId.Value && schemeClass.SchemeId == schemeId.Value, cancellationToken);
            if (!classExists)
            {
                throw new NotFoundException("Scheme class was not found.");
            }
        }
    }

    private async Task<LimitCheckRun> LoadLimitRunAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.LimitCheckRuns
            .Include(run => run.Breaches)
            .Include(run => run.RelatedPartyExposures)
            .Include(run => run.CounterpartyLimitUsages)
            .AsSplitQuery();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(run => run.Id == id, cancellationToken)
            ?? throw new NotFoundException("Limit check run was not found.");
    }

    private async Task<LimitBreach> LoadBreachAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.LimitBreaches.FirstOrDefaultAsync(breach => breach.Id == id, cancellationToken)
            ?? throw new NotFoundException("Limit breach was not found.");
    }

    private static BreachSeverity DetermineSeverity(decimal actualValue, decimal limitValue)
    {
        if (limitValue <= 0m)
        {
            return BreachSeverity.Critical;
        }

        var excessRatio = (actualValue - limitValue) / limitValue;
        return excessRatio switch
        {
            >= 0.50m => BreachSeverity.Critical,
            >= 0.25m => BreachSeverity.High,
            >= 0.10m => BreachSeverity.Medium,
            _ => BreachSeverity.Low
        };
    }

    private static LimitCheckRunDto MapLimitCheckRun(LimitCheckRun run)
    {
        return new LimitCheckRunDto(
            run.Id,
            run.SchemeId,
            run.SchemeClassId,
            run.BusinessDate.Value,
            run.RunNumber,
            run.FormulaVersion,
            run.AssetsUnderManagement,
            run.NetFlow,
            run.AverageNav,
            run.ExpenseToAumRatio,
            run.AnnualizedYield,
            run.Status.ToString(),
            run.RunByUserId,
            run.RunAtUtc,
            run.Breaches.OrderBy(breach => breach.Reference).Select(MapBreach).ToArray(),
            run.RelatedPartyExposures.Select(MapRelatedPartyExposure).ToArray(),
            run.CounterpartyLimitUsages.Select(MapCounterpartyUsage).ToArray());
    }

    private static LimitBreachDto MapBreach(LimitBreach breach)
    {
        return new LimitBreachDto(
            breach.Id,
            breach.LimitCheckRunId,
            breach.RuleScope.ToString(),
            breach.LimitType.ToString(),
            breach.Reference,
            breach.LimitValue,
            breach.ActualValue,
            breach.Severity.ToString(),
            breach.Description,
            breach.Status.ToString(),
            breach.OwnerUserId,
            breach.RemediationPlan,
            breach.ClosureEvidenceReference,
            breach.CreatedByUserId,
            breach.CreatedAtUtc,
            breach.RemediatedByUserId,
            breach.RemediatedAtUtc,
            breach.ClosedByUserId,
            breach.ClosedAtUtc);
    }

    private static RelatedPartyExposureDto MapRelatedPartyExposure(RelatedPartyExposure exposure)
    {
        return new RelatedPartyExposureDto(exposure.Id, exposure.LimitCheckRunId, exposure.RelatedPartyName, exposure.ExposureAmount, exposure.ExposurePercentOfAum, exposure.SourceReference);
    }

    private static CounterpartyLimitUsageDto MapCounterpartyUsage(CounterpartyLimitUsage usage)
    {
        return new CounterpartyLimitUsageDto(usage.Id, usage.LimitCheckRunId, usage.CounterpartyName, usage.ExposureAmount, usage.LimitAmount, usage.UsagePercent, usage.SourceReference);
    }

    private static LiquidityCoverageRunDto MapLiquidityCoverageRun(LiquidityCoverageRun run)
    {
        return new LiquidityCoverageRunDto(run.Id, run.SchemeId, run.SchemeClassId, run.BusinessDate.Value, run.FormulaVersion, run.AvailableLiquidAssets, run.ProjectedShortTermRedemptions, run.LiquidityCoverageRatio, run.RunByUserId, run.RunAtUtc);
    }

    private static RedemptionStressScenarioDto MapStressScenario(RedemptionStressScenario scenario)
    {
        return new RedemptionStressScenarioDto(scenario.Id, scenario.SchemeId, scenario.Name, scenario.StressRedemptionAmount, scenario.AssumptionsJson, scenario.Status.ToString());
    }

    private static RedemptionStressTestRunDto MapStressTestRun(RedemptionStressTestRun run)
    {
        return new RedemptionStressTestRunDto(run.Id, run.ScenarioId, run.SchemeId, run.BusinessDate.Value, run.FormulaVersion, run.AvailableLiquidAssets, run.StressRedemptionAmount, run.StressCoverage, run.RunByUserId, run.RunAtUtc);
    }

    private static LiquidationTimeAnalysisRunDto MapLiquidationTimeAnalysisRun(LiquidationTimeAnalysisRun run)
    {
        return new LiquidationTimeAnalysisRunDto(run.Id, run.SchemeId, run.SchemeClassId, run.BusinessDate.Value, run.FormulaVersion, run.WeightedAverageMaturityDays, run.AssumptionsJson, run.RunByUserId, run.RunAtUtc);
    }

    private static RiskDashboardSnapshotDto MapDashboard(RiskDashboardSnapshot snapshot)
    {
        return new RiskDashboardSnapshotDto(snapshot.Id, snapshot.BusinessDate.Value, snapshot.AssetsUnderManagement, snapshot.NetFlow, snapshot.AverageNav, snapshot.ExpenseToAumRatio, snapshot.LatestLiquidityCoverageRatio, snapshot.LatestStressCoverage, snapshot.OpenBreaches, snapshot.CriticalBreaches, snapshot.RelatedPartyExposurePercent, snapshot.GeneratedAtUtc);
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(ModuleName, action, entityName, entityId, EventType: eventType, Summary: reason, BeforeJson: beforeJson, AfterJson: afterJson, Reason: reason), cancellationToken);
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException("The compliance or risk change conflicts with an existing record.");
        }
    }

    private static T CreateValidated<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("complianceRisk", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "complianceRisk", exception.Message);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw Validation(fieldName, $"Unsupported {fieldName} value '{value}'.");
        }

        return parsed;
    }

    private static void EnsureJson(string json, string fieldName)
    {
        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw Validation(fieldName, $"{fieldName} must contain valid JSON.");
        }
    }

    private static ValidationException Validation(string fieldName, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private static string Snapshot<T>(T? value)
    {
        return value is null ? "{}" : JsonSerializer.Serialize(value);
    }
}
