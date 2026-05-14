using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Cash;
using Cis.Domain.Audit;
using Cis.Domain.Cash;
using Cis.Domain.Dealing;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Cash;

internal sealed class CashService : ICashService
{
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CashService(
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

    public async Task<BankStatementImportDto> ImportBankStatementAsync(ImportBankStatementRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ImportBankStatementCoreAsync(request, idempotencyKey, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("cashImport", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "cashImport", exception.Message);
        }
    }

    public async Task<BankStatementImportDto> GetBankStatementImportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var import = await _dbContext.BankStatementImports
            .AsNoTracking()
            .Include(item => item.Lines)
            .SingleAsync(item => item.Id == id, cancellationToken);

        return await MapImportAsync(import, cancellationToken);
    }

    public async Task<ReconciliationRunDto> CreateReconciliationRunAsync(ReconciliationRunRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            var run = ReconciliationRun.Create(request.SchemeBankAccountId, request.RunDate, request.AgingThresholdDays, userId, now);

            var matchedCount = await _dbContext.BankStatementLines
                .AsNoTracking()
                .Where(line => line.SchemeBankAccountId == request.SchemeBankAccountId && line.MatchStatus == BankStatementMatchStatus.Matched)
                .CountAsync(cancellationToken);

            var suspenseQuery = _dbContext.SuspenseItems.AsNoTracking().Where(item => item.SchemeBankAccountId == request.SchemeBankAccountId);
            var suspenseCount = await suspenseQuery.CountAsync(item => item.Status == SuspenseItemStatus.Open, cancellationToken);
            var agedThresholdDateTime = request.RunDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-request.AgingThresholdDays);
            var agedBreakCount = await suspenseQuery.CountAsync(item => item.Status == SuspenseItemStatus.Open && item.OpenedAtUtc <= agedThresholdDateTime, cancellationToken);
            var breakCount = suspenseCount;
            run.Complete(matchedCount, suspenseCount, breakCount, agedBreakCount, userId, now, "Reconciliation run completed.");
            _dbContext.ReconciliationRuns.Add(run);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(AuditEventType.Created, "CashReconciliationRunCreated", "ReconciliationRun", run.Id.ToString(), null, Snapshot(run), "Reconciliation run completed.", cancellationToken);
            return MapReconciliationRun(run);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reconciliationRun", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "reconciliationRun", exception.Message);
        }
    }

    public async Task<ReconciliationRunDto> GetReconciliationRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.ReconciliationRuns.AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return MapReconciliationRun(run);
    }

    public async Task<IReadOnlyCollection<SuspenseItemDto>> GetSuspenseAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SuspenseItems
            .AsNoTracking()
            .OrderByDescending(item => item.OpenedAtUtc)
            .Select(item => new SuspenseItemDto(
                item.Id,
                item.BankStatementLineId,
                item.SchemeBankAccountId,
                item.Amount,
                item.Currency,
                item.Reference,
                item.Reason,
                item.Status.ToString(),
                item.OpenedByUserId,
                item.OpenedAtUtc,
                item.ResolvedByUserId,
                item.ResolvedAtUtc,
                item.ResolutionComment,
                item.PaymentInstructionId,
                item.CashBookEntryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<SuspenseItemDto> ResolveSuspenseAsync(Guid id, ResolveSuspenseRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var suspense = await _dbContext.SuspenseItems.SingleAsync(item => item.Id == id, cancellationToken);
            var userId = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            suspense.Resolve(userId, now, request.ResolutionComment, request.PaymentInstructionId, request.CashBookEntryId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(AuditEventType.Updated, "CashSuspenseResolved", "SuspenseItem", suspense.Id.ToString(), null, Snapshot(suspense), request.ResolutionComment ?? "Suspense resolved.", cancellationToken);
            return MapSuspenseItem(suspense);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("suspense", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "suspense", exception.Message);
        }
    }

    public async Task<PaymentInstructionDto> CreatePaymentInstructionAsync(CreatePaymentInstructionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            var paymentType = ParsePaymentType(request.PaymentType);
            var instruction = PaymentInstruction.Create(
                request.InvestorId,
                request.SchemeId,
                request.SchemeClassId,
                request.SchemeBankAccountId,
                request.Amount,
                request.Currency,
                request.Reference,
                paymentType,
                request.RelatedDealingInstructionId,
                userId,
                now,
                null);

            _dbContext.PaymentInstructions.Add(instruction);
            _dbContext.CashBookEntries.Add(CashBookEntry.Create(
                request.SchemeBankAccountId,
                request.InvestorId,
                request.SchemeId,
                request.SchemeClassId,
                DateOnly.FromDateTime(now),
                request.Amount,
                request.Currency,
                CashBookEntryDirection.Outbound,
                CashBookEntrySourceType.PaymentInstruction,
                instruction.Id,
                request.Reference,
                $"Payment instruction {request.Reference}.",
                userId,
                now));
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(AuditEventType.Created, "CashPaymentInstructionCreated", "PaymentInstruction", instruction.Id.ToString(), null, Snapshot(instruction), request.Reference, cancellationToken);
            return await MapPaymentInstructionAsync(instruction.Id, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("paymentInstruction", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "paymentInstruction", exception.Message);
        }
    }

    public async Task<PaymentInstructionDto> UpdatePaymentStatusAsync(Guid id, UpdatePaymentStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var instruction = await _dbContext.PaymentInstructions.Include(payment => payment.StatusEvents).SingleAsync(payment => payment.Id == id, cancellationToken);
            var userId = CurrentUserIdOrThrow();
            var status = ParsePaymentStatus(request.Status);
            var eventType = ParsePaymentStatusEventType(status);
            instruction.AddStatusEvent(status, eventType, request.OccurredAtUtc, request.Reason, request.ExternalReference, userId);

            if (status == PaymentInstructionStatus.Returned)
            {
                _dbContext.ReturnedFunds.Add(ReturnedFund.Create(instruction.Id, instruction.Amount, instruction.Currency, request.Reason ?? "Returned by bank.", userId, request.OccurredAtUtc));
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(AuditEventType.Updated, "CashPaymentStatusUpdated", "PaymentInstruction", instruction.Id.ToString(), null, Snapshot(instruction), request.Reason ?? status.ToString(), cancellationToken);
            return await MapPaymentInstructionAsync(instruction.Id, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("paymentInstruction", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "paymentInstruction", exception.Message);
        }
    }

    public async Task<ReversalRequestDto> CreateReversalAsync(CreateReversalRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            var reversal = ReversalRequest.Create(request.PaymentInstructionId, request.Reason, userId, now);
            _dbContext.ReversalRequests.Add(reversal);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(AuditEventType.Created, "CashReversalRequested", "ReversalRequest", reversal.Id.ToString(), null, Snapshot(reversal), request.Reason, cancellationToken);
            return MapReversalRequest(reversal);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reversalRequest", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "reversalRequest", exception.Message);
        }
    }

    public async Task<ReversalRequestDto> ApproveReversalAsync(Guid id, ApproveReversalRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var reversal = await _dbContext.ReversalRequests.SingleAsync(item => item.Id == id, cancellationToken);
            var payment = await _dbContext.PaymentInstructions.SingleAsync(item => item.Id == reversal.PaymentInstructionId, cancellationToken);
            var userId = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            reversal.Approve(userId, now, request.Comment);
            payment.AddStatusEvent(PaymentInstructionStatus.Reversed, PaymentStatusEventType.Reversed, now, request.Comment ?? "Payment reversed.", null, userId);
            _dbContext.CashBookEntries.Add(CashBookEntry.Create(
                payment.SchemeBankAccountId,
                payment.InvestorId,
                payment.SchemeId,
                payment.SchemeClassId,
                DateOnly.FromDateTime(now),
                payment.Amount,
                payment.Currency,
                CashBookEntryDirection.Inbound,
                CashBookEntrySourceType.Reversal,
                reversal.Id,
                payment.Reference,
                request.Comment ?? "Reversal approved.",
                userId,
                now));
            await _dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(AuditEventType.Approved, "CashReversalApproved", "ReversalRequest", reversal.Id.ToString(), null, Snapshot(reversal), request.Comment ?? "Reversal approved.", cancellationToken);
            return MapReversalRequest(reversal);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reversalRequest", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "reversalRequest", exception.Message);
        }
    }

    private async Task<BankStatementImportDto> ImportBankStatementCoreAsync(ImportBankStatementRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var userId = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);
        var sourceHash = ComputeHash(request.CsvContent);
        var schemeBankAccount = await _dbContext.SchemeBankAccounts.AsNoTracking().SingleAsync(item => item.Id == request.SchemeBankAccountId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedIdempotencyKey))
        {
            var existing = await _dbContext.BankStatementImports.AsNoTracking().Include(item => item.Lines).FirstOrDefaultAsync(item => item.IdempotencyKey == normalizedIdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.SourceHash != sourceHash || existing.ImportedByUserId != userId || existing.SchemeBankAccountId != request.SchemeBankAccountId)
                {
                    throw new ConflictException("Idempotency key cannot be reused with a different bank statement payload.");
                }

                return await MapImportAsync(existing, cancellationToken);
            }
        }

        var rows = ParseCsv(request.CsvContent);
        if (rows.Count == 0)
        {
            throw Validation("csvContent", "The CSV file must contain at least one data row.");
        }

        var importId = Guid.NewGuid();
        var lines = rows.Select(row => BankStatementLine.Create(
            importId,
            request.SchemeBankAccountId,
            row.LineNumber,
            row.TransactionDate,
            row.Reference,
            row.Description,
            row.Amount,
            row.Direction,
            row.InvestorId,
            row.SchemeId,
            row.SchemeClassId)).ToList();

        var import = BankStatementImport.Create(request.SchemeBankAccountId, request.FileName, request.StatementDate, request.DateToleranceDays, normalizedIdempotencyKey, sourceHash, userId, now, lines);
        _dbContext.BankStatementImports.Add(import);

        var matchedCount = 0;
        var suspenseCount = 0;
        var matchedPaymentIds = new List<Guid>();
        foreach (var line in lines)
        {
            if (line.Direction == BankStatementLineDirection.Credit)
            {
                var matched = await TryMatchSubscriptionAsync(import, schemeBankAccount, line, userId, now, cancellationToken);
                if (matched)
                {
                    matchedCount++;
                    continue;
                }
            }
            else
            {
                var matchedPaymentId = await TryMatchPaymentAsync(import, line, userId, now, cancellationToken);
                if (matchedPaymentId.HasValue)
                {
                    matchedCount++;
                    matchedPaymentIds.Add(matchedPaymentId.Value);
                    continue;
                }
            }

            suspenseCount++;
            var suspense = SuspenseItem.Create(line.Id, request.SchemeBankAccountId, line.Amount, schemeBankAccount.Currency, line.Reference, $"Unmatched bank statement line {line.LineNumber}.", userId, now);
            line.MarkSuspense(suspense.Id, userId, now);
            _dbContext.SuspenseItems.Add(suspense);
        }

        import.MarkProcessed(matchedCount, suspenseCount);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var entries = string.Join(", ", exception.Entries.Select(entry => entry.Metadata.ClrType.Name));
            throw new ConflictException($"Cash bank statement import concurrency conflict on: {entries}.");
        }

        await WriteAuditAsync(AuditEventType.Created, "CashBankStatementImported", "BankStatementImport", import.Id.ToString(), null, Snapshot(import), request.FileName, cancellationToken, normalizedIdempotencyKey);
        foreach (var paymentId in matchedPaymentIds)
        {
            var payment = await MapPaymentInstructionAsync(paymentId, cancellationToken);
            await WriteAuditAsync(AuditEventType.Updated, "CashPaymentStatusUpdated", "PaymentInstruction", paymentId.ToString(), null, Snapshot(payment), "Payment confirmed from bank statement.", cancellationToken);
        }

        return await MapImportAsync(import, cancellationToken);
    }

    private async Task<bool> TryMatchSubscriptionAsync(BankStatementImport import, SchemeBankAccount schemeBankAccount, BankStatementLine line, string userId, DateTime now, CancellationToken cancellationToken)
    {
        if (!line.InvestorId.HasValue || !line.SchemeId.HasValue || !line.SchemeClassId.HasValue)
        {
            return false;
        }

        var candidate = await _dbContext.DealingInstructions
            .Include(instruction => instruction.SubscriptionInstructions)
            .SingleOrDefaultAsync(instruction =>
                instruction.Status == DealingInstructionStatus.PendingFunds &&
                instruction.InvestorId == line.InvestorId &&
                instruction.SchemeId == line.SchemeId &&
                instruction.SchemeClassId == line.SchemeClassId &&
                instruction.InstructionNumber == line.Reference,
                cancellationToken);

        if (candidate is null)
        {
            return false;
        }

        var subscription = candidate.SubscriptionInstructions.Single();
        if (!subscription.Amount.HasValue || subscription.Amount.Value != line.Amount)
        {
            return false;
        }

        var dayDelta = Math.Abs(candidate.BusinessDate.Value.DayNumber - line.TransactionDate.DayNumber);
        if (dayDelta > import.DateToleranceDays)
        {
            return false;
        }

        candidate.MarkCashVerified(userId, now);
        var cashBookEntry = CashBookEntry.Create(
            import.SchemeBankAccountId,
            candidate.InvestorId,
            candidate.SchemeId,
            candidate.SchemeClassId,
            line.TransactionDate,
            line.Amount,
            schemeBankAccount.Currency,
            CashBookEntryDirection.Inbound,
            CashBookEntrySourceType.BankStatement,
            line.Id,
            line.Reference,
            line.Description,
            userId,
            now);
        _dbContext.CashBookEntries.Add(cashBookEntry);
        _dbContext.CashMatches.Add(CashMatch.Create(line.Id, cashBookEntry.Id, CashMatchRule.ReferenceAmountDateInvestorSchemeBankAccount, candidate.Id, nameof(DealingInstruction), userId, now));
        line.MarkMatched(cashBookEntry.Id, candidate.Id, nameof(DealingInstruction), CashMatchRule.ReferenceAmountDateInvestorSchemeBankAccount, userId, now);
        return true;
    }

    private async Task<Guid?> TryMatchPaymentAsync(BankStatementImport import, BankStatementLine line, string userId, DateTime now, CancellationToken cancellationToken)
    {
        if (!line.InvestorId.HasValue || !line.SchemeId.HasValue || !line.SchemeClassId.HasValue)
        {
            return null;
        }

        var candidate = await _dbContext.PaymentInstructions
            .SingleOrDefaultAsync(payment =>
                payment.SchemeBankAccountId == import.SchemeBankAccountId &&
                payment.InvestorId == line.InvestorId &&
                payment.SchemeId == line.SchemeId &&
                payment.SchemeClassId == line.SchemeClassId &&
                payment.Reference == line.Reference &&
                payment.Amount == line.Amount,
                cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        var cashBookEntry = await _dbContext.CashBookEntries.SingleAsync(entry => entry.SourceType == CashBookEntrySourceType.PaymentInstruction && entry.SourceEntityId == candidate.Id, cancellationToken);
        candidate.MarkCompletedFromBankMatch(line.Reference, userId, now);
        _dbContext.CashMatches.Add(CashMatch.Create(line.Id, cashBookEntry.Id, CashMatchRule.ReferenceAmountDateInvestorSchemeBankAccount, candidate.Id, nameof(PaymentInstruction), userId, now));
        line.MarkMatched(cashBookEntry.Id, candidate.Id, nameof(PaymentInstruction), CashMatchRule.ReferenceAmountDateInvestorSchemeBankAccount, userId, now);
        return candidate.Id;
    }

    private async Task<BankStatementImportDto> MapImportAsync(BankStatementImport import, CancellationToken cancellationToken)
    {
        var lines = await _dbContext.BankStatementLines
            .AsNoTracking()
            .Where(line => line.BankStatementImportId == import.Id)
            .OrderBy(line => line.LineNumber)
            .Select(line => new BankStatementLineDto(
                line.Id,
                line.LineNumber,
                line.TransactionDate,
                line.Reference,
                line.Description,
                line.Amount,
                line.Direction.ToString(),
                line.InvestorId,
                line.SchemeId,
                line.SchemeClassId,
                line.MatchStatus.ToString(),
                line.CashBookEntryId,
                line.SuspenseItemId,
                line.RelatedEntityId,
                line.RelatedEntityType,
                line.MatchRule.HasValue ? line.MatchRule.Value.ToString() : null))
            .ToListAsync(cancellationToken);

        return new BankStatementImportDto(
            import.Id,
            import.SchemeBankAccountId,
            import.FileName,
            import.StatementDate,
            import.DateToleranceDays,
            import.IdempotencyKey,
            import.Status.ToString(),
            import.ImportedByUserId,
            import.ImportedAtUtc,
            import.LineCount,
            import.MatchedLineCount,
            import.SuspenseLineCount,
            lines);
    }

    private async Task<PaymentInstructionDto> MapPaymentInstructionAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.PaymentInstructions
            .AsNoTracking()
            .Include(item => item.StatusEvents)
            .SingleAsync(item => item.Id == id, cancellationToken);

        return new PaymentInstructionDto(
            payment.Id,
            payment.InvestorId,
            payment.SchemeId,
            payment.SchemeClassId,
            payment.SchemeBankAccountId,
            payment.Amount,
            payment.Currency,
            payment.Reference,
            payment.PaymentType.ToString(),
            payment.Status.ToString(),
            payment.RelatedDealingInstructionId,
            payment.RequestedByUserId,
            payment.RequestedAtUtc,
            payment.IdempotencyKey,
            payment.ExternalReference,
            payment.FailedReason,
            payment.StatusEvents
                .OrderBy(statusEvent => statusEvent.OccurredAtUtc)
                .Select(statusEvent => new PaymentStatusEventDto(
                    statusEvent.Id,
                    statusEvent.Status.ToString(),
                    statusEvent.EventType.ToString(),
                    statusEvent.OccurredAtUtc,
                    statusEvent.Reason,
                    statusEvent.ExternalReference,
                    statusEvent.ChangedByUserId))
                .ToList());
    }

    private static ReconciliationRunDto MapReconciliationRun(ReconciliationRun run)
    {
        return new ReconciliationRunDto(
            run.Id,
            run.SchemeBankAccountId,
            run.RunDate,
            run.AgingThresholdDays,
            run.Status.ToString(),
            run.RequestedByUserId,
            run.RequestedAtUtc,
            run.CompletedAtUtc,
            run.CompletedByUserId,
            run.MatchedCount,
            run.SuspenseCount,
            run.BreakCount,
            run.AgedBreakCount,
            run.Summary);
    }

    private static SuspenseItemDto MapSuspenseItem(SuspenseItem item)
    {
        return new SuspenseItemDto(
            item.Id,
            item.BankStatementLineId,
            item.SchemeBankAccountId,
            item.Amount,
            item.Currency,
            item.Reference,
            item.Reason,
            item.Status.ToString(),
            item.OpenedByUserId,
            item.OpenedAtUtc,
            item.ResolvedByUserId,
            item.ResolvedAtUtc,
            item.ResolutionComment,
            item.PaymentInstructionId,
            item.CashBookEntryId);
    }

    private static ReversalRequestDto MapReversalRequest(ReversalRequest request)
    {
        return new ReversalRequestDto(
            request.Id,
            request.PaymentInstructionId,
            request.Reason,
            request.Status.ToString(),
            request.RequestedByUserId,
            request.RequestedAtUtc,
            request.ApprovedByUserId,
            request.ApprovedAtUtc,
            request.ApprovalComment);
    }


    private static PaymentInstructionStatus ParsePaymentStatus(string status)
    {
        if (!Enum.TryParse<PaymentInstructionStatus>(status, true, out var parsed))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = [$"Unsupported payment status '{status}'."]
            });
        }

        return parsed;
    }

    private static PaymentInstructionType ParsePaymentType(string paymentType)
    {
        if (!Enum.TryParse<PaymentInstructionType>(paymentType, true, out var parsed))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["paymentType"] = [$"Unsupported payment type '{paymentType}'."]
            });
        }

        return parsed;
    }

    private static PaymentStatusEventType ParsePaymentStatusEventType(PaymentInstructionStatus status)
    {
        return status switch
        {
            PaymentInstructionStatus.Sent => PaymentStatusEventType.Sent,
            PaymentInstructionStatus.Completed => PaymentStatusEventType.Completed,
            PaymentInstructionStatus.Failed => PaymentStatusEventType.Failed,
            PaymentInstructionStatus.Returned => PaymentStatusEventType.Returned,
            PaymentInstructionStatus.Reversed => PaymentStatusEventType.Reversed,
            _ => PaymentStatusEventType.Sent
        };
    }

    private static List<ParsedStatementRow> ParseCsv(string csvContent)
    {
        var normalized = csvContent.Replace("\r", string.Empty);
        var rows = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length < 2)
        {
            return [];
        }

        var headers = SplitCsvLine(rows[0]).Select(header => header.Trim()).ToArray();
        var requiredHeaders = new[] { "LineNumber", "TransactionDate", "Reference", "Description", "Amount", "Direction", "InvestorId", "SchemeId", "SchemeClassId" };
        foreach (var header in requiredHeaders)
        {
            if (!headers.Contains(header, StringComparer.OrdinalIgnoreCase))
            {
                throw Validation("csvContent", $"CSV header '{header}' is required.");
            }
        }

        var parsedRows = new List<ParsedStatementRow>();
        for (var rowIndex = 1; rowIndex < rows.Length; rowIndex++)
        {
            var columns = SplitCsvLine(rows[rowIndex]);
            if (columns.Length != headers.Length)
            {
                throw Validation("csvContent", $"CSV row {rowIndex + 1} does not match the header column count.");
            }

            var row = headers.Zip(columns, (header, value) => new { Header = header, Value = value.Trim() })
                .ToDictionary(item => item.Header, item => item.Value, StringComparer.OrdinalIgnoreCase);

            parsedRows.Add(new ParsedStatementRow(
                int.Parse(row["LineNumber"], CultureInfo.InvariantCulture),
                DateOnly.Parse(row["TransactionDate"], CultureInfo.InvariantCulture),
                row["Reference"],
                string.IsNullOrWhiteSpace(row["Description"]) ? null : row["Description"],
                decimal.Parse(row["Amount"], NumberStyles.Number, CultureInfo.InvariantCulture),
                Enum.Parse<BankStatementLineDirection>(row["Direction"], true),
                string.IsNullOrWhiteSpace(row["InvestorId"]) ? null : Guid.Parse(row["InvestorId"]),
                string.IsNullOrWhiteSpace(row["SchemeId"]) ? null : Guid.Parse(row["SchemeId"]),
                string.IsNullOrWhiteSpace(row["SchemeClassId"]) ? null : Guid.Parse(row["SchemeClassId"])));
        }

        return parsedRows;
    }

    private static string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    builder.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(builder.ToString());
                builder.Clear();
                continue;
            }

            builder.Append(character);
        }

        values.Add(builder.ToString());
        return values.ToArray();
    }

    private static string ComputeHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        return string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
    }

    private static ValidationException Validation(string fieldName, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            "Cash",
            action,
            entityName,
            entityId,
            eventType,
            CurrentUserIdOrThrow(),
            _currentUserContext.DisplayName,
            _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles),
            _currentUserContext.CorrelationId,
            _currentUserContext.IpAddress,
            _currentUserContext.UserAgent,
            reason,
            null,
            beforeJson,
            afterJson,
            reason,
            null,
            idempotencyKey),
            cancellationToken);
    }

    private static string? Snapshot<T>(T value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    private sealed record ParsedStatementRow(
        int LineNumber,
        DateOnly TransactionDate,
        string Reference,
        string? Description,
        decimal Amount,
        BankStatementLineDirection Direction,
        Guid? InvestorId,
        Guid? SchemeId,
        Guid? SchemeClassId);
}
