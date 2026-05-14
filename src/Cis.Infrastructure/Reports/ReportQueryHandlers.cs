using System.Globalization;
using System.Reflection;
using System.Text;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Reports;
using Cis.Domain.Cases;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Reports;

internal interface IReportCatalogueQueryHandler
{
    string Code { get; }

    Task<ReportQueryResultDto> QueryAsync(ReportQueryParameters parameters, CancellationToken cancellationToken);

    Task<ReportCsvExportDto> ExportCsvAsync(ReportQueryParameters parameters, CancellationToken cancellationToken);
}

internal sealed class ReportQueryHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IReportCatalogueQueryHandler> _handlers;

    public ReportQueryHandlerRegistry(CisDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _handlers = ReportCatalogue.Entries
            .Select(entry => (IReportCatalogueQueryHandler)new RegisteredReportQueryHandler(entry, dbContext, dateTimeProvider))
            .ToDictionary(handler => handler.Code, StringComparer.OrdinalIgnoreCase);
    }

    public IReportCatalogueQueryHandler Get(string code)
    {
        return _handlers.TryGetValue(code.Trim().ToUpperInvariant(), out var handler)
            ? handler
            : throw new NotFoundException("Report query handler was not found.");
    }

    public IReadOnlyCollection<string> Codes => _handlers.Keys.OrderBy(code => code).ToArray();
}

internal sealed class RegisteredReportQueryHandler : IReportCatalogueQueryHandler
{
    private readonly ReportCatalogueEntry _entry;
    private readonly CisDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisteredReportQueryHandler(ReportCatalogueEntry entry, CisDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _entry = entry;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public string Code => _entry.Code;

    public async Task<ReportQueryResultDto> QueryAsync(ReportQueryParameters parameters, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var sourceRecordCount = await CountSourceRecordsAsync(_entry.SourceEntity, cancellationToken);
        var exceptionCount = await CountExceptionsAsync(_entry.SourceEntity, cancellationToken);
        var amountTotal = await CalculateAmountTotalAsync(_entry.SourceEntity, cancellationToken);
        var row = CreateRow(parameters, sourceRecordCount, exceptionCount, amountTotal, now);

        return new ReportQueryResultDto(
            _entry.Code,
            _entry.Name,
            _entry.Category,
            _entry.RequiredPermission,
            parameters,
            now,
            1,
            [row]);
    }

    public async Task<ReportCsvExportDto> ExportCsvAsync(ReportQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await QueryAsync(parameters, cancellationToken);
        var csv = ToCsv(result.Rows);
        return new ReportCsvExportDto(
            _entry.Code,
            $"{_entry.Code.ToLowerInvariant()}-{result.GeneratedAtUtc:yyyyMMddHHmmss}.csv",
            "text/csv",
            csv,
            result.RowCount,
            result.GeneratedAtUtc);
    }

    private ReportCatalogueRowDto CreateRow(ReportQueryParameters parameters, int sourceRecordCount, int exceptionCount, decimal amountTotal, DateTime generatedAtUtc)
    {
        var instance = Activator.CreateInstance(
            _entry.DtoType,
            parameters.FromDate,
            parameters.ToDate,
            _entry.SourceEntity,
            sourceRecordCount,
            exceptionCount,
            amountTotal,
            generatedAtUtc);

        return instance as ReportCatalogueRowDto
            ?? throw new InvalidOperationException($"Report DTO type '{_entry.DtoType.Name}' is not a report catalogue row DTO.");
    }

    private async Task<int> CountSourceRecordsAsync(string sourceEntity, CancellationToken cancellationToken)
    {
        return sourceEntity switch
        {
            "AccessChangeRequests" => await _dbContext.AccessChangeRequests.AsNoTracking().CountAsync(cancellationToken),
            "AccountingNavReconciliations" => await _dbContext.AccountingNavReconciliations.AsNoTracking().CountAsync(cancellationToken),
            "Accounts" => await _dbContext.Accounts.AsNoTracking().CountAsync(cancellationToken),
            "AmlScreeningHits" => await _dbContext.AmlScreeningHits.AsNoTracking().CountAsync(cancellationToken),
            "ApprovedInstrumentRules" => await _dbContext.ApprovedInstrumentRules.AsNoTracking().CountAsync(cancellationToken),
            "AuditLogs" => await _dbContext.AuditLogs.AsNoTracking().CountAsync(cancellationToken),
            "BankStatementLines" => await _dbContext.BankStatementLines.AsNoTracking().CountAsync(cancellationToken),
            "CashBookEntries" => await _dbContext.CashBookEntries.AsNoTracking().CountAsync(cancellationToken),
            "Complaints" => await _dbContext.Complaints.AsNoTracking().CountAsync(cancellationToken),
            "CounterpartyExposures" => await _dbContext.CounterpartyExposures.AsNoTracking().CountAsync(cancellationToken),
            "CounterpartyLimitUsages" => await _dbContext.CounterpartyLimitUsages.AsNoTracking().CountAsync(cancellationToken),
            "CustodianCashLines" => await _dbContext.CustodianCashLines.AsNoTracking().CountAsync(cancellationToken),
            "CustodianHoldingLines" => await _dbContext.CustodianHoldingLines.AsNoTracking().CountAsync(cancellationToken),
            "CustodyReconciliationRuns" => await _dbContext.CustodyReconciliationRuns.AsNoTracking().CountAsync(cancellationToken),
            "CutOffBreaches" => await _dbContext.CutOffBreaches.AsNoTracking().CountAsync(cancellationToken),
            "DealingInstructions" => await _dbContext.DealingInstructions.AsNoTracking().CountAsync(cancellationToken),
            "DistributionRuns" => await _dbContext.DistributionRuns.AsNoTracking().CountAsync(cancellationToken),
            "FeeAccrualRuns" => await _dbContext.FeeAccrualRuns.AsNoTracking().CountAsync(cancellationToken),
            "FeeCalculations" => await _dbContext.FeeCalculations.AsNoTracking().CountAsync(cancellationToken),
            "FeeSchedules" => await _dbContext.FeeSchedules.AsNoTracking().CountAsync(cancellationToken),
            "FinancialStatements" => await _dbContext.FinancialStatements.AsNoTracking().CountAsync(cancellationToken),
            "IncomeSchedules" => await _dbContext.IncomeSchedules.AsNoTracking().CountAsync(cancellationToken),
            "InternalPolicyLimits" => await _dbContext.InternalPolicyLimits.AsNoTracking().CountAsync(cancellationToken),
            "InstrumentValuations" => await _dbContext.InstrumentValuations.AsNoTracking().CountAsync(cancellationToken),
            "IntegrationErrors" => await _dbContext.IntegrationErrors.AsNoTracking().CountAsync(cancellationToken),
            "InvestmentTransactions" => await _dbContext.InvestmentTransactions.AsNoTracking().CountAsync(cancellationToken),
            "InvestorBankAccounts" => await _dbContext.InvestorBankAccounts.AsNoTracking().CountAsync(cancellationToken),
            "InvestorDistributions" => await _dbContext.InvestorDistributions.AsNoTracking().CountAsync(cancellationToken),
            "InvestorMandates" => await _dbContext.InvestorMandates.AsNoTracking().CountAsync(cancellationToken),
            "InvestorPositions" => await _dbContext.InvestorPositions.AsNoTracking().CountAsync(cancellationToken),
            "Investors" => await _dbContext.Investors.AsNoTracking().CountAsync(cancellationToken),
            "JournalLines" => await _dbContext.JournalLines.AsNoTracking().CountAsync(cancellationToken),
            "Journals" => await _dbContext.Journals.AsNoTracking().CountAsync(cancellationToken),
            "KycDocuments" => await _dbContext.KycDocuments.AsNoTracking().CountAsync(cancellationToken),
            "KycRequirements" => await _dbContext.KycRequirements.AsNoTracking().CountAsync(cancellationToken),
            "LedgerEntries" => await _dbContext.LedgerEntries.AsNoTracking().CountAsync(cancellationToken),
            "Liens" => await _dbContext.Liens.AsNoTracking().CountAsync(cancellationToken),
            "LimitBreaches" => await _dbContext.LimitBreaches.AsNoTracking().CountAsync(cancellationToken),
            "LiquidationTimeAnalysisRuns" => await _dbContext.LiquidationTimeAnalysisRuns.AsNoTracking().CountAsync(cancellationToken),
            "LiquidityCoverageRuns" => await _dbContext.LiquidityCoverageRuns.AsNoTracking().CountAsync(cancellationToken),
            "MandateValidationResults" => await _dbContext.MandateValidationResults.AsNoTracking().CountAsync(cancellationToken),
            "ManualValuationOverrides" => await _dbContext.ManualValuationOverrides.AsNoTracking().CountAsync(cancellationToken),
            "NavApprovals" => await _dbContext.NavApprovals.AsNoTracking().CountAsync(cancellationToken),
            "NavCalculations" => await _dbContext.NavCalculations.AsNoTracking().CountAsync(cancellationToken),
            "NavPerUnits" => await _dbContext.NavPerUnits.AsNoTracking().CountAsync(cancellationToken),
            "NavPublications" => await _dbContext.NavPublications.AsNoTracking().CountAsync(cancellationToken),
            "NavVersionArchives" => await _dbContext.NavVersionArchives.AsNoTracking().CountAsync(cancellationToken),
            "PaymentInstructions" => await _dbContext.PaymentInstructions.AsNoTracking().CountAsync(cancellationToken),
            "Placements" => await _dbContext.Placements.AsNoTracking().CountAsync(cancellationToken),
            "PortfolioHoldings" => await _dbContext.PortfolioHoldings.AsNoTracking().CountAsync(cancellationToken),
            "PortalActivityLogs" => await _dbContext.PortalActivityLogs.AsNoTracking().CountAsync(cancellationToken),
            "PortalDocumentDownloads" => await _dbContext.PortalDocumentDownloads.AsNoTracking().CountAsync(cancellationToken),
            "PortalSessions" => await _dbContext.PortalSessions.AsNoTracking().CountAsync(cancellationToken),
            "ReconciliationBreaks" => await _dbContext.HoldingsReconciliationBreaks.AsNoTracking().CountAsync(cancellationToken) + await _dbContext.CashReconciliationBreaks.AsNoTracking().CountAsync(cancellationToken),
            "ReconciliationRuns" => await _dbContext.ReconciliationRuns.AsNoTracking().CountAsync(cancellationToken),
            "RecurringContributionPlans" => await _dbContext.RecurringContributionPlans.AsNoTracking().CountAsync(cancellationToken),
            "RedemptionInstructions" => await _dbContext.RedemptionInstructions.AsNoTracking().CountAsync(cancellationToken),
            "RedemptionStressTestRuns" => await _dbContext.RedemptionStressTestRuns.AsNoTracking().CountAsync(cancellationToken),
            "RelatedPartyExposures" => await _dbContext.RelatedPartyExposures.AsNoTracking().CountAsync(cancellationToken),
            "ReportBundles" => await _dbContext.ReportBundles.AsNoTracking().CountAsync(cancellationToken),
            "ReportRuns" => await _dbContext.ReportRuns.AsNoTracking().CountAsync(cancellationToken),
            "ReturnedFunds" => await _dbContext.ReturnedFunds.AsNoTracking().CountAsync(cancellationToken),
            "RiskDashboardSnapshots" => await _dbContext.RiskDashboardSnapshots.AsNoTracking().CountAsync(cancellationToken),
            "SafekeepingConfirmations" => await _dbContext.SafekeepingConfirmations.AsNoTracking().CountAsync(cancellationToken),
            "Schemes" => await _dbContext.Schemes.AsNoTracking().CountAsync(cancellationToken),
            "StatutoryLimits" => await _dbContext.StatutoryLimits.AsNoTracking().CountAsync(cancellationToken),
            "SubscriptionInstructions" => await _dbContext.SubscriptionInstructions.AsNoTracking().CountAsync(cancellationToken),
            "SuspenseItems" => await _dbContext.SuspenseItems.AsNoTracking().CountAsync(cancellationToken),
            "SwitchInstructions" => await _dbContext.SwitchInstructions.AsNoTracking().CountAsync(cancellationToken),
            "SwitchTransferInstructions" => await _dbContext.SwitchInstructions.AsNoTracking().CountAsync(cancellationToken) + await _dbContext.TransferInstructions.AsNoTracking().CountAsync(cancellationToken),
            "TaxCalculations" => await _dbContext.TaxCalculations.AsNoTracking().CountAsync(cancellationToken),
            "TransferInstructions" => await _dbContext.TransferInstructions.AsNoTracking().CountAsync(cancellationToken),
            "TrialBalances" => await _dbContext.TrialBalances.AsNoTracking().CountAsync(cancellationToken),
            "UnitHoldings" => await _dbContext.UnitHoldings.AsNoTracking().CountAsync(cancellationToken),
            "ValuationExceptions" => await _dbContext.StalePriceExceptions.AsNoTracking().CountAsync(cancellationToken) + await _dbContext.PricingVarianceExceptions.AsNoTracking().CountAsync(cancellationToken),
            "ValuationSources" => await _dbContext.ValuationSources.AsNoTracking().CountAsync(cancellationToken),
            "VatCalculations" => await _dbContext.VatCalculations.AsNoTracking().CountAsync(cancellationToken),
            "WithholdingTaxCalculations" => await _dbContext.WithholdingTaxCalculations.AsNoTracking().CountAsync(cancellationToken),
            "WorkflowActions" => await _dbContext.WorkflowActions.AsNoTracking().CountAsync(cancellationToken),
            "WorkflowInstances" => await _dbContext.WorkflowInstances.AsNoTracking().CountAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Report source entity '{sourceEntity}' is not mapped.")
        };
    }

    private async Task<int> CountExceptionsAsync(string sourceEntity, CancellationToken cancellationToken)
    {
        return sourceEntity switch
        {
            "AmlScreeningHits" => await _dbContext.AmlScreeningHits.AsNoTracking().CountAsync(cancellationToken),
            "Complaints" => await _dbContext.ServiceCases.AsNoTracking().CountAsync(serviceCase =>
                serviceCase.Status == ServiceCaseStatus.Escalated ||
                (serviceCase.Status != ServiceCaseStatus.Resolved && serviceCase.Status != ServiceCaseStatus.Closed && serviceCase.SlaTargetAtUtc < _dateTimeProvider.UtcNow),
                cancellationToken),
            "CutOffBreaches" => await _dbContext.CutOffBreaches.AsNoTracking().CountAsync(cancellationToken),
            "KycDocuments" => await _dbContext.KycDocuments.AsNoTracking().CountAsync(cancellationToken),
            "IntegrationErrors" => await _dbContext.IntegrationErrors.AsNoTracking().CountAsync(cancellationToken),
            "LimitBreaches" => await _dbContext.LimitBreaches.AsNoTracking().CountAsync(cancellationToken),
            "ReconciliationBreaks" => await _dbContext.HoldingsReconciliationBreaks.AsNoTracking().CountAsync(cancellationToken) + await _dbContext.CashReconciliationBreaks.AsNoTracking().CountAsync(cancellationToken),
            "SuspenseItems" => await _dbContext.SuspenseItems.AsNoTracking().CountAsync(cancellationToken),
            "ValuationExceptions" => await _dbContext.StalePriceExceptions.AsNoTracking().CountAsync(cancellationToken) + await _dbContext.PricingVarianceExceptions.AsNoTracking().CountAsync(cancellationToken),
            _ => 0
        };
    }

    private async Task<decimal> CalculateAmountTotalAsync(string sourceEntity, CancellationToken cancellationToken)
    {
        return sourceEntity switch
        {
            "Complaints" => Math.Round(await _dbContext.Complaints
                .AsNoTracking()
                .Where(complaint => complaint.TurnaroundDays.HasValue)
                .Select(complaint => (decimal?)complaint.TurnaroundDays!.Value)
                .AverageAsync(cancellationToken) ?? 0m, 2),
            _ => 0m
        };
    }

    private static string ToCsv(IEnumerable<ReportCatalogueRowDto> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Code,Name,Category,FromDate,ToDate,SourceEntity,SourceRecordCount,ExceptionCount,AmountTotal,GeneratedAtUtc");
        foreach (var row in rows)
        {
            builder.Append(Escape(row.Code)).Append(',')
                .Append(Escape(row.Name)).Append(',')
                .Append(Escape(row.Category)).Append(',')
                .Append(Escape(row.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty)).Append(',')
                .Append(Escape(row.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty)).Append(',')
                .Append(Escape(row.SourceEntity)).Append(',')
                .Append(row.SourceRecordCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.ExceptionCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.AmountTotal.ToString(CultureInfo.InvariantCulture)).Append(',')
                .AppendLine(Escape(row.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
