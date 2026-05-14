using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Dealing;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Dealing;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Dealing;

internal sealed class DealingService : IDealingService
{
    private const string ModuleName = "Dealing";
    private const decimal DefaultLargeRedemptionThreshold = 1_000_000m;

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DealingService(
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

    public async Task<DealingInstructionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        var schemeClass = await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var channel = ParseEnum<DealingChannel>(request.Channel, "channel");
        var mode = ParseEnum<DealingInstructionMode>(request.Mode, "mode");
        ValidateSubscriptionMinimum(mode, request.Amount, request.Units, request.ApprovedNavPrice, schemeClass.MinimumContribution);

        var instruction = CreateValidated(() => DealingInstruction.CreateSubscription(
            GenerateInstructionNumber("SUB"),
            request.InvestorId,
            request.SchemeId,
            request.SchemeClassId,
            channel,
            BusinessDate.From(request.BusinessDate),
            EnsureUtc(request.ReceivedAtUtc, nameof(request.ReceivedAtUtc)),
            mode,
            request.Amount,
            request.Units,
            request.Currency,
            request.FundsCleared,
            request.ApprovedNavAvailable,
            request.ApprovedNavPrice,
            request.ApprovedNavDate.HasValue ? BusinessDate.From(request.ApprovedNavDate.Value) : null,
            actor,
            now));

        AddCutOffBreachIfNeeded(instruction, schemeClass.CutOffTime);
        instruction.AddValidationResult("InvestorApproved", "Investor is approved.", ValidationSeverity.Info, true, now);
        instruction.AddValidationResult("MinimumContribution", "Subscription satisfies the scheme class minimum contribution.", ValidationSeverity.Info, true, now);
        if (!request.FundsCleared)
        {
            instruction.AddValidationResult("FundsPending", "Subscription is pending uncleared funds.", ValidationSeverity.Warning, false, now);
        }

        _dbContext.DealingInstructions.Add(instruction);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInstructionAsync(instruction.Id, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "SubscriptionInstructionCreated", "DealingInstruction", instruction.Id.ToString(), null, Snapshot(dto), "Subscription instruction created.", cancellationToken);
        return dto;
    }

    public async Task<DealingInstructionDto> CreateRedemptionAsync(CreateRedemptionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        var schemeClass = await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        var channel = ParseEnum<DealingChannel>(request.Channel, "channel");
        var mode = ParseEnum<DealingInstructionMode>(request.Mode, "mode");
        var lienUnits = await ActiveLienUnitsAsync(request.InvestorId, request.SchemeId, request.SchemeClassId, request.ApprovedNavPrice, cancellationToken);

        var instruction = CreateValidated(() => DealingInstruction.CreateRedemption(
            GenerateInstructionNumber("RED"),
            request.InvestorId,
            request.SchemeId,
            request.SchemeClassId,
            channel,
            BusinessDate.From(request.BusinessDate),
            EnsureUtc(request.ReceivedAtUtc, nameof(request.ReceivedAtUtc)),
            mode,
            request.Amount,
            request.Units,
            request.FullRedemption,
            request.AvailableUnits,
            lienUnits,
            request.LockedUnits,
            request.MinimumBalanceUnits,
            request.LockInDays,
            request.NoticePeriodDays,
            request.ApprovedNavAvailable,
            request.ApprovedNavPrice,
            BusinessDate.From(request.ApprovedNavDate),
            request.ExitFeeRate,
            request.TaxRate,
            actor,
            now));

        var redemption = instruction.RedemptionInstructions.Single();
        var threshold = await ApprovalThresholdAsync(DealingInstructionType.Redemption, request.Currency, cancellationToken);
        if (redemption.GrossAmount >= threshold)
        {
            redemption.MarkLargeRedemption();
            instruction.AddValidationResult("LargeRedemptionThreshold", "Large or exceptional redemption requires approval threshold workflow.", ValidationSeverity.Warning, false, now);
        }

        AddCutOffBreachIfNeeded(instruction, schemeClass.CutOffTime);
        instruction.AddValidationResult("RedeemableBalance", "Redemption is within available redeemable balance after liens and locks.", ValidationSeverity.Info, true, now);

        _dbContext.DealingInstructions.Add(instruction);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInstructionAsync(instruction.Id, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "RedemptionInstructionCreated", "DealingInstruction", instruction.Id.ToString(), null, Snapshot(dto), "Redemption instruction created.", cancellationToken);
        return dto;
    }

    public async Task<DealingInstructionDto> CreateSwitchAsync(CreateSwitchRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        var sourceClass = await EnsureActiveSchemeClassAsync(request.SourceSchemeId, request.SourceSchemeClassId, cancellationToken);
        await EnsureActiveSchemeClassAsync(request.TargetSchemeId, request.TargetSchemeClassId, cancellationToken);

        var instruction = CreateValidated(() => DealingInstruction.CreateSwitch(
            GenerateInstructionNumber("SWI"),
            request.InvestorId,
            request.SourceSchemeId,
            request.SourceSchemeClassId,
            request.TargetSchemeId,
            request.TargetSchemeClassId,
            ParseEnum<DealingChannel>(request.Channel, "channel"),
            BusinessDate.From(request.BusinessDate),
            EnsureUtc(request.ReceivedAtUtc, nameof(request.ReceivedAtUtc)),
            ParseEnum<DealingInstructionMode>(request.Mode, "mode"),
            request.Amount,
            request.Units,
            request.FeeAmount,
            ValidateJson(request.OwnershipHistoryJson, "ownershipHistoryJson"),
            actor,
            now));

        AddCutOffBreachIfNeeded(instruction, sourceClass.CutOffTime);
        instruction.AddValidationResult("SwitchPermitted", "Switch source and target scheme classes are active.", ValidationSeverity.Info, true, now);
        _dbContext.DealingInstructions.Add(instruction);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInstructionAsync(instruction.Id, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "SwitchInstructionCreated", "DealingInstruction", instruction.Id.ToString(), null, Snapshot(dto), "Switch instruction created.", cancellationToken);
        return dto;
    }

    public async Task<DealingInstructionDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.FromInvestorId, cancellationToken);
        await EnsureApprovedInvestorAsync(request.ToInvestorId, cancellationToken);
        var schemeClass = await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var instruction = CreateValidated(() => DealingInstruction.CreateTransfer(
            GenerateInstructionNumber("TRF"),
            request.FromInvestorId,
            request.ToInvestorId,
            request.SchemeId,
            request.SchemeClassId,
            ParseEnum<DealingChannel>(request.Channel, "channel"),
            BusinessDate.From(request.BusinessDate),
            EnsureUtc(request.ReceivedAtUtc, nameof(request.ReceivedAtUtc)),
            request.Units,
            ValidateJson(request.OwnershipHistoryJson, "ownershipHistoryJson"),
            actor,
            now));

        AddCutOffBreachIfNeeded(instruction, schemeClass.CutOffTime);
        instruction.AddValidationResult("TransferPermitted", "Investor-to-investor transfer is permitted for approved investors.", ValidationSeverity.Info, true, now);
        _dbContext.DealingInstructions.Add(instruction);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInstructionAsync(instruction.Id, cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "TransferInstructionCreated", "DealingInstruction", instruction.Id.ToString(), null, Snapshot(dto), "Transfer instruction created.", cancellationToken);
        return dto;
    }

    public async Task<LienDto> CreateLienAsync(CreateLienRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var lien = CreateValidated(() => Lien.Create(
            request.InvestorId,
            request.SchemeId,
            request.SchemeClassId,
            request.Units,
            request.Amount,
            request.Currency,
            request.DocumentationReference,
            request.ApproverEvidenceReference,
            actor,
            now));
        _dbContext.Liens.Add(lien);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapLien(lien);
        await WriteAuditAsync(AuditEventType.Created, "LienPlaced", "Lien", lien.Id.ToString(), null, JsonSerializer.Serialize(dto), "Lien placed.", cancellationToken);
        return dto;
    }

    public async Task<LienDto> ReleaseLienAsync(Guid id, ReleaseLienRequest request, CancellationToken cancellationToken = default)
    {
        var lien = await _dbContext.Liens.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lien was not found.");
        var beforeJson = JsonSerializer.Serialize(MapLien(lien));
        try
        {
            lien.Release(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.ReleaseEvidenceReference);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("lien", exception.Message);
        }

        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapLien(lien);
        await WriteAuditAsync(AuditEventType.Updated, "LienReleased", "Lien", lien.Id.ToString(), beforeJson, JsonSerializer.Serialize(dto), "Lien released.", cancellationToken);
        return dto;
    }

    public async Task<RecurringContributionPlanDto> CreateRecurringPlanAsync(CreateRecurringContributionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var plan = CreateValidated(() => RecurringContributionPlan.Create(
            request.InvestorId,
            request.SchemeId,
            request.SchemeClassId,
            ParseEnum<RecurringPlanFrequency>(request.Frequency, "frequency"),
            request.Amount,
            request.Currency,
            ParseEnum<DealingChannel>(request.Channel, "channel"),
            BusinessDate.From(request.EffectiveFrom),
            request.EffectiveTo.HasValue ? BusinessDate.From(request.EffectiveTo.Value) : null,
            request.CollectionMethod,
            actor,
            now));

        _dbContext.RecurringContributionPlans.Add(plan);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapRecurringPlan(plan);
        await WriteAuditAsync(AuditEventType.Created, "RecurringContributionPlanCreated", "RecurringContributionPlan", plan.Id.ToString(), null, JsonSerializer.Serialize(dto), "Recurring contribution plan created.", cancellationToken);
        return dto;
    }

    public Task<RecurringContributionPlanDto> AmendRecurringPlanAsync(Guid id, AmendRecurringContributionPlanRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(
            id,
            "RecurringContributionPlanAmended",
            plan => plan.Amend(
                ParseEnum<RecurringPlanFrequency>(request.Frequency, "frequency"),
                request.Amount,
                ParseEnum<DealingChannel>(request.Channel, "channel"),
                BusinessDate.From(request.EffectiveFrom),
                request.EffectiveTo.HasValue ? BusinessDate.From(request.EffectiveTo.Value) : null,
                request.CollectionMethod),
            "Recurring contribution plan amended.",
            cancellationToken);
    }

    public Task<RecurringContributionPlanDto> PauseRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(id, "RecurringContributionPlanPaused", plan => plan.Pause(), "Recurring contribution plan paused.", cancellationToken);
    }

    public Task<RecurringContributionPlanDto> ResumeRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(id, "RecurringContributionPlanResumed", plan => plan.Resume(), "Recurring contribution plan resumed.", cancellationToken);
    }

    public Task<RecurringContributionPlanDto> CancelRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(id, "RecurringContributionPlanCancelled", plan => plan.Cancel(), "Recurring contribution plan cancelled.", cancellationToken);
    }

    public Task<RecurringContributionPlanDto> RegisterRecurringPlanFailedDebitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(id, "RecurringContributionPlanFailedDebitRegistered", plan => plan.RegisterFailedDebit(), "Recurring plan failed debit registered.", cancellationToken);
    }

    public Task<RecurringContributionPlanDto> RegisterRecurringPlanMissedCollectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return ChangeRecurringPlanAsync(id, "RecurringContributionPlanMissedCollectionRegistered", plan => plan.RegisterMissedCollection(), "Recurring plan missed collection registered.", cancellationToken);
    }

    public Task<DealingInstructionDto> SubmitInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInstructionAsync(
            id,
            "DealingInstructionSubmitted",
            AuditEventType.Submitted,
            instruction => instruction.Submit(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow),
            request.Comment ?? "Dealing instruction submitted.",
            cancellationToken);
    }

    public Task<DealingInstructionDto> ApproveInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInstructionAsync(
            id,
            "DealingInstructionApproved",
            AuditEventType.Approved,
            instruction => instruction.Approve(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow),
            request.Comment ?? "Dealing instruction approved.",
            cancellationToken);
    }

    public Task<DealingInstructionDto> RejectInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInstructionAsync(
            id,
            "DealingInstructionRejected",
            AuditEventType.Rejected,
            instruction => instruction.Reject(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.Comment),
            request.Comment ?? "Dealing instruction rejected.",
            cancellationToken);
    }

    public Task<DealingInstructionDto> CancelInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default)
    {
        return ChangeInstructionAsync(
            id,
            "DealingInstructionCancelled",
            AuditEventType.Updated,
            instruction => instruction.Cancel(CurrentUserIdOrThrow(), _dateTimeProvider.UtcNow, request.Comment),
            request.Comment ?? "Dealing instruction cancelled.",
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<DealingInstructionDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingStatuses = new[]
        {
            DealingInstructionStatus.Submitted,
            DealingInstructionStatus.PendingFunds,
            DealingInstructionStatus.CashVerified,
            DealingInstructionStatus.PendingReview,
            DealingInstructionStatus.PendingApproval
        };
        var ids = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Where(instruction => pendingStatuses.Contains(instruction.Status))
            .OrderBy(instruction => instruction.ReceivedAtUtc)
            .Select(instruction => instruction.Id)
            .ToListAsync(cancellationToken);

        var instructions = new List<DealingInstructionDto>();
        foreach (var id in ids)
        {
            instructions.Add(await MapInstructionAsync(id, cancellationToken));
        }

        return instructions;
    }

    public async Task<IReadOnlyCollection<CutOffBreachDto>> GetCutOffBreachesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CutOffBreaches
            .AsNoTracking()
            .Where(breach => breach.RequiresApproval)
            .OrderBy(breach => breach.ReceivedAtUtc)
            .Select(breach => new CutOffBreachDto(breach.Id, breach.ReceivedAtUtc, breach.CutOffTime, breach.Reason, breach.RequiresApproval, breach.ApprovedByUserId))
            .ToListAsync(cancellationToken);
    }

    private async Task<DealingInstructionDto> ChangeInstructionAsync(
        Guid id,
        string auditAction,
        AuditEventType auditEventType,
        Action<DealingInstruction> change,
        string reason,
        CancellationToken cancellationToken)
    {
        var instruction = await GetInstructionAggregateAsync(id, cancellationToken);
        var existingChildIds = DealingChildIds(instruction);
        var beforeJson = Snapshot(instruction);
        try
        {
            change(instruction);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("dealingInstruction", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "dealingInstruction", exception.Message);
        }

        MarkNewDealingChildrenAsAdded(instruction, existingChildIds);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = await MapInstructionAsync(id, cancellationToken);
        await WriteAuditAsync(auditEventType, auditAction, "DealingInstruction", id.ToString(), beforeJson, Snapshot(dto), reason, cancellationToken);
        return dto;
    }

    private async Task<RecurringContributionPlanDto> ChangeRecurringPlanAsync(
        Guid id,
        string auditAction,
        Action<RecurringContributionPlan> change,
        string reason,
        CancellationToken cancellationToken)
    {
        var plan = await _dbContext.RecurringContributionPlans.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Recurring contribution plan was not found.");
        var beforeJson = JsonSerializer.Serialize(MapRecurringPlan(plan));
        try
        {
            change(plan);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("recurringPlan", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "recurringPlan", exception.Message);
        }

        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapRecurringPlan(plan);
        await WriteAuditAsync(AuditEventType.Updated, auditAction, "RecurringContributionPlan", plan.Id.ToString(), beforeJson, JsonSerializer.Serialize(dto), reason, cancellationToken);
        return dto;
    }

    private async Task EnsureApprovedInvestorAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var status = await _dbContext.Investors
            .AsNoTracking()
            .Where(investor => investor.Id == investorId)
            .Select(investor => (InvestorStatus?)investor.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            throw new NotFoundException("Investor was not found.");
        }

        if (status != InvestorStatus.Approved)
        {
            throw Validation("investor", "No dealing instruction can be processed for an unapproved investor.");
        }
    }

    private async Task<SchemeClass> EnsureActiveSchemeClassAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes
            .AsNoTracking()
            .Include(candidate => candidate.Classes)
            .FirstOrDefaultAsync(candidate => candidate.Id == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme was not found.");

        if (scheme.Status != SchemeStatus.Active)
        {
            throw Validation("scheme", "Scheme must be active for dealing.");
        }

        var schemeClass = scheme.Classes.FirstOrDefault(candidate => candidate.Id == schemeClassId && candidate.IsActive);
        if (schemeClass is null)
        {
            throw Validation("schemeClass", "Scheme class must be active and belong to the scheme.");
        }

        return schemeClass;
    }

    private async Task<decimal> ActiveLienUnitsAsync(Guid investorId, Guid schemeId, Guid schemeClassId, decimal approvedNavPrice, CancellationToken cancellationToken)
    {
        var liens = await _dbContext.Liens
            .AsNoTracking()
            .Where(lien => lien.InvestorId == investorId
                && lien.SchemeId == schemeId
                && lien.SchemeClassId == schemeClassId
                && lien.Status == LienStatus.Placed)
            .ToListAsync(cancellationToken);

        return liens.Sum(lien => lien.Units ?? decimal.Round((lien.Amount ?? 0m) / approvedNavPrice, 12, MidpointRounding.AwayFromZero));
    }

    private async Task<decimal> ApprovalThresholdAsync(DealingInstructionType instructionType, string currency, CancellationToken cancellationToken)
    {
        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        var threshold = await _dbContext.ApprovalThresholds
            .Where(candidate => candidate.InstructionType == instructionType && candidate.Currency == normalizedCurrency && candidate.IsActive)
            .OrderByDescending(candidate => candidate.ThresholdAmount)
            .FirstOrDefaultAsync(cancellationToken);
        if (threshold is not null)
        {
            return threshold.ThresholdAmount;
        }

        _dbContext.ApprovalThresholds.Add(ApprovalThreshold.Create(instructionType, normalizedCurrency, DefaultLargeRedemptionThreshold));
        return DefaultLargeRedemptionThreshold;
    }

    private static void ValidateSubscriptionMinimum(DealingInstructionMode mode, decimal? amount, decimal? units, decimal? approvedNavPrice, decimal minimumContribution)
    {
        var contributionAmount = mode == DealingInstructionMode.Amount
            ? amount ?? 0m
            : approvedNavPrice.HasValue
                ? (units ?? 0m) * approvedNavPrice.Value
                : throw Validation("approvedNavPrice", "Approved NAV price is required to validate unit-based subscription minimum contribution.");
        if (contributionAmount < minimumContribution)
        {
            throw Validation("amount", "Subscription is below the scheme class minimum contribution.");
        }
    }

    private static void AddCutOffBreachIfNeeded(DealingInstruction instruction, TimeOnly cutOffTime)
    {
        if (TimeOnly.FromDateTime(instruction.ReceivedAtUtc) > cutOffTime)
        {
            instruction.AddCutOffBreach(instruction.ReceivedAtUtc, cutOffTime);
        }
    }

    private async Task<DealingInstruction> GetInstructionAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.DealingInstructions
            .Include(instruction => instruction.SubscriptionInstructions)
            .Include(instruction => instruction.RedemptionInstructions)
            .Include(instruction => instruction.SwitchInstructions)
            .Include(instruction => instruction.TransferInstructions)
            .Include(instruction => instruction.ValidationResults)
            .Include(instruction => instruction.CutOffBreaches)
            .Include(instruction => instruction.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(instruction => instruction.Id == id, cancellationToken)
            ?? throw new NotFoundException("Dealing instruction was not found.");
    }

    private async Task<DealingInstructionDto> MapInstructionAsync(Guid id, CancellationToken cancellationToken)
    {
        var instruction = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Include(candidate => candidate.SubscriptionInstructions)
            .Include(candidate => candidate.RedemptionInstructions)
            .Include(candidate => candidate.SwitchInstructions)
            .Include(candidate => candidate.TransferInstructions)
            .Include(candidate => candidate.ValidationResults)
            .Include(candidate => candidate.CutOffBreaches)
            .Include(candidate => candidate.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Dealing instruction was not found.");

        return MapInstruction(instruction);
    }

    private static DealingInstructionDto MapInstruction(DealingInstruction instruction)
    {
        return new DealingInstructionDto(
            instruction.Id,
            instruction.InstructionNumber,
            instruction.InstructionType.ToString(),
            instruction.InvestorId,
            instruction.SchemeId,
            instruction.SchemeClassId,
            instruction.Channel.ToString(),
            instruction.BusinessDate.Value,
            instruction.ReceivedAtUtc,
            instruction.Status.ToString(),
            instruction.SubmittedByUserId,
            instruction.ApprovedByUserId,
            instruction.SubscriptionInstructions.Select(subscription => new SubscriptionInstructionDto(
                subscription.Id,
                subscription.Mode.ToString(),
                subscription.Amount,
                subscription.Units,
                subscription.Currency,
                subscription.FundsCleared,
                subscription.ApprovedNavAvailable,
                subscription.ApprovedNavPrice,
                subscription.ApprovedNavDate?.Value,
                subscription.AllocatedUnits,
                subscription.ConfirmationNumber)).ToArray(),
            instruction.RedemptionInstructions.Select(redemption => new RedemptionInstructionDto(
                redemption.Id,
                redemption.Mode.ToString(),
                redemption.Amount,
                redemption.Units,
                redemption.FullRedemption,
                redemption.AvailableUnits,
                redemption.LienUnits,
                redemption.LockedUnits,
                redemption.RequestedUnits,
                redemption.GrossAmount,
                redemption.ExitFeeAmount,
                redemption.TaxAmount,
                redemption.NetPayoutAmount,
                redemption.RequiresApprovalThreshold,
                redemption.PayoutAuthorized,
                redemption.RedemptionAdviceNumber)).ToArray(),
            instruction.SwitchInstructions.Select(switchInstruction => new SwitchInstructionDto(
                switchInstruction.Id,
                switchInstruction.TargetSchemeId,
                switchInstruction.TargetSchemeClassId,
                switchInstruction.Mode.ToString(),
                switchInstruction.Amount,
                switchInstruction.Units,
                switchInstruction.FeeAmount,
                switchInstruction.OwnershipHistoryJson)).ToArray(),
            instruction.TransferInstructions.Select(transfer => new TransferInstructionDto(
                transfer.Id,
                transfer.ToInvestorId,
                transfer.Units,
                transfer.OwnershipHistoryJson)).ToArray(),
            instruction.ValidationResults.OrderBy(result => result.CreatedAtUtc).Select(result => new DealingValidationResultDto(
                result.Id,
                result.RuleCode,
                result.Message,
                result.Severity.ToString(),
                result.Passed,
                result.CreatedAtUtc)).ToArray(),
            instruction.CutOffBreaches.Select(breach => new CutOffBreachDto(
                breach.Id,
                breach.ReceivedAtUtc,
                breach.CutOffTime,
                breach.Reason,
                breach.RequiresApproval,
                breach.ApprovedByUserId)).ToArray(),
            instruction.StatusHistory.OrderBy(history => history.ChangedAtUtc).Select(history => new InstructionStatusHistoryDto(
                history.Id,
                history.Status.ToString(),
                history.ChangedByUserId,
                history.ChangedAtUtc,
                history.Comment)).ToArray());
    }

    private static LienDto MapLien(Lien lien)
    {
        return new LienDto(
            lien.Id,
            lien.InvestorId,
            lien.SchemeId,
            lien.SchemeClassId,
            lien.Units,
            lien.Amount,
            lien.Currency,
            lien.DocumentationReference,
            lien.ApproverEvidenceReference,
            lien.Status.ToString(),
            lien.PlacedByUserId,
            lien.PlacedAtUtc,
            lien.ReleasedByUserId,
            lien.ReleasedAtUtc,
            lien.ReleaseEvidenceReference);
    }

    private static RecurringContributionPlanDto MapRecurringPlan(RecurringContributionPlan plan)
    {
        return new RecurringContributionPlanDto(
            plan.Id,
            plan.InvestorId,
            plan.SchemeId,
            plan.SchemeClassId,
            plan.Frequency.ToString(),
            plan.Amount,
            plan.Currency,
            plan.Channel.ToString(),
            plan.EffectiveFrom.Value,
            plan.EffectiveTo?.Value,
            plan.CollectionMethod,
            plan.Status.ToString(),
            plan.MissedCollections,
            plan.FailedDebits);
    }

    private static string Snapshot(DealingInstruction instruction)
    {
        return JsonSerializer.Serialize(new
        {
            instruction.Id,
            instruction.InstructionNumber,
            Type = instruction.InstructionType.ToString(),
            instruction.InvestorId,
            instruction.SchemeId,
            instruction.SchemeClassId,
            Status = instruction.Status.ToString(),
            ValidationCount = instruction.ValidationResults.Count,
            CutOffBreachCount = instruction.CutOffBreaches.Count
        });
    }

    private static string Snapshot(DealingInstructionDto instruction)
    {
        return JsonSerializer.Serialize(new
        {
            instruction.Id,
            instruction.InstructionNumber,
            instruction.InstructionType,
            instruction.InvestorId,
            instruction.SchemeId,
            instruction.SchemeClassId,
            instruction.Status,
            ValidationCount = instruction.ValidationResults.Count,
            CutOffBreachCount = instruction.CutOffBreaches.Count
        });
    }

    private Task WriteAuditAsync(
        AuditEventType eventType,
        string action,
        string entityName,
        string entityId,
        string? beforeJson,
        string afterJson,
        string reason,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            EventType: eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason), cancellationToken);
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException("The dealing change conflicts with an existing record.");
        }
    }

    private static HashSet<Guid> DealingChildIds(DealingInstruction instruction)
    {
        return DealingChildren(instruction).Select(child => child.Id).ToHashSet();
    }

    private void MarkNewDealingChildrenAsAdded(DealingInstruction instruction, HashSet<Guid> existingChildIds)
    {
        _dbContext.ChangeTracker.DetectChanges();
        foreach (var child in DealingChildren(instruction).Where(child => !existingChildIds.Contains(child.Id)))
        {
            var entry = _dbContext.Entry(child);
            if (entry.State is EntityState.Detached or EntityState.Modified or EntityState.Unchanged)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private static IEnumerable<Entity> DealingChildren(DealingInstruction instruction)
    {
        return instruction.SubscriptionInstructions.Cast<Entity>()
            .Concat(instruction.RedemptionInstructions)
            .Concat(instruction.SwitchInstructions)
            .Concat(instruction.TransferInstructions)
            .Concat(instruction.ValidationResults)
            .Concat(instruction.CutOffBreaches)
            .Concat(instruction.StatusHistory);
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw Validation(fieldName, $"Unsupported {fieldName}.");
    }

    private static T CreateValidated<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("dealingInstruction", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "dealingInstruction", exception.Message);
        }
    }

    private static DateTime EnsureUtc(DateTime value, string fieldName)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.ToUniversalTime();
    }

    private static string ValidateJson(string payload, string fieldName)
    {
        try
        {
            using var _ = JsonDocument.Parse(payload);
            return payload;
        }
        catch (JsonException exception)
        {
            throw Validation(fieldName, $"JSON payload is invalid: {exception.Message}");
        }
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
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private static string GenerateInstructionNumber(string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..35].ToUpperInvariant();
    }
}
