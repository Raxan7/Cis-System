using Cis.Application.Common.Interfaces;
using Cis.Domain.Accounting;
using Cis.Domain.Archive;
using Cis.Domain.Audit;
using Cis.Domain.Cash;
using Cis.Domain.Cases;
using Cis.Domain.Common;
using Cis.Domain.ComplianceRisk;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.DataQuality;
using Cis.Domain.Dealing;
using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Integrations;
using Cis.Domain.NAV;
using Cis.Domain.Operations;
using Cis.Domain.Portal;
using Cis.Domain.Reports;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Domain.Workflows;
using Cis.Domain.Portfolio;
using Cis.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cis.Infrastructure.Persistence;

public sealed class CisDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly IDateTimeProvider? _dateTimeProvider;

    public CisDbContext(DbContextOptions<CisDbContext> options)
        : base(options)
    {
    }

    public CisDbContext(
        DbContextOptions<CisDbContext> options,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();

    public DbSet<ApprovalPolicy> ApprovalPolicies => Set<ApprovalPolicy>();

    public DbSet<ImmutableArchiveRecord> ImmutableArchiveRecords => Set<ImmutableArchiveRecord>();

    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<UserMfaFactor> UserMfaFactors => Set<UserMfaFactor>();

    public DbSet<AccessChangeRequest> AccessChangeRequests => Set<AccessChangeRequest>();

    public DbSet<AccessChangeRequestRole> AccessChangeRequestRoles => Set<AccessChangeRequestRole>();

    public DbSet<Scheme> Schemes => Set<Scheme>();

    public DbSet<SchemeClass> SchemeClasses => Set<SchemeClass>();

    public DbSet<SchemeConfiguration> SchemeConfigurations => Set<SchemeConfiguration>();

    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();

    public DbSet<FeeRule> FeeRules => Set<FeeRule>();

    public DbSet<SchemeEligibilityRule> SchemeEligibilityRules => Set<SchemeEligibilityRule>();

    public DbSet<ApprovedInstrumentRule> ApprovedInstrumentRules => Set<ApprovedInstrumentRule>();

    public DbSet<SchemeRiskProfile> SchemeRiskProfiles => Set<SchemeRiskProfile>();

    public DbSet<LiquidityThreshold> LiquidityThresholds => Set<LiquidityThreshold>();

    public DbSet<SchemeBankAccount> SchemeBankAccounts => Set<SchemeBankAccount>();

    public DbSet<SchemeCustodianMapping> SchemeCustodianMappings => Set<SchemeCustodianMapping>();

    public DbSet<DistributionRule> DistributionRules => Set<DistributionRule>();

    public DbSet<TemplateMapping> TemplateMappings => Set<TemplateMapping>();

    public DbSet<SchemeVersionHistory> SchemeVersionHistory => Set<SchemeVersionHistory>();

    public DbSet<Investor> Investors => Set<Investor>();

    public DbSet<InvestorProfileIndividual> InvestorProfileIndividuals => Set<InvestorProfileIndividual>();

    public DbSet<InvestorProfileCorporate> InvestorProfileCorporates => Set<InvestorProfileCorporate>();

    public DbSet<InvestorProfileJoint> InvestorProfileJoints => Set<InvestorProfileJoint>();

    public DbSet<InvestorProfileGroup> InvestorProfileGroups => Set<InvestorProfileGroup>();

    public DbSet<BeneficialOwner> BeneficialOwners => Set<BeneficialOwner>();

    public DbSet<InvestorBankAccount> InvestorBankAccounts => Set<InvestorBankAccount>();

    public DbSet<InvestorTaxProfile> InvestorTaxProfiles => Set<InvestorTaxProfile>();

    public DbSet<InvestorContact> InvestorContacts => Set<InvestorContact>();

    public DbSet<InvestorMandate> InvestorMandates => Set<InvestorMandate>();

    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();

    public DbSet<KycRequirement> KycRequirements => Set<KycRequirement>();

    public DbSet<KycReview> KycReviews => Set<KycReview>();

    public DbSet<AmlScreeningCase> AmlScreeningCases => Set<AmlScreeningCase>();

    public DbSet<AmlScreeningHit> AmlScreeningHits => Set<AmlScreeningHit>();

    public DbSet<InvestorRiskClassification> InvestorRiskClassifications => Set<InvestorRiskClassification>();

    public DbSet<DuplicateDetectionResult> DuplicateDetectionResults => Set<DuplicateDetectionResult>();

    public DbSet<InvestorChangeLog> InvestorChangeLogs => Set<InvestorChangeLog>();

    public DbSet<DealingInstruction> DealingInstructions => Set<DealingInstruction>();

    public DbSet<SubscriptionInstruction> SubscriptionInstructions => Set<SubscriptionInstruction>();

    public DbSet<RedemptionInstruction> RedemptionInstructions => Set<RedemptionInstruction>();

    public DbSet<SwitchInstruction> SwitchInstructions => Set<SwitchInstruction>();

    public DbSet<TransferInstruction> TransferInstructions => Set<TransferInstruction>();

    public DbSet<Lien> Liens => Set<Lien>();

    public DbSet<RecurringContributionPlan> RecurringContributionPlans => Set<RecurringContributionPlan>();

    public DbSet<DealingBatch> DealingBatches => Set<DealingBatch>();

    public DbSet<DealingValidationResult> DealingValidationResults => Set<DealingValidationResult>();

    public DbSet<CutOffBreach> CutOffBreaches => Set<CutOffBreach>();

    public DbSet<ApprovalThreshold> ApprovalThresholds => Set<ApprovalThreshold>();

    public DbSet<InstructionStatusHistory> InstructionStatusHistory => Set<InstructionStatusHistory>();

    public DbSet<UnitLedgerEntry> UnitLedgerEntries => Set<UnitLedgerEntry>();

    public DbSet<UnitHolding> UnitHoldings => Set<UnitHolding>();

    public DbSet<UnitRegisterSnapshot> UnitRegisterSnapshots => Set<UnitRegisterSnapshot>();

    public DbSet<UnitMovementSource> UnitMovementSources => Set<UnitMovementSource>();

    public DbSet<InvestorPosition> InvestorPositions => Set<InvestorPosition>();

    public DbSet<HistoricalHoldingView> HistoricalHoldingViews => Set<HistoricalHoldingView>();

    public DbSet<UnitAdjustment> UnitAdjustments => Set<UnitAdjustment>();

    public DbSet<BankStatementImport> BankStatementImports => Set<BankStatementImport>();

    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

    public DbSet<CashBookEntry> CashBookEntries => Set<CashBookEntry>();

    public DbSet<CashMatch> CashMatches => Set<CashMatch>();

    public DbSet<SuspenseItem> SuspenseItems => Set<SuspenseItem>();

    public DbSet<PaymentInstruction> PaymentInstructions => Set<PaymentInstruction>();

    public DbSet<PaymentStatusEvent> PaymentStatusEvents => Set<PaymentStatusEvent>();

    public DbSet<ReturnedFund> ReturnedFunds => Set<ReturnedFund>();

    public DbSet<ReversalRequest> ReversalRequests => Set<ReversalRequest>();

    public DbSet<ReconciliationRun> ReconciliationRuns => Set<ReconciliationRun>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    public DbSet<Issuer> Issuers => Set<Issuer>();

    public DbSet<Placement> Placements => Set<Placement>();

    public DbSet<IncomeSchedule> IncomeSchedules => Set<IncomeSchedule>();

    public DbSet<PortfolioHolding> PortfolioHoldings => Set<PortfolioHolding>();

    public DbSet<InvestmentTransaction> InvestmentTransactions => Set<InvestmentTransaction>();

    public DbSet<MandateValidationResult> MandateValidationResults => Set<MandateValidationResult>();

    public DbSet<CounterpartyExposure> CounterpartyExposures => Set<CounterpartyExposure>();

    public DbSet<ValuationRun> ValuationRuns => Set<ValuationRun>();

    public DbSet<ValuationInput> ValuationInputs => Set<ValuationInput>();

    public DbSet<ValuationSource> ValuationSources => Set<ValuationSource>();

    public DbSet<PriceSourceHierarchy> PriceSourceHierarchies => Set<PriceSourceHierarchy>();

    public DbSet<InstrumentValuation> InstrumentValuations => Set<InstrumentValuation>();

    public DbSet<StalePriceException> StalePriceExceptions => Set<StalePriceException>();

    public DbSet<PricingVarianceException> PricingVarianceExceptions => Set<PricingVarianceException>();

    public DbSet<ManualValuationOverride> ManualValuationOverrides => Set<ManualValuationOverride>();

    public DbSet<NavCalculation> NavCalculations => Set<NavCalculation>();

    public DbSet<NavPerUnit> NavPerUnits => Set<NavPerUnit>();

    public DbSet<NavApproval> NavApprovals => Set<NavApproval>();

    public DbSet<NavPublication> NavPublications => Set<NavPublication>();

    public DbSet<NavVersionArchive> NavVersionArchives => Set<NavVersionArchive>();

    public DbSet<NavRestatement> NavRestatements => Set<NavRestatement>();

    public DbSet<ChartOfAccounts> ChartOfAccounts => Set<ChartOfAccounts>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Ledger> Ledgers => Set<Ledger>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<Journal> Journals => Set<Journal>();

    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    public DbSet<JournalTemplate> JournalTemplates => Set<JournalTemplate>();

    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    public DbSet<TrialBalance> TrialBalances => Set<TrialBalance>();

    public DbSet<FinancialStatement> FinancialStatements => Set<FinancialStatement>();

    public DbSet<SuspenseLedgerEntry> SuspenseLedgerEntries => Set<SuspenseLedgerEntry>();

    public DbSet<FairValueAdjustment> FairValueAdjustments => Set<FairValueAdjustment>();

    public DbSet<AccountingNavReconciliation> AccountingNavReconciliations => Set<AccountingNavReconciliation>();

    public DbSet<FeeAccrualRun> FeeAccrualRuns => Set<FeeAccrualRun>();

    public DbSet<FeeCalculation> FeeCalculations => Set<FeeCalculation>();

    public DbSet<FeeWaiverRequest> FeeWaiverRequests => Set<FeeWaiverRequest>();

    public DbSet<TaxRule> TaxRules => Set<TaxRule>();

    public DbSet<TaxCalculation> TaxCalculations => Set<TaxCalculation>();

    public DbSet<VatCalculation> VatCalculations => Set<VatCalculation>();

    public DbSet<WithholdingTaxCalculation> WithholdingTaxCalculations => Set<WithholdingTaxCalculation>();

    public DbSet<DistributionDeclaration> DistributionDeclarations => Set<DistributionDeclaration>();

    public DbSet<DistributionRun> DistributionRuns => Set<DistributionRun>();

    public DbSet<InvestorDistribution> InvestorDistributions => Set<InvestorDistribution>();

    public DbSet<ReinvestmentInstruction> ReinvestmentInstructions => Set<ReinvestmentInstruction>();

    public DbSet<ReinvestmentUnitAllocation> ReinvestmentUnitAllocations => Set<ReinvestmentUnitAllocation>();

    public DbSet<StatutoryLimit> StatutoryLimits => Set<StatutoryLimit>();

    public DbSet<InternalPolicyLimit> InternalPolicyLimits => Set<InternalPolicyLimit>();

    public DbSet<LimitCheckRun> LimitCheckRuns => Set<LimitCheckRun>();

    public DbSet<LimitBreach> LimitBreaches => Set<LimitBreach>();

    public DbSet<BreachExceptionRegister> BreachExceptionRegisters => Set<BreachExceptionRegister>();

    public DbSet<RemediationAction> RemediationActions => Set<RemediationAction>();

    public DbSet<LiquidityCoverageRun> LiquidityCoverageRuns => Set<LiquidityCoverageRun>();

    public DbSet<RedemptionStressScenario> RedemptionStressScenarios => Set<RedemptionStressScenario>();

    public DbSet<RedemptionStressTestRun> RedemptionStressTestRuns => Set<RedemptionStressTestRun>();

    public DbSet<LiquidationTimeAnalysisRun> LiquidationTimeAnalysisRuns => Set<LiquidationTimeAnalysisRun>();

    public DbSet<RelatedPartyExposure> RelatedPartyExposures => Set<RelatedPartyExposure>();

    public DbSet<CounterpartyLimitUsage> CounterpartyLimitUsages => Set<CounterpartyLimitUsage>();

    public DbSet<RiskDashboardSnapshot> RiskDashboardSnapshots => Set<RiskDashboardSnapshot>();

    public DbSet<Custodian> Custodians => Set<Custodian>();

    public DbSet<CustodianAccount> CustodianAccounts => Set<CustodianAccount>();

    public DbSet<CustodianStatementImport> CustodianStatementImports => Set<CustodianStatementImport>();

    public DbSet<CustodianHoldingLine> CustodianHoldingLines => Set<CustodianHoldingLine>();

    public DbSet<CustodianCashLine> CustodianCashLines => Set<CustodianCashLine>();

    public DbSet<CustodyReconciliationRun> CustodyReconciliationRuns => Set<CustodyReconciliationRun>();

    public DbSet<HoldingsReconciliationBreak> HoldingsReconciliationBreaks => Set<HoldingsReconciliationBreak>();

    public DbSet<CashReconciliationBreak> CashReconciliationBreaks => Set<CashReconciliationBreak>();

    public DbSet<BreakAging> BreakAgings => Set<BreakAging>();

    public DbSet<BreakActionNote> BreakActionNotes => Set<BreakActionNote>();

    public DbSet<SafekeepingConfirmation> SafekeepingConfirmations => Set<SafekeepingConfirmation>();

    public DbSet<PortalUserProfile> PortalUserProfiles => Set<PortalUserProfile>();

    public DbSet<PortalSelfRegistration> PortalSelfRegistrations => Set<PortalSelfRegistration>();

    public DbSet<PortalSession> PortalSessions => Set<PortalSession>();

    public DbSet<PortalActivityLog> PortalActivityLogs => Set<PortalActivityLog>();

    public DbSet<DigitalServiceRequest> DigitalServiceRequests => Set<DigitalServiceRequest>();

    public DbSet<PortalDocumentDownload> PortalDocumentDownloads => Set<PortalDocumentDownload>();

    public DbSet<InvestorNotice> InvestorNotices => Set<InvestorNotice>();

    public DbSet<PortalMfaSetting> PortalMfaSettings => Set<PortalMfaSetting>();

    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();

    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();

    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();

    public DbSet<ReportParameter> ReportParameters => Set<ReportParameter>();

    public DbSet<ReportOutput> ReportOutputs => Set<ReportOutput>();

    public DbSet<ReportApproval> ReportApprovals => Set<ReportApproval>();

    public DbSet<ReportDistribution> ReportDistributions => Set<ReportDistribution>();

    public DbSet<ReportVersionArchive> ReportVersionArchives => Set<ReportVersionArchive>();

    public DbSet<ReportBundle> ReportBundles => Set<ReportBundle>();

    public DbSet<ReportOwnerMatrix> ReportOwnerMatrices => Set<ReportOwnerMatrix>();

    public DbSet<IntegrationEndpoint> IntegrationEndpoints => Set<IntegrationEndpoint>();

    public DbSet<IntegrationCredentialReference> IntegrationCredentialReferences => Set<IntegrationCredentialReference>();

    public DbSet<IntegrationMessage> IntegrationMessages => Set<IntegrationMessage>();

    public DbSet<IntegrationIngestionRun> IntegrationIngestionRuns => Set<IntegrationIngestionRun>();

    public DbSet<IntegrationDeliveryAttempt> IntegrationDeliveryAttempts => Set<IntegrationDeliveryAttempt>();

    public DbSet<IntegrationError> IntegrationErrors => Set<IntegrationError>();

    public DbSet<IntegrationIdempotencyKey> IntegrationIdempotencyKeys => Set<IntegrationIdempotencyKey>();

    public DbSet<ServiceCase> ServiceCases => Set<ServiceCase>();

    public DbSet<Complaint> Complaints => Set<Complaint>();

    public DbSet<CaseAction> CaseActions => Set<CaseAction>();

    public DbSet<CaseEscalation> CaseEscalations => Set<CaseEscalation>();

    public DbSet<CaseSlaPolicy> CaseSlaPolicies => Set<CaseSlaPolicy>();

    public DbSet<CaseStatusHistory> CaseStatusHistory => Set<CaseStatusHistory>();

    public DbSet<DataQualityRule> DataQualityRules => Set<DataQualityRule>();

    public DbSet<DataQualityCheckRun> DataQualityCheckRuns => Set<DataQualityCheckRun>();

    public DbSet<DataQualityException> DataQualityExceptions => Set<DataQualityException>();

    public DbSet<ExceptionQueue> ExceptionQueue => Set<ExceptionQueue>();

    public DbSet<ExceptionAssignment> ExceptionAssignments => Set<ExceptionAssignment>();

    public DbSet<DataQualityDashboardSnapshot> DataQualityDashboardSnapshots => Set<DataQualityDashboardSnapshot>();

    public DbSet<RtoRpoConfiguration> RtoRpoConfigurations => Set<RtoRpoConfiguration>();

    public DbSet<DRTestRecord> DRTestRecords => Set<DRTestRecord>();

    public DbSet<BackupRunRecord> BackupRunRecords => Set<BackupRunRecord>();

    public DbSet<RestoreTestRecord> RestoreTestRecords => Set<RestoreTestRecord>();

    public override int SaveChanges()
    {
        ApplyAuditMetadataAndConcurrency();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditMetadataAndConcurrency();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<BusinessDate>().HaveConversion<BusinessDateConverter>().HaveColumnType("date");
        configurationBuilder.Properties<Percentage>().HaveConversion<PercentageConverter>().HavePrecision(18, 10);
        configurationBuilder.Properties<RowVersion>().HaveConversion<RowVersionConverter>().HaveColumnType("uuid");
        configurationBuilder.Properties<decimal>().HavePrecision(38, 12);
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CisDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureRowVersion(entityType);
            ConfigureDecimalPrecision(entityType);
        }
    }

    private static void ConfigureRowVersion(IMutableEntityType entityType)
    {
        var rowVersion = entityType.FindProperty(nameof(Entity.RowVersion));
        if (rowVersion is null)
        {
            return;
        }

        rowVersion.IsConcurrencyToken = true;
        rowVersion.SetColumnName("row_version");
        rowVersion.SetValueComparer(new ValueComparer<RowVersion>(
            (left, right) => left != null && right != null && left.Value == right.Value,
            value => value.Value.GetHashCode(),
            value => RowVersion.From(value.Value)));
    }

    private static void ConfigureDecimalPrecision(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties().Where(property => property.ClrType == typeof(decimal)))
        {
            property.SetPrecision(38);
            property.SetScale(12);
        }
    }

    private void ApplyAuditMetadataAndConcurrency()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var actor = _currentUserContext?.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit logs are append-only and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<ImmutableArchiveRecord>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Immutable archive records cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<UnitLedgerEntry>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Unit ledger entries are append-only and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<LedgerEntry>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Accounting ledger entries are append-only and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<NavPublication>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Published NAV records are immutable and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<NavVersionArchive>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("NAV version archives are immutable and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<NavRestatement>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("NAV restatements are immutable and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<ReportVersionArchive>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Published report version archives are immutable and cannot be updated or deleted.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            entry.Entity.RefreshRowVersion();

            if (entry.Entity is not IAuditableEntity auditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                auditable.MarkCreated(actor, now);
            }
            else
            {
                auditable.MarkModified(actor, now);
            }
        }
    }
}
