using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class Scheme : AuditableAggregateRoot
{
    private readonly List<SchemeClass> _classes = [];
    private readonly List<SchemeConfiguration> _configurations = [];
    private readonly List<FeeSchedule> _feeSchedules = [];
    private readonly List<SchemeEligibilityRule> _eligibilityRules = [];
    private readonly List<ApprovedInstrumentRule> _approvedInstrumentRules = [];
    private readonly List<SchemeRiskProfile> _riskProfiles = [];
    private readonly List<LiquidityThreshold> _liquidityThresholds = [];
    private readonly List<SchemeBankAccount> _bankAccounts = [];
    private readonly List<SchemeCustodianMapping> _custodianMappings = [];
    private readonly List<DistributionRule> _distributionRules = [];
    private readonly List<TemplateMapping> _templateMappings = [];
    private readonly List<SchemeVersionHistory> _versionHistory = [];

    private Scheme()
    {
    }

    private Scheme(string code, string name, string legalType, string baseCurrency, string createdByUserId, DateTime createdAtUtc)
    {
        SchemeValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Code = SchemeValidation.Required(code, nameof(code), 30).ToUpperInvariant();
        Name = SchemeValidation.Required(name, nameof(name), 200);
        LegalType = SchemeValidation.Required(legalType, nameof(legalType), 100);
        BaseCurrency = SchemeValidation.Currency(baseCurrency, nameof(baseCurrency));
        Status = SchemeStatus.Draft;
        VersionNumber = 1;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string LegalType { get; private set; } = string.Empty;

    public string BaseCurrency { get; private set; } = string.Empty;

    public SchemeStatus Status { get; private set; }

    public int VersionNumber { get; private set; }

    public BusinessDate? PendingEffectiveDate { get; private set; }

    public string? SubmittedByUserId { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? CheckedByUserId { get; private set; }

    public DateTime? CheckedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public IReadOnlyCollection<SchemeClass> Classes => _classes.AsReadOnly();

    public IReadOnlyCollection<SchemeConfiguration> Configurations => _configurations.AsReadOnly();

    public IReadOnlyCollection<FeeSchedule> FeeSchedules => _feeSchedules.AsReadOnly();

    public IReadOnlyCollection<SchemeEligibilityRule> EligibilityRules => _eligibilityRules.AsReadOnly();

    public IReadOnlyCollection<ApprovedInstrumentRule> ApprovedInstrumentRules => _approvedInstrumentRules.AsReadOnly();

    public IReadOnlyCollection<SchemeRiskProfile> RiskProfiles => _riskProfiles.AsReadOnly();

    public IReadOnlyCollection<LiquidityThreshold> LiquidityThresholds => _liquidityThresholds.AsReadOnly();

    public IReadOnlyCollection<SchemeBankAccount> BankAccounts => _bankAccounts.AsReadOnly();

    public IReadOnlyCollection<SchemeCustodianMapping> CustodianMappings => _custodianMappings.AsReadOnly();

    public IReadOnlyCollection<DistributionRule> DistributionRules => _distributionRules.AsReadOnly();

    public IReadOnlyCollection<TemplateMapping> TemplateMappings => _templateMappings.AsReadOnly();

    public IReadOnlyCollection<SchemeVersionHistory> VersionHistory => _versionHistory.AsReadOnly();

    public static Scheme Create(string code, string name, string legalType, string baseCurrency, string createdByUserId, DateTime createdAtUtc)
    {
        return new Scheme(code, name, legalType, baseCurrency, createdByUserId, createdAtUtc);
    }

    public void AmendDetails(string name, string legalType, string baseCurrency, BusinessDate? effectiveDate)
    {
        EnsureCanChange();
        Name = SchemeValidation.Required(name, nameof(name), 200);
        LegalType = SchemeValidation.Required(legalType, nameof(legalType), 100);
        BaseCurrency = SchemeValidation.Currency(baseCurrency, nameof(baseCurrency));
        PendingEffectiveDate = effectiveDate;
        MoveActiveSchemeToAmendmentDraft();
    }

    public SchemeClass AddClass(
        string code,
        string name,
        string currency,
        SchemeFrequency valuationFrequency,
        SchemeFrequency dealingFrequency,
        TimeOnly cutOffTime,
        decimal minimumContribution,
        decimal minimumBalance,
        int lockInDays,
        int noticePeriodDays)
    {
        EnsureCanChange();
        var schemeClass = SchemeClass.Create(Id, code, name, currency, valuationFrequency, dealingFrequency, cutOffTime, minimumContribution, minimumBalance, lockInDays, noticePeriodDays);
        if (_classes.Any(existing => string.Equals(existing.Code, schemeClass.Code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A class with the same code already exists for this scheme.");
        }

        _classes.Add(schemeClass);
        MoveActiveSchemeToAmendmentDraft();
        return schemeClass;
    }

    public SchemeConfiguration AddConfiguration(string navPricingBasis, string incomeRecognitionBasis)
    {
        EnsureCanChange();
        var configuration = SchemeConfiguration.Create(Id, navPricingBasis, incomeRecognitionBasis);
        _configurations.Add(configuration);
        MoveActiveSchemeToAmendmentDraft();
        return configuration;
    }

    public FeeSchedule AddFeeSchedule(
        Guid? schemeClassId,
        string feeType,
        string calculationBasis,
        decimal? rate,
        decimal? fixedAmount,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        IEnumerable<(decimal? FromAmount, decimal? ToAmount, decimal? Rate, decimal? FixedAmount)> tierRules)
    {
        EnsureCanChange();
        if (schemeClassId.HasValue && _classes.All(schemeClass => schemeClass.Id != schemeClassId.Value))
        {
            throw new InvalidOperationException("Fee schedule class does not belong to the scheme.");
        }

        var schedule = FeeSchedule.Create(Id, schemeClassId, feeType, calculationBasis, rate, fixedAmount, effectiveFrom, effectiveTo);
        foreach (var tierRule in tierRules)
        {
            schedule.AddRule(tierRule.FromAmount, tierRule.ToAmount, tierRule.Rate, tierRule.FixedAmount);
        }

        if (!schedule.Rate.HasValue && !schedule.FixedAmount.HasValue && !schedule.IsTiered)
        {
            throw new InvalidOperationException("Fee schedule must define a rate, fixed amount, or tier rules.");
        }

        var overlaps = _feeSchedules
            .Where(existing => existing.Status != FeeScheduleStatus.Rejected
                && string.Equals(existing.FeeType, schedule.FeeType, StringComparison.OrdinalIgnoreCase)
                && existing.SchemeClassId == schedule.SchemeClassId
                && existing.EffectiveDatesOverlap(schedule));
        if (overlaps.Any(existing => !existing.IsTiered || !schedule.IsTiered))
        {
            throw new InvalidOperationException("Fee schedule effective dates cannot overlap for the same fee type and class unless both schedules are tiered.");
        }

        _feeSchedules.Add(schedule);
        MoveActiveSchemeToAmendmentDraft();
        return schedule;
    }

    public SchemeEligibilityRule AddEligibilityRule(string ruleType, string description, string ruleExpressionJson)
    {
        EnsureCanChange();
        var rule = SchemeEligibilityRule.Create(Id, ruleType, description, ruleExpressionJson);
        _eligibilityRules.Add(rule);
        MoveActiveSchemeToAmendmentDraft();
        return rule;
    }

    public ApprovedInstrumentRule AddApprovedInstrumentRule(
        string instrumentType,
        int tenorLimitDays,
        decimal issuerLimit,
        decimal counterpartyLimit,
        decimal assetClassLimit)
    {
        EnsureCanChange();
        var rule = ApprovedInstrumentRule.Create(Id, instrumentType, tenorLimitDays, issuerLimit, counterpartyLimit, assetClassLimit);
        _approvedInstrumentRules.Add(rule);
        MoveActiveSchemeToAmendmentDraft();
        return rule;
    }

    public SchemeBankAccount AddBankAccount(string bankName, string accountNumber, string accountName, string currency, string? swiftCode)
    {
        EnsureCanChange();
        if (_bankAccounts.Any(account =>
                account.IsActive
                && string.Equals(account.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase)
                && string.Equals(account.Currency, currency, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A bank account with the same account number and currency already exists for this scheme.");
        }

        var bankAccount = SchemeBankAccount.Create(Id, bankName, accountNumber, accountName, currency, swiftCode);
        _bankAccounts.Add(bankAccount);
        MoveActiveSchemeToAmendmentDraft();
        return bankAccount;
    }

    public SchemeCustodianMapping AddCustodianMapping(string custodianName, string custodyAccountReference, string settlementAccountReference)
    {
        EnsureCanChange();
        var mapping = SchemeCustodianMapping.Create(Id, custodianName, custodyAccountReference, settlementAccountReference);
        _custodianMappings.Add(mapping);
        MoveActiveSchemeToAmendmentDraft();
        return mapping;
    }

    public SchemeRiskProfile AddRiskProfile(string riskRating, decimal maxSingleIssuerExposure)
    {
        EnsureCanChange();
        var profile = SchemeRiskProfile.Create(Id, riskRating, maxSingleIssuerExposure);
        _riskProfiles.Add(profile);
        MoveActiveSchemeToAmendmentDraft();
        return profile;
    }

    public LiquidityThreshold AddLiquidityThreshold(decimal minimumLiquidAssetRatio, decimal warningThreshold, decimal breachThreshold)
    {
        EnsureCanChange();
        var threshold = LiquidityThreshold.Create(Id, minimumLiquidAssetRatio, warningThreshold, breachThreshold);
        _liquidityThresholds.Add(threshold);
        MoveActiveSchemeToAmendmentDraft();
        return threshold;
    }

    public DistributionRule AddDistributionRule(SchemeFrequency distributionFrequency, bool reinvestmentAllowed, int paymentDay)
    {
        EnsureCanChange();
        var rule = DistributionRule.Create(Id, distributionFrequency, reinvestmentAllowed, paymentDay);
        _distributionRules.Add(rule);
        MoveActiveSchemeToAmendmentDraft();
        return rule;
    }

    public TemplateMapping AddTemplateMapping(string templateType, string templateCode)
    {
        EnsureCanChange();
        var mapping = TemplateMapping.Create(Id, templateType, templateCode);
        _templateMappings.Add(mapping);
        MoveActiveSchemeToAmendmentDraft();
        return mapping;
    }

    public void Submit(string submittedByUserId, DateTime submittedAtUtc)
    {
        SchemeValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status is not (SchemeStatus.Draft or SchemeStatus.AmendmentDraft))
        {
            throw new InvalidOperationException("Only draft schemes can be submitted.");
        }

        SubmittedByUserId = SchemeValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = submittedAtUtc;
        CheckedByUserId = null;
        CheckedAtUtc = null;
        ApprovedByUserId = null;
        ApprovedAtUtc = null;
        Status = SchemeStatus.Submitted;
    }

    public void Check(string checkedByUserId, DateTime checkedAtUtc)
    {
        SchemeValidation.EnsureUtc(checkedAtUtc, nameof(checkedAtUtc));
        if (Status != SchemeStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted schemes can be checked.");
        }

        var actor = SchemeValidation.Required(checkedByUserId, nameof(checkedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot check the same scheme workflow.");
        }

        CheckedByUserId = actor;
        CheckedAtUtc = checkedAtUtc;
        Status = SchemeStatus.Checked;
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        SchemeValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != SchemeStatus.Checked)
        {
            throw new InvalidOperationException("Only checked schemes can be approved.");
        }

        var actor = SchemeValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(actor, CheckedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: approver cannot be the submitter or checker.");
        }

        EnsureReadyForActivation();
        foreach (var feeSchedule in _feeSchedules.Where(fee => fee.Status == FeeScheduleStatus.Draft))
        {
            feeSchedule.Activate();
        }

        foreach (var rule in _eligibilityRules.Where(rule => rule.Status == RuleStatus.Draft))
        {
            rule.Activate();
        }

        foreach (var rule in _approvedInstrumentRules.Where(rule => rule.Status == RuleStatus.Draft))
        {
            rule.Activate();
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        Status = SchemeStatus.Active;
        VersionNumber += 1;
    }

    public void AddVersionHistory(
        string changeType,
        string changedByUserId,
        DateTime changedAtUtc,
        BusinessDate? effectiveDate,
        string? beforeJson,
        string afterJson)
    {
        _versionHistory.Add(SchemeVersionHistory.Create(
            Id,
            VersionNumber,
            changeType,
            changedByUserId,
            changedAtUtc,
            effectiveDate,
            beforeJson,
            afterJson));
    }

    public void EnsureReadyForActivation()
    {
        if (_classes.Count == 0)
        {
            throw new InvalidOperationException("Scheme requires at least one class before activation.");
        }

        if (_classes.Any(schemeClass => schemeClass.ValuationFrequency == default || schemeClass.DealingFrequency == default))
        {
            throw new InvalidOperationException("Scheme classes require valuation and dealing frequencies before activation.");
        }

        if (_bankAccounts.All(account => !account.IsActive))
        {
            throw new InvalidOperationException("Scheme requires at least one active bank account before activation.");
        }

        if (_feeSchedules.Count == 0)
        {
            throw new InvalidOperationException("Scheme requires at least one fee schedule before activation.");
        }

        if (_approvedInstrumentRules.Count == 0)
        {
            throw new InvalidOperationException("Scheme requires at least one approved instrument rule before activation.");
        }
    }

    private void EnsureCanChange()
    {
        if (Status is SchemeStatus.Submitted or SchemeStatus.Checked)
        {
            throw new InvalidOperationException("Submitted or checked schemes cannot be amended until the workflow is completed or rejected.");
        }

        if (Status is SchemeStatus.Closed)
        {
            throw new InvalidOperationException("Closed schemes cannot be amended.");
        }
    }

    private void MoveActiveSchemeToAmendmentDraft()
    {
        if (Status == SchemeStatus.Active)
        {
            Status = SchemeStatus.AmendmentDraft;
        }
    }
}
