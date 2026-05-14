namespace Cis.Application.Common.Interfaces;

public interface IAmlScreeningProvider
{
    Task<AmlScreeningProviderResult> ScreenAsync(AmlScreeningProviderRequest request, CancellationToken cancellationToken = default);
}

public sealed record AmlScreeningProviderRequest(
    Guid InvestorId,
    string InvestorNumber,
    string DisplayName,
    string ProviderName,
    string? ScreeningReference,
    IReadOnlyCollection<AmlScreeningProviderHit> ManualHits);

public sealed record AmlScreeningProviderResult(
    string ProviderName,
    string ScreeningReference,
    IReadOnlyCollection<AmlScreeningProviderHit> Hits);

public sealed record AmlScreeningProviderHit(
    string ListName,
    string MatchedName,
    string RiskLevel,
    string? Notes);
