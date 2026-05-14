using Cis.Contracts.Dealing;

namespace Cis.Application.Common.Interfaces;

public interface IDealingService
{
    Task<DealingInstructionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> CreateRedemptionAsync(CreateRedemptionRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> CreateSwitchAsync(CreateSwitchRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> CreateTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default);

    Task<LienDto> CreateLienAsync(CreateLienRequest request, CancellationToken cancellationToken = default);

    Task<LienDto> ReleaseLienAsync(Guid id, ReleaseLienRequest request, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> CreateRecurringPlanAsync(CreateRecurringContributionPlanRequest request, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> AmendRecurringPlanAsync(Guid id, AmendRecurringContributionPlanRequest request, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> PauseRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> ResumeRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> CancelRecurringPlanAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> RegisterRecurringPlanFailedDebitAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecurringContributionPlanDto> RegisterRecurringPlanMissedCollectionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> SubmitInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> ApproveInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> RejectInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default);

    Task<DealingInstructionDto> CancelInstructionAsync(Guid id, DealingInstructionActionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DealingInstructionDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CutOffBreachDto>> GetCutOffBreachesAsync(CancellationToken cancellationToken = default);
}
