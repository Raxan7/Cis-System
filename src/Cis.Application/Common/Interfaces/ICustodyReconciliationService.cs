using Cis.Contracts.CustodyReconciliation;

namespace Cis.Application.Common.Interfaces;

public interface ICustodyReconciliationService
{
    Task<CustodianDto> CreateCustodianAsync(CreateCustodianRequest request, CancellationToken cancellationToken = default);

    Task<CustodianStatementImportDto> ImportHoldingsAsync(ImportCustodianHoldingsRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);

    Task<CustodianStatementImportDto> ImportCashAsync(ImportCustodianCashRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);

    Task<CustodyReconciliationRunDto> CreateReconciliationRunAsync(CreateCustodyReconciliationRunRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CustodyReconciliationBreakDto>> GetBreaksAsync(string? status, CancellationToken cancellationToken = default);

    Task<CustodyReconciliationBreakDto> AssignBreakAsync(Guid id, AssignCustodyBreakRequest request, CancellationToken cancellationToken = default);

    Task<CustodyReconciliationBreakDto> ResolveBreakAsync(Guid id, ResolveCustodyBreakRequest request, CancellationToken cancellationToken = default);

    Task<SafekeepingConfirmationDto> CreateSafekeepingConfirmationAsync(CreateSafekeepingConfirmationRequest request, CancellationToken cancellationToken = default);
}
