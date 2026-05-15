using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts;
using Cis.Contracts.Schemes;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Schemes;

internal sealed class SchemeService : ISchemeService
{
    private const string ModuleName = "Schemes";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SchemeService(
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

    public async Task<SchemeDto> CreateAsync(CreateSchemeRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _dbContext.Schemes.AnyAsync(scheme => scheme.Code == normalizedCode, cancellationToken))
        {
            throw new ConflictException("A scheme with the same code already exists.");
        }

        var scheme = Scheme.Create(request.Code, request.Name, request.LegalType, request.BaseCurrency, actor, now);
        scheme.AddVersionHistory("Created", actor, now, null, null, Snapshot(scheme));
        _dbContext.Schemes.Add(scheme);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = await MapSchemeAsync(scheme.Id, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "SchemeCreated", dto, null, Snapshot(dto), "Scheme draft created.", cancellationToken);

        return dto;
    }

    public async Task<PagedResult<SchemeDto>> GetAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var query = ApplySchemeListQuery(_dbContext.Schemes.AsNoTracking(), pagination);
        var totalCount = await query.CountAsync(cancellationToken);
        var ids = await ApplySchemeListSorting(query, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(scheme => scheme.Id)
            .ToListAsync(cancellationToken);

        var schemes = new List<SchemeDto>();
        foreach (var id in ids)
        {
            schemes.Add(await MapSchemeAsync(id, cancellationToken));
        }

        return new PagedResult<SchemeDto>(schemes, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public Task<SchemeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return MapSchemeAsync(id, cancellationToken);
    }

    private static IQueryable<Scheme> ApplySchemeListQuery(IQueryable<Scheme> query, PaginationRequest pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination.Search))
        {
            return query;
        }

        var search = pagination.Search.Trim();
        return query.Where(scheme =>
            scheme.Code.Contains(search) ||
            scheme.Name.Contains(search) ||
            scheme.LegalType.Contains(search) ||
            scheme.BaseCurrency.Contains(search));
    }

    private static IQueryable<Scheme> ApplySchemeListSorting(IQueryable<Scheme> query, PaginationRequest pagination)
    {
        var descending = pagination.IsDescending;
        return (pagination.SortBy ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "NAME" => descending ? query.OrderByDescending(scheme => scheme.Name).ThenBy(scheme => scheme.Code) : query.OrderBy(scheme => scheme.Name).ThenBy(scheme => scheme.Code),
            "STATUS" => descending ? query.OrderByDescending(scheme => scheme.Status).ThenBy(scheme => scheme.Code) : query.OrderBy(scheme => scheme.Status).ThenBy(scheme => scheme.Code),
            "BASECURRENCY" => descending ? query.OrderByDescending(scheme => scheme.BaseCurrency).ThenBy(scheme => scheme.Code) : query.OrderBy(scheme => scheme.BaseCurrency).ThenBy(scheme => scheme.Code),
            "CREATEDATUTC" => descending ? query.OrderByDescending(scheme => scheme.Audit.CreatedAtUtc).ThenBy(scheme => scheme.Code) : query.OrderBy(scheme => scheme.Audit.CreatedAtUtc).ThenBy(scheme => scheme.Code),
            _ => descending ? query.OrderByDescending(scheme => scheme.Code) : query.OrderBy(scheme => scheme.Code)
        };
    }

    public Task<SchemeDto> UpdateAsync(Guid id, UpdateSchemeRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeAmended",
            AuditEventType.Updated,
            scheme =>
            {
                scheme.AmendDetails(
                    request.Name,
                    request.LegalType,
                    request.BaseCurrency,
                    request.EffectiveDate.HasValue ? BusinessDate.From(request.EffectiveDate.Value) : null);
            },
            "Scheme amended.",
            request.EffectiveDate.HasValue ? BusinessDate.From(request.EffectiveDate.Value) : null,
            cancellationToken);
    }

    public Task<SchemeDto> SubmitAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeSubmitted",
            AuditEventType.Submitted,
            scheme => scheme.Submit(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow),
            request.Comment ?? "Scheme submitted for checking.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> CheckAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeChecked",
            AuditEventType.Checked,
            scheme => scheme.Check(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow),
            request.Comment ?? "Scheme checked.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> ApproveAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeApproved",
            AuditEventType.Approved,
            scheme => scheme.Approve(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow),
            request.Comment ?? "Scheme approved and activated.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddClassAsync(Guid id, AddSchemeClassRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeClassAdded",
            AuditEventType.Updated,
            scheme =>
            {
                scheme.AddClass(
                    request.Code,
                    request.Name,
                    request.Currency,
                    ParseFrequency(request.ValuationFrequency, "valuationFrequency"),
                    ParseFrequency(request.DealingFrequency, "dealingFrequency"),
                    request.CutOffTime,
                    request.MinimumContribution,
                    request.MinimumBalance,
                    request.LockInDays,
                    request.NoticePeriodDays);
            },
            "Scheme class added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddFeeScheduleAsync(Guid id, AddFeeScheduleRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "FeeScheduleAdded",
            AuditEventType.Updated,
            scheme =>
            {
                var tierRules = (request.TierRules ?? [])
                    .Select(rule => (rule.FromAmount, rule.ToAmount, rule.Rate, rule.FixedAmount));
                scheme.AddFeeSchedule(
                    request.SchemeClassId,
                    request.FeeType,
                    request.CalculationBasis,
                    request.Rate,
                    request.FixedAmount,
                    BusinessDate.From(request.EffectiveFrom),
                    request.EffectiveTo.HasValue ? BusinessDate.From(request.EffectiveTo.Value) : null,
                    tierRules);
            },
            "Fee schedule added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddEligibilityRuleAsync(Guid id, AddEligibilityRuleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateJson(request.RuleExpressionJson, "ruleExpressionJson");
        return ChangeSchemeAsync(
            id,
            "SchemeEligibilityRuleAdded",
            AuditEventType.Updated,
            scheme => scheme.AddEligibilityRule(request.RuleType, request.Description, request.RuleExpressionJson),
            "Scheme eligibility rule added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddApprovedInstrumentAsync(Guid id, AddApprovedInstrumentRuleRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "ApprovedInstrumentRuleAdded",
            AuditEventType.Updated,
            scheme =>
            {
                scheme.AddApprovedInstrumentRule(
                    request.InstrumentType,
                    request.TenorLimitDays,
                    request.IssuerLimit,
                    request.CounterpartyLimit,
                    request.AssetClassLimit);
            },
            "Approved instrument rule added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddBankAccountAsync(Guid id, AddSchemeBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeBankAccountAdded",
            AuditEventType.Updated,
            scheme => scheme.AddBankAccount(request.BankName, request.AccountNumber, request.AccountName, request.Currency, request.SwiftCode),
            "Scheme bank account added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddCustodianMappingAsync(Guid id, AddSchemeCustodianMappingRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeCustodianMappingAdded",
            AuditEventType.Updated,
            scheme => scheme.AddCustodianMapping(request.CustodianName, request.CustodyAccountReference, request.SettlementAccountReference),
            "Scheme custodian mapping added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddConfigurationAsync(Guid id, AddSchemeConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeConfigurationAdded",
            AuditEventType.Updated,
            scheme => scheme.AddConfiguration(request.NavPricingBasis, request.IncomeRecognitionBasis),
            "Scheme configuration added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddRiskProfileAsync(Guid id, AddSchemeRiskProfileRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "SchemeRiskProfileAdded",
            AuditEventType.Updated,
            scheme => scheme.AddRiskProfile(request.RiskRating, request.MaxSingleIssuerExposure),
            "Scheme risk profile added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddLiquidityThresholdAsync(Guid id, AddLiquidityThresholdRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "LiquidityThresholdAdded",
            AuditEventType.Updated,
            scheme => scheme.AddLiquidityThreshold(request.MinimumLiquidAssetRatio, request.WarningThreshold, request.BreachThreshold),
            "Scheme liquidity threshold added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddDistributionRuleAsync(Guid id, AddDistributionRuleRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "DistributionRuleAdded",
            AuditEventType.Updated,
            scheme => scheme.AddDistributionRule(ParseFrequency(request.DistributionFrequency, "distributionFrequency"), request.ReinvestmentAllowed, request.PaymentDay),
            "Scheme distribution rule added.",
            null,
            cancellationToken);
    }

    public Task<SchemeDto> AddTemplateMappingAsync(Guid id, AddTemplateMappingRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeSchemeAsync(
            id,
            "TemplateMappingAdded",
            AuditEventType.Updated,
            scheme => scheme.AddTemplateMapping(request.TemplateType, request.TemplateCode),
            "Scheme template mapping added.",
            null,
            cancellationToken);
    }

    private async Task<SchemeDto> ChangeSchemeAsync(
        Guid id,
        string auditAction,
        AuditEventType eventType,
        Action<Scheme> change,
        string reason,
        BusinessDate? effectiveDate,
        CancellationToken cancellationToken)
    {
        var actor = CurrentUserIdOrThrow();
        var scheme = await GetSchemeAggregateAsync(id, cancellationToken);
        var existingChildIds = SchemeChildIds(scheme);
        var beforeJson = Snapshot(scheme);
        try
        {
            change(scheme);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["scheme"] = [exception.Message]
            });
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "scheme"] = [exception.Message]
            });
        }

        scheme.AddVersionHistory(auditAction, actor, _dateTimeProvider.UtcNow, effectiveDate ?? scheme.PendingEffectiveDate, beforeJson, Snapshot(scheme));
        MarkNewSchemeChildrenAsAdded(scheme, existingChildIds);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = await MapSchemeAsync(id, cancellationToken);
        await WriteAuditAsync(eventType, auditAction, dto, beforeJson, Snapshot(dto), reason, cancellationToken);

        return dto;
    }

    private async Task<Scheme> GetSchemeAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Schemes
            .Include(scheme => scheme.Classes)
            .Include(scheme => scheme.Configurations)
            .Include(scheme => scheme.FeeSchedules).ThenInclude(fee => fee.Rules)
            .Include(scheme => scheme.EligibilityRules)
            .Include(scheme => scheme.ApprovedInstrumentRules)
            .Include(scheme => scheme.RiskProfiles)
            .Include(scheme => scheme.LiquidityThresholds)
            .Include(scheme => scheme.BankAccounts)
            .Include(scheme => scheme.CustodianMappings)
            .Include(scheme => scheme.DistributionRules)
            .Include(scheme => scheme.TemplateMappings)
            .Include(scheme => scheme.VersionHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(scheme => scheme.Id == id, cancellationToken)
            ?? throw new NotFoundException("Scheme was not found.");
    }

    private static HashSet<Guid> SchemeChildIds(Scheme scheme)
    {
        return scheme.Classes.Select(item => item.Id)
            .Concat(scheme.Configurations.Select(item => item.Id))
            .Concat(scheme.FeeSchedules.Select(item => item.Id))
            .Concat(scheme.FeeSchedules.SelectMany(item => item.Rules).Select(item => item.Id))
            .Concat(scheme.EligibilityRules.Select(item => item.Id))
            .Concat(scheme.ApprovedInstrumentRules.Select(item => item.Id))
            .Concat(scheme.RiskProfiles.Select(item => item.Id))
            .Concat(scheme.LiquidityThresholds.Select(item => item.Id))
            .Concat(scheme.BankAccounts.Select(item => item.Id))
            .Concat(scheme.CustodianMappings.Select(item => item.Id))
            .Concat(scheme.DistributionRules.Select(item => item.Id))
            .Concat(scheme.TemplateMappings.Select(item => item.Id))
            .Concat(scheme.VersionHistory.Select(item => item.Id))
            .ToHashSet();
    }

    private void MarkNewSchemeChildrenAsAdded(Scheme scheme, HashSet<Guid> existingChildIds)
    {
        _dbContext.ChangeTracker.DetectChanges();
        foreach (var child in SchemeChildren(scheme).Where(child => !existingChildIds.Contains(child.Id)))
        {
            var entry = _dbContext.Entry(child);
            if (entry.State is EntityState.Detached or EntityState.Modified or EntityState.Unchanged)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private static IEnumerable<Entity> SchemeChildren(Scheme scheme)
    {
        return scheme.Classes.Cast<Entity>()
            .Concat(scheme.Configurations)
            .Concat(scheme.FeeSchedules)
            .Concat(scheme.FeeSchedules.SelectMany(fee => fee.Rules))
            .Concat(scheme.EligibilityRules)
            .Concat(scheme.ApprovedInstrumentRules)
            .Concat(scheme.RiskProfiles)
            .Concat(scheme.LiquidityThresholds)
            .Concat(scheme.BankAccounts)
            .Concat(scheme.CustodianMappings)
            .Concat(scheme.DistributionRules)
            .Concat(scheme.TemplateMappings)
            .Concat(scheme.VersionHistory);
    }

    private async Task<SchemeDto> MapSchemeAsync(Guid id, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes
            .AsNoTracking()
            .Include(candidate => candidate.Classes)
            .Include(candidate => candidate.FeeSchedules).ThenInclude(fee => fee.Rules)
            .Include(candidate => candidate.EligibilityRules)
            .Include(candidate => candidate.ApprovedInstrumentRules)
            .Include(candidate => candidate.BankAccounts)
            .Include(candidate => candidate.CustodianMappings)
            .Include(candidate => candidate.Configurations)
            .Include(candidate => candidate.RiskProfiles)
            .Include(candidate => candidate.LiquidityThresholds)
            .Include(candidate => candidate.DistributionRules)
            .Include(candidate => candidate.TemplateMappings)
            .Include(candidate => candidate.VersionHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Scheme was not found.");

        return new SchemeDto(
            scheme.Id,
            scheme.Code,
            scheme.Name,
            scheme.LegalType,
            scheme.BaseCurrency,
            scheme.Status.ToString(),
            scheme.VersionNumber,
            scheme.PendingEffectiveDate?.Value,
            scheme.SubmittedByUserId,
            scheme.CheckedByUserId,
            scheme.ApprovedByUserId,
            scheme.Classes.OrderBy(schemeClass => schemeClass.Code).Select(MapClass).ToArray(),
            scheme.FeeSchedules.OrderBy(fee => fee.FeeType).ThenBy(fee => fee.EffectiveFrom.Value).Select(MapFeeSchedule).ToArray(),
            scheme.EligibilityRules.OrderBy(rule => rule.RuleType).Select(MapEligibilityRule).ToArray(),
            scheme.ApprovedInstrumentRules.OrderBy(rule => rule.InstrumentType).Select(MapApprovedInstrumentRule).ToArray(),
            scheme.BankAccounts.OrderBy(account => account.Currency).ThenBy(account => account.AccountNumber).Select(MapBankAccount).ToArray(),
            scheme.CustodianMappings.OrderBy(mapping => mapping.CustodianName).Select(MapCustodianMapping).ToArray(),
            scheme.Configurations.OrderBy(configuration => configuration.NavPricingBasis).Select(MapConfiguration).ToArray(),
            scheme.RiskProfiles.OrderBy(profile => profile.RiskRating).Select(MapRiskProfile).ToArray(),
            scheme.LiquidityThresholds.OrderBy(threshold => threshold.MinimumLiquidAssetRatio).Select(MapLiquidityThreshold).ToArray(),
            scheme.DistributionRules.OrderBy(rule => rule.DistributionFrequency).Select(MapDistributionRule).ToArray(),
            scheme.TemplateMappings.OrderBy(mapping => mapping.TemplateType).Select(MapTemplateMapping).ToArray(),
            scheme.VersionHistory.OrderBy(history => history.VersionNumber).ThenBy(history => history.ChangedAtUtc).Select(MapVersionHistory).ToArray());
    }

    private static SchemeClassDto MapClass(SchemeClass schemeClass)
    {
        return new SchemeClassDto(
            schemeClass.Id,
            schemeClass.Code,
            schemeClass.Name,
            schemeClass.Currency,
            schemeClass.ValuationFrequency.ToString(),
            schemeClass.DealingFrequency.ToString(),
            schemeClass.CutOffTime,
            schemeClass.MinimumContribution,
            schemeClass.MinimumBalance,
            schemeClass.LockInDays,
            schemeClass.NoticePeriodDays,
            schemeClass.IsActive);
    }

    private static FeeScheduleDto MapFeeSchedule(FeeSchedule fee)
    {
        return new FeeScheduleDto(
            fee.Id,
            fee.SchemeClassId,
            fee.FeeType,
            fee.CalculationBasis,
            fee.Rate,
            fee.FixedAmount,
            fee.EffectiveFrom.Value,
            fee.EffectiveTo?.Value,
            fee.Status.ToString(),
            fee.Rules.OrderBy(rule => rule.FromAmount).Select(rule => new FeeRuleDto(
                rule.Id,
                rule.FromAmount,
                rule.ToAmount,
                rule.Rate,
                rule.FixedAmount)).ToArray());
    }

    private static SchemeEligibilityRuleDto MapEligibilityRule(SchemeEligibilityRule rule)
    {
        return new SchemeEligibilityRuleDto(rule.Id, rule.RuleType, rule.Description, rule.RuleExpressionJson, rule.Status.ToString());
    }

    private static ApprovedInstrumentRuleDto MapApprovedInstrumentRule(ApprovedInstrumentRule rule)
    {
        return new ApprovedInstrumentRuleDto(
            rule.Id,
            rule.InstrumentType,
            rule.TenorLimitDays,
            rule.IssuerLimit,
            rule.CounterpartyLimit,
            rule.AssetClassLimit,
            rule.Status.ToString());
    }

    private static SchemeBankAccountDto MapBankAccount(SchemeBankAccount account)
    {
        return new SchemeBankAccountDto(account.Id, account.BankName, account.AccountNumber, account.AccountName, account.Currency, account.SwiftCode, account.IsActive);
    }

    private static SchemeCustodianMappingDto MapCustodianMapping(SchemeCustodianMapping mapping)
    {
        return new SchemeCustodianMappingDto(mapping.Id, mapping.CustodianName, mapping.CustodyAccountReference, mapping.SettlementAccountReference, mapping.IsActive);
    }

    private static SchemeConfigurationDto MapConfiguration(Cis.Domain.Schemes.SchemeConfiguration configuration)
    {
        return new SchemeConfigurationDto(configuration.Id, configuration.NavPricingBasis, configuration.IncomeRecognitionBasis);
    }

    private static SchemeRiskProfileDto MapRiskProfile(SchemeRiskProfile profile)
    {
        return new SchemeRiskProfileDto(profile.Id, profile.RiskRating, profile.MaxSingleIssuerExposure);
    }

    private static LiquidityThresholdDto MapLiquidityThreshold(LiquidityThreshold threshold)
    {
        return new LiquidityThresholdDto(threshold.Id, threshold.MinimumLiquidAssetRatio, threshold.WarningThreshold, threshold.BreachThreshold);
    }

    private static DistributionRuleDto MapDistributionRule(DistributionRule rule)
    {
        return new DistributionRuleDto(rule.Id, rule.DistributionFrequency.ToString(), rule.ReinvestmentAllowed, rule.PaymentDay, rule.IsActive);
    }

    private static TemplateMappingDto MapTemplateMapping(TemplateMapping mapping)
    {
        return new TemplateMappingDto(mapping.Id, mapping.TemplateType, mapping.TemplateCode, mapping.IsActive);
    }

    private static SchemeVersionHistoryDto MapVersionHistory(SchemeVersionHistory history)
    {
        return new SchemeVersionHistoryDto(
            history.Id,
            history.VersionNumber,
            history.ChangeType,
            history.ChangedByUserId,
            history.ChangedAtUtc,
            history.EffectiveDate?.Value);
    }

    private static SchemeFrequency ParseFrequency(string value, string fieldName)
    {
        if (Enum.TryParse<SchemeFrequency>(value, ignoreCase: true, out var frequency))
        {
            return frequency;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = ["Unsupported frequency."]
        });
    }

    private static void ValidateJson(string payload, string fieldName)
    {
        try
        {
            using var _ = JsonDocument.Parse(payload);
        }
        catch (JsonException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [fieldName] = [$"JSON payload is invalid: {exception.Message}"]
            });
        }
    }

    private Task WriteAuditAsync(
        AuditEventType eventType,
        string action,
        SchemeDto scheme,
        string? beforeJson,
        string afterJson,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            "Scheme",
            scheme.Id.ToString(),
            EventType: eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason), cancellationToken);
    }

    private static string Snapshot(Scheme scheme)
    {
        return JsonSerializer.Serialize(new
        {
            scheme.Id,
            scheme.Code,
            scheme.Name,
            scheme.LegalType,
            scheme.BaseCurrency,
            Status = scheme.Status.ToString(),
            scheme.VersionNumber,
            ClassCount = scheme.Classes.Count,
            FeeScheduleCount = scheme.FeeSchedules.Count,
            ApprovedInstrumentRuleCount = scheme.ApprovedInstrumentRules.Count,
            BankAccountCount = scheme.BankAccounts.Count
        });
    }

    private static string Snapshot(SchemeDto scheme)
    {
        return JsonSerializer.Serialize(new
        {
            scheme.Id,
            scheme.Code,
            scheme.Name,
            scheme.LegalType,
            scheme.BaseCurrency,
            scheme.Status,
            scheme.VersionNumber,
            ClassCount = scheme.Classes.Count,
            FeeScheduleCount = scheme.FeeSchedules.Count,
            ApprovedInstrumentRuleCount = scheme.ApprovedInstrumentRules.Count,
            BankAccountCount = scheme.BankAccounts.Count
        });
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }
}
