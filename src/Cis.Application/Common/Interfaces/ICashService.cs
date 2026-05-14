using Cis.Contracts.Cash;

namespace Cis.Application.Common.Interfaces;

public interface ICashService
{
    Task<BankStatementImportDto> ImportBankStatementAsync(ImportBankStatementRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);

    Task<BankStatementImportDto> GetBankStatementImportAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReconciliationRunDto> CreateReconciliationRunAsync(ReconciliationRunRequest request, CancellationToken cancellationToken = default);

    Task<ReconciliationRunDto> GetReconciliationRunAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SuspenseItemDto>> GetSuspenseAsync(CancellationToken cancellationToken = default);

    Task<SuspenseItemDto> ResolveSuspenseAsync(Guid id, ResolveSuspenseRequest request, CancellationToken cancellationToken = default);

    Task<PaymentInstructionDto> CreatePaymentInstructionAsync(CreatePaymentInstructionRequest request, CancellationToken cancellationToken = default);

    Task<PaymentInstructionDto> UpdatePaymentStatusAsync(Guid id, UpdatePaymentStatusRequest request, CancellationToken cancellationToken = default);

    Task<ReversalRequestDto> CreateReversalAsync(CreateReversalRequest request, CancellationToken cancellationToken = default);

    Task<ReversalRequestDto> ApproveReversalAsync(Guid id, ApproveReversalRequest request, CancellationToken cancellationToken = default);
}