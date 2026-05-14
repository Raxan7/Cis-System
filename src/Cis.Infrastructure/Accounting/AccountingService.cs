using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Accounting;
using Cis.Domain.Accounting;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.NAV;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Accounting;

internal sealed class AccountingService : IAccountingService
{
    private const string ModuleName = "Accounting";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccountingService(
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

    public async Task<ChartOfAccountsDto> CreateChartOfAccountsAsync(CreateChartOfAccountsRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        if (request.Accounts is null || request.Accounts.Count < 2)
        {
            throw Validation("accounts", "A chart of accounts requires at least two accounts.");
        }

        await EnsureSchemeActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var normalizedCode = request.Code.ToUpperInvariant();
        if (await _dbContext.ChartOfAccounts.AnyAsync(chart => chart.SchemeId == request.SchemeId && chart.Code == normalizedCode, cancellationToken))
        {
            throw new ConflictException("A chart of accounts with the same code already exists for this scheme.");
        }

        var chart = ChartOfAccounts.Create(request.SchemeId, request.Code, request.Name, request.BaseCurrency, actor, now);
        foreach (var accountRequest in request.Accounts)
        {
            chart.AddAccount(
                accountRequest.Code,
                accountRequest.Name,
                ParseEnum<AccountType>(accountRequest.AccountType, "accountType"),
                accountRequest.IsControlAccount);
        }

        AddDefaultTemplates(chart);
        var ledger = Ledger.Create(request.SchemeId, request.SchemeClassId, chart.Id, $"LED-{chart.Code}", $"{chart.Name} General Ledger", chart.BaseCurrency, actor, now);
        var period = AccountingPeriod.Create(request.SchemeId, request.InitialPeriodName, BusinessDate.From(request.InitialPeriodStart), BusinessDate.From(request.InitialPeriodEnd), actor, now);

        _dbContext.ChartOfAccounts.Add(chart);
        _dbContext.Ledgers.Add(ledger);
        _dbContext.AccountingPeriods.Add(period);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapChart(chart, ledger, period);
        await WriteAuditAsync(AuditEventType.Created, "ChartOfAccountsCreated", "ChartOfAccounts", chart.Id.ToString(), null, Snapshot(dto), "Chart of accounts created.", cancellationToken);
        return dto;
    }

    public async Task<JournalDto> CreateManualJournalAsync(CreateManualJournalRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var postingDate = BusinessDate.From(request.PostingDate);
        await EnsureSchemeActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var ledger = await GetLedgerAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var period = await GetPeriodForDateAsync(request.SchemeId, postingDate, cancellationToken);
        if (period.Status == AccountingPeriodStatus.Locked)
        {
            throw Validation("postingDate", "Locked accounting periods cannot accept journals.");
        }

        if (request.IsCorrection)
        {
            if (!request.CorrectedJournalId.HasValue)
            {
                throw Validation("correctedJournalId", "Correction journals must reference the original journal.");
            }

            _ = await _dbContext.Journals.AsNoTracking().FirstOrDefaultAsync(journal => journal.Id == request.CorrectedJournalId.Value, cancellationToken)
                ?? throw new NotFoundException("Corrected journal not found.");
        }

        var journal = Journal.CreateManual(
            request.SchemeId,
            request.SchemeClassId,
            ledger.Id,
            period.Id,
            postingDate,
            request.Currency,
            request.Description,
            request.PostingDate < DateOnly.FromDateTime(now),
            request.IsCorrection,
            request.CorrectedJournalId,
            actor,
            now);

        await AddJournalLinesAsync(journal, request.Lines, cancellationToken);
        try
        {
            journal.EnsureBalanced();
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("journal", ex.Message);
        }

        _dbContext.Journals.Add(journal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = await MapJournalAsync(journal, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "ManualJournalCreated", "Journal", journal.Id.ToString(), null, Snapshot(dto), "Manual journal created.", cancellationToken);
        return dto;
    }

    public async Task<JournalDto> CreateAutomatedJournalAsync(CreateAutomatedJournalRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var postingDate = BusinessDate.From(request.PostingDate);
        var automatedType = ParseEnum<AutomatedJournalType>(request.AutomatedJournalType, "automatedJournalType");
        await EnsureSchemeActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var ledger = await GetLedgerAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var period = await GetPeriodForDateAsync(request.SchemeId, postingDate, cancellationToken);
        if (period.Status != AccountingPeriodStatus.Open)
        {
            throw Validation("postingDate", "Automated journals can only post into an open accounting period.");
        }

        var journal = Journal.CreateAutomated(
            request.SchemeId,
            request.SchemeClassId,
            ledger.Id,
            period.Id,
            automatedType,
            postingDate,
            request.Currency,
            request.Description,
            request.OriginatingEventType,
            request.OriginatingEventId,
            actor,
            now);

        await AddJournalLinesAsync(journal, request.Lines, cancellationToken);
        try
        {
            journal.ApproveAndPost(actor, now, "Automated journal posted.");
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("journal", ex.Message);
        }

        _dbContext.Journals.Add(journal);
        var ledgerEntries = journal.Lines.Select(line => LedgerEntry.Create(journal, line, now)).ToList();
        _dbContext.LedgerEntries.AddRange(ledgerEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await CreateAutomationCompanionRecordsAsync(journal, ledgerEntries, automatedType, request.Description, actor, now, cancellationToken);
        var dto = await MapJournalAsync(journal, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "AutomatedJournalCreated", "Journal", journal.Id.ToString(), null, Snapshot(dto), $"Automated journal linked to {request.OriginatingEventType}:{request.OriginatingEventId}.", cancellationToken);
        await WriteAuditAsync(AuditEventType.Approved, "AutomatedJournalPosted", "Journal", journal.Id.ToString(), null, Snapshot(dto), "Automated journal posted.", cancellationToken);
        return dto;
    }

    public async Task<JournalDto> SubmitJournalAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var journal = await LoadJournalForUpdateAsync(id, cancellationToken);

        try
        {
            journal.Submit(actor, now, request.Comment);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("journal", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = await MapJournalAsync(journal, cancellationToken);
        await WriteAuditAsync(AuditEventType.Submitted, "JournalSubmitted", "Journal", journal.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Journal submitted.", cancellationToken);
        return dto;
    }

    public async Task<JournalDto> ApproveJournalAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var journal = await LoadJournalForUpdateAsync(id, cancellationToken);
        var period = await _dbContext.AccountingPeriods.FirstOrDefaultAsync(item => item.Id == journal.AccountingPeriodId, cancellationToken)
            ?? throw new NotFoundException("Accounting period not found.");

        if (period.Status == AccountingPeriodStatus.Locked)
        {
            throw Validation("accountingPeriod", "Locked accounting periods cannot accept postings.");
        }

        if (period.Status == AccountingPeriodStatus.Closed && !journal.IsCorrection)
        {
            throw Validation("accountingPeriod", "Closed period blocks posting unless the journal is an approved correction workflow.");
        }

        try
        {
            journal.ApproveAndPost(actor, now, request.Comment);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("journal", ex.Message);
        }

        var ledgerEntries = journal.Lines.Select(line => LedgerEntry.Create(journal, line, now)).ToList();
        _dbContext.LedgerEntries.AddRange(ledgerEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = await MapJournalAsync(journal, cancellationToken);
        await WriteAuditAsync(AuditEventType.Approved, "JournalApprovedAndPosted", "Journal", journal.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Journal approved and posted.", cancellationToken);
        return dto;
    }

    public async Task<GeneralLedgerDto> GetGeneralLedgerAsync(Guid schemeId, Guid? schemeClassId, Guid? accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var entries = await GetLedgerEntriesAsync(schemeId, schemeClassId, accountId, fromDate, toDate, cancellationToken);
        return new GeneralLedgerDto(
            schemeId,
            schemeClassId,
            fromDate,
            toDate,
            entries.Sum(entry => entry.Debit),
            entries.Sum(entry => entry.Credit),
            entries);
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(Guid schemeId, Guid? schemeClassId, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var entries = await GetLedgerEntriesAsync(schemeId, schemeClassId, null, null, asOfDate, cancellationToken);
        var accounts = await _dbContext.Accounts.AsNoTracking().Where(account => account.SchemeId == schemeId).ToListAsync(cancellationToken);
        var accountMap = accounts.ToDictionary(account => account.Id);
        var lines = entries
            .GroupBy(entry => entry.AccountId)
            .Select(group => BuildTrialBalanceLine(group.Key, group.Sum(entry => entry.Debit), group.Sum(entry => entry.Credit), accountMap[group.Key]))
            .OrderBy(line => line.AccountCode)
            .ToList();

        return new TrialBalanceDto(schemeId, schemeClassId, asOfDate, lines.Sum(line => line.Debit), lines.Sum(line => line.Credit), lines.Sum(line => line.Debit) == lines.Sum(line => line.Credit), lines);
    }

    public async Task<JournalRegisterDto> GetJournalRegisterAsync(Guid? schemeId, Guid? schemeClassId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Journals.Include(journal => journal.Lines).AsNoTracking().AsQueryable();
        if (schemeId.HasValue)
        {
            query = query.Where(journal => journal.SchemeId == schemeId.Value);
        }

        if (schemeClassId.HasValue)
        {
            query = query.Where(journal => journal.SchemeClassId == schemeClassId.Value);
        }

        var journals = await query.ToListAsync(cancellationToken);
        journals = journals
            .Where(journal => !fromDate.HasValue || journal.PostingDate.Value >= fromDate.Value)
            .Where(journal => !toDate.HasValue || journal.PostingDate.Value <= toDate.Value)
            .OrderByDescending(journal => journal.PostingDate.Value)
            .ThenByDescending(journal => journal.CreatedAtUtc)
            .ToList();

        var mapped = new List<JournalDto>();
        foreach (var journal in journals)
        {
            mapped.Add(await MapJournalAsync(journal, cancellationToken));
        }

        return new JournalRegisterDto(schemeId, schemeClassId, fromDate, toDate, mapped);
    }

    public async Task<AccountingPeriodDto> ClosePeriodAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var period = await _dbContext.AccountingPeriods.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Accounting period not found.");

        try
        {
            period.Close(actor, now);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("period", ex.Message);
        }

        var trialBalance = await GetTrialBalanceAsync(period.SchemeId, null, period.EndDate.Value, cancellationToken);
        var payload = Snapshot(trialBalance);
        _dbContext.TrialBalances.Add(TrialBalance.Create(period.SchemeId, null, period.EndDate, trialBalance.TotalDebits, trialBalance.TotalCredits, payload, now));
        _dbContext.FinancialStatements.Add(FinancialStatement.Create(period.SchemeId, null, period.Id, FinancialStatementType.TrialBalance, payload, now));
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapPeriod(period);
        await WriteAuditAsync(AuditEventType.Updated, "AccountingPeriodClosed", "AccountingPeriod", period.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Accounting period closed.", cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "TrialBalanceGenerated", "TrialBalance", period.Id.ToString(), null, payload, "Trial balance generated during period close.", cancellationToken);
        return dto;
    }

    public async Task<AccountingNavReconciliationDto> GetNavReconciliationAsync(Guid schemeId, Guid? schemeClassId, DateOnly valuationDate, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var accountingNav = await CalculateAccountingNavAsync(schemeId, schemeClassId, valuationDate, cancellationToken);
        var publications = await _dbContext.NavPublications.AsNoTracking().Where(publication => publication.SchemeId == schemeId).ToListAsync(cancellationToken);
        var publication = publications
            .Where(publication => (!schemeClassId.HasValue || publication.SchemeClassId == schemeClassId.Value) && publication.ValuationDate.Value == valuationDate)
            .OrderByDescending(publication => publication.VersionNumber)
            .FirstOrDefault();

        var publishedNav = publication?.PublishedNav;
        var difference = publishedNav.HasValue ? accountingNav - publishedNav.Value : accountingNav;
        var status = !publishedNav.HasValue
            ? AccountingNavReconciliationStatus.PendingPublishedNav
            : difference == 0m
                ? AccountingNavReconciliationStatus.Balanced
                : AccountingNavReconciliationStatus.Variance;

        var reconciliation = AccountingNavReconciliation.Create(schemeId, schemeClassId, BusinessDate.From(valuationDate), accountingNav, publishedNav, difference, status, now);
        _dbContext.AccountingNavReconciliations.Add(reconciliation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapNavReconciliation(reconciliation);
        await WriteAuditAsync(AuditEventType.Created, "AccountingNavReconciliationGenerated", "AccountingNavReconciliation", reconciliation.Id.ToString(), null, Snapshot(dto), "Accounting NAV reconciliation generated.", cancellationToken);
        return dto;
    }

    private async Task AddJournalLinesAsync(Journal journal, IReadOnlyCollection<CreateJournalLineRequest>? lineRequests, CancellationToken cancellationToken)
    {
        if (lineRequests is null || lineRequests.Count < 2)
        {
            throw Validation("lines", "A journal requires at least two lines.");
        }

        var accountIds = lineRequests.Select(line => line.AccountId).ToArray();
        var accounts = await _dbContext.Accounts.AsNoTracking().Where(account => accountIds.Contains(account.Id)).ToListAsync(cancellationToken);
        foreach (var lineRequest in lineRequests)
        {
            var account = accounts.FirstOrDefault(item => item.Id == lineRequest.AccountId)
                ?? throw new NotFoundException("Journal line account not found.");
            if (account.SchemeId != journal.SchemeId || account.Status != AccountStatus.Active)
            {
                throw Validation("accountId", "Journal line account must be active and belong to the journal scheme.");
            }

            journal.AddLine(lineRequest.AccountId, lineRequest.Description, lineRequest.Debit, lineRequest.Credit);
        }
    }

    private async Task CreateAutomationCompanionRecordsAsync(
        Journal journal,
        IReadOnlyCollection<LedgerEntry> ledgerEntries,
        AutomatedJournalType automatedType,
        string reason,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (automatedType == AutomatedJournalType.FairValueAdjustment)
        {
            var amount = journal.TotalDebits;
            _dbContext.FairValueAdjustments.Add(FairValueAdjustment.Create(journal.Id, journal.SchemeId, journal.SchemeClassId, journal.PostingDate, amount, reason, actor, now));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (automatedType == AutomatedJournalType.SuspenseResolution)
        {
            foreach (var entry in ledgerEntries)
            {
                _dbContext.SuspenseLedgerEntries.Add(SuspenseLedgerEntry.Create(journal.Id, entry.Id, reason, now));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static void AddDefaultTemplates(ChartOfAccounts chart)
    {
        var accounts = chart.Accounts.ToList();
        var asset = PreferredAccount(accounts, AccountType.Asset);
        var secondAsset = accounts.FirstOrDefault(account => account.AccountType == AccountType.Asset && account.Id != asset?.Id) ?? asset;
        var liability = PreferredAccount(accounts, AccountType.Liability);
        var equity = PreferredAccount(accounts, AccountType.Equity);
        var income = PreferredAccount(accounts, AccountType.Income);
        var expense = PreferredAccount(accounts, AccountType.Expense);

        AddTemplateIfPossible(chart, AutomatedJournalType.Subscription, "Subscription journal", asset, equity ?? liability);
        AddTemplateIfPossible(chart, AutomatedJournalType.Redemption, "Redemption journal", equity ?? liability, asset);
        AddTemplateIfPossible(chart, AutomatedJournalType.InvestmentPlacement, "Investment placement journal", secondAsset, asset);
        AddTemplateIfPossible(chart, AutomatedJournalType.Maturity, "Maturity journal", asset, secondAsset);
        AddTemplateIfPossible(chart, AutomatedJournalType.IncomeReceipt, "Income receipt journal", asset, income);
        AddTemplateIfPossible(chart, AutomatedJournalType.FeeAccrual, "Fee accrual journal", expense, liability);
        AddTemplateIfPossible(chart, AutomatedJournalType.Tax, "Tax journal", expense, liability);
        AddTemplateIfPossible(chart, AutomatedJournalType.Distribution, "Distribution journal", equity ?? expense, asset);
        AddTemplateIfPossible(chart, AutomatedJournalType.SuspenseResolution, "Suspense resolution journal", asset, liability);
        AddTemplateIfPossible(chart, AutomatedJournalType.FairValueAdjustment, "Fair value adjustment journal", secondAsset, income ?? equity);
    }

    private static Account? PreferredAccount(IReadOnlyCollection<Account> accounts, AccountType accountType)
    {
        return accounts.FirstOrDefault(account => account.AccountType == accountType);
    }

    private static void AddTemplateIfPossible(ChartOfAccounts chart, AutomatedJournalType type, string name, Account? debitAccount, Account? creditAccount)
    {
        if (debitAccount is not null && creditAccount is not null)
        {
            chart.AddJournalTemplate(type, name, debitAccount.Code, creditAccount.Code);
        }
    }

    private async Task EnsureSchemeActiveAsync(Guid schemeId, Guid? schemeClassId, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes.Include(scheme => scheme.Classes).AsNoTracking().FirstOrDefaultAsync(scheme => scheme.Id == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme not found.");
        if (scheme.Status != SchemeStatus.Active)
        {
            throw Validation("schemeId", "Scheme must be active for accounting.");
        }

        if (schemeClassId.HasValue && scheme.Classes.All(schemeClass => schemeClass.Id != schemeClassId.Value || !schemeClass.IsActive))
        {
            throw new NotFoundException("Scheme class not found or inactive.");
        }
    }

    private async Task<Ledger> GetLedgerAsync(Guid schemeId, Guid? schemeClassId, CancellationToken cancellationToken)
    {
        var ledgers = await _dbContext.Ledgers.Where(ledger => ledger.SchemeId == schemeId).ToListAsync(cancellationToken);
        var ledger = ledgers.FirstOrDefault(item => item.SchemeClassId == schemeClassId)
            ?? ledgers.FirstOrDefault(item => item.SchemeClassId == null)
            ?? throw new NotFoundException("Ledger not found for scheme.");
        if (ledger.Status != LedgerStatus.Active)
        {
            throw Validation("ledger", "Ledger is not active.");
        }

        return ledger;
    }

    private async Task<AccountingPeriod> GetPeriodForDateAsync(Guid schemeId, BusinessDate postingDate, CancellationToken cancellationToken)
    {
        var periods = await _dbContext.AccountingPeriods.Where(period => period.SchemeId == schemeId).ToListAsync(cancellationToken);
        return periods.FirstOrDefault(period => period.Contains(postingDate))
            ?? throw Validation("postingDate", "Posting date is not inside a configured accounting period.");
    }

    private async Task<Journal> LoadJournalForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Journals.Include(journal => journal.Lines).FirstOrDefaultAsync(journal => journal.Id == id, cancellationToken)
            ?? throw new NotFoundException("Journal not found.");
    }

    private async Task<IReadOnlyCollection<LedgerEntryDto>> GetLedgerEntriesAsync(Guid schemeId, Guid? schemeClassId, Guid? accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        var query = _dbContext.LedgerEntries.AsNoTracking().Where(entry => entry.SchemeId == schemeId);
        if (schemeClassId.HasValue)
        {
            query = query.Where(entry => entry.SchemeClassId == schemeClassId.Value);
        }

        if (accountId.HasValue)
        {
            query = query.Where(entry => entry.AccountId == accountId.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);
        entries = entries
            .Where(entry => !fromDate.HasValue || entry.PostingDate.Value >= fromDate.Value)
            .Where(entry => !toDate.HasValue || entry.PostingDate.Value <= toDate.Value)
            .OrderBy(entry => entry.PostingDate.Value)
            .ThenBy(entry => entry.PostedAtUtc)
            .ToList();

        var accounts = await _dbContext.Accounts.AsNoTracking().Where(account => entries.Select(entry => entry.AccountId).Contains(account.Id)).ToDictionaryAsync(account => account.Id, cancellationToken);
        return entries.Select(entry => MapLedgerEntry(entry, accounts[entry.AccountId])).ToList();
    }

    private async Task<decimal> CalculateAccountingNavAsync(Guid schemeId, Guid? schemeClassId, DateOnly valuationDate, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.LedgerEntries.AsNoTracking().Where(entry => entry.SchemeId == schemeId).ToListAsync(cancellationToken);
        entries = entries
            .Where(entry => (!schemeClassId.HasValue || entry.SchemeClassId == schemeClassId.Value) && entry.PostingDate.Value <= valuationDate)
            .ToList();
        var accountIds = entries.Select(entry => entry.AccountId).Distinct().ToArray();
        var accounts = await _dbContext.Accounts.AsNoTracking().Where(account => accountIds.Contains(account.Id)).ToDictionaryAsync(account => account.Id, cancellationToken);

        var assets = entries.Where(entry => accounts[entry.AccountId].AccountType == AccountType.Asset).Sum(entry => entry.Debit - entry.Credit);
        var liabilities = entries.Where(entry => accounts[entry.AccountId].AccountType == AccountType.Liability).Sum(entry => entry.Credit - entry.Debit);
        return assets - liabilities;
    }

    private static TrialBalanceLineDto BuildTrialBalanceLine(Guid accountId, decimal rawDebits, decimal rawCredits, Account account)
    {
        var balance = account.NormalBalance == LedgerEntryDirection.Debit
            ? rawDebits - rawCredits
            : rawCredits - rawDebits;
        var debit = account.NormalBalance == LedgerEntryDirection.Debit
            ? Math.Max(balance, 0m)
            : Math.Max(-balance, 0m);
        var credit = account.NormalBalance == LedgerEntryDirection.Credit
            ? Math.Max(balance, 0m)
            : Math.Max(-balance, 0m);
        return new TrialBalanceLineDto(accountId, account.Code, account.Name, account.AccountType.ToString(), debit, credit);
    }

    private static LedgerEntryDto MapLedgerEntry(LedgerEntry entry, Account account)
    {
        return new LedgerEntryDto(entry.Id, entry.JournalId, entry.JournalLineId, entry.LedgerId, entry.AccountId, account.Code, account.Name, account.AccountType.ToString(), entry.SchemeId, entry.SchemeClassId, entry.PostingDate.Value, entry.Description, entry.Debit, entry.Credit, entry.Currency, entry.OriginatingEventType, entry.OriginatingEventId, entry.PostedAtUtc);
    }

    private ChartOfAccountsDto MapChart(ChartOfAccounts chart, Ledger ledger, AccountingPeriod period)
    {
        return new ChartOfAccountsDto(chart.Id, chart.SchemeId, chart.Code, chart.Name, chart.BaseCurrency, chart.IsActive, MapLedger(ledger), MapPeriod(period), chart.Accounts.OrderBy(account => account.Code).Select(MapAccount).ToList(), chart.JournalTemplates.OrderBy(template => template.JournalType).Select(MapTemplate).ToList());
    }

    private static AccountDto MapAccount(Account account)
    {
        return new AccountDto(account.Id, account.ChartOfAccountsId, account.SchemeId, account.Code, account.Name, account.AccountType.ToString(), account.NormalBalance.ToString(), account.IsControlAccount, account.Status.ToString());
    }

    private static LedgerDto MapLedger(Ledger ledger)
    {
        return new LedgerDto(ledger.Id, ledger.SchemeId, ledger.SchemeClassId, ledger.ChartOfAccountsId, ledger.Code, ledger.Name, ledger.Currency, ledger.Status.ToString());
    }

    private static AccountingPeriodDto MapPeriod(AccountingPeriod period)
    {
        return new AccountingPeriodDto(period.Id, period.SchemeId, period.Name, period.StartDate.Value, period.EndDate.Value, period.Status.ToString(), period.ClosedByUserId, period.ClosedAtUtc);
    }

    private static JournalTemplateDto MapTemplate(JournalTemplate template)
    {
        return new JournalTemplateDto(template.Id, template.JournalType.ToString(), template.Name, template.DebitAccountCode, template.CreditAccountCode, template.IsActive);
    }

    private async Task<JournalDto> MapJournalAsync(Journal journal, CancellationToken cancellationToken)
    {
        var lines = journal.Lines.Count > 0
            ? journal.Lines
            : await _dbContext.JournalLines.AsNoTracking().Where(line => line.JournalId == journal.Id).OrderBy(line => line.LineNumber).ToListAsync(cancellationToken);
        return new JournalDto(journal.Id, journal.SchemeId, journal.SchemeClassId, journal.LedgerId, journal.AccountingPeriodId, journal.JournalNumber, journal.Source.ToString(), journal.AutomatedJournalType?.ToString(), journal.Status.ToString(), journal.PostingDate.Value, journal.Currency, journal.Description, journal.OriginatingEventType, journal.OriginatingEventId, journal.IsBackdated, journal.IsCorrection, journal.CorrectedJournalId, journal.TotalDebits, journal.TotalCredits, journal.CreatedByUserId, journal.CreatedAtUtc, journal.SubmittedByUserId, journal.SubmittedAtUtc, journal.ApprovedByUserId, journal.ApprovedAtUtc, journal.PostedByUserId, journal.PostedAtUtc, lines.OrderBy(line => line.LineNumber).Select(MapLine).ToList());
    }

    private static JournalLineDto MapLine(JournalLine line)
    {
        return new JournalLineDto(line.Id, line.AccountId, line.LineNumber, line.Description, line.Debit, line.Credit, line.Currency);
    }

    private static AccountingNavReconciliationDto MapNavReconciliation(AccountingNavReconciliation reconciliation)
    {
        return new AccountingNavReconciliationDto(reconciliation.Id, reconciliation.SchemeId, reconciliation.SchemeClassId, reconciliation.ValuationDate.Value, reconciliation.AccountingNav, reconciliation.PublishedNav, reconciliation.Difference, reconciliation.Status.ToString(), reconciliation.GeneratedAtUtc);
    }

    private string CurrentUserIdOrThrow()
    {
        var userId = _currentUserContext.UserId;
        return string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("User context is not available.") : userId;
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(ModuleName, action, entityName, entityId, eventType, CurrentUserIdOrThrow(), _currentUserContext.DisplayName, _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles), _currentUserContext.CorrelationId, _currentUserContext.IpAddress, _currentUserContext.UserAgent, reason, null, beforeJson, afterJson, reason, null, null), cancellationToken);
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

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }

    private static string Snapshot<T>(T? obj)
    {
        return obj is null ? "{}" : JsonSerializer.Serialize(obj);
    }
}
