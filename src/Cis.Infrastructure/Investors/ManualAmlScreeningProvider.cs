using Cis.Application.Common.Interfaces;

namespace Cis.Infrastructure.Investors;

internal sealed class ManualAmlScreeningProvider : IAmlScreeningProvider
{
    public Task<AmlScreeningProviderResult> ScreenAsync(AmlScreeningProviderRequest request, CancellationToken cancellationToken = default)
    {
        var reference = string.IsNullOrWhiteSpace(request.ScreeningReference)
            ? $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.InvestorNumber}"
            : request.ScreeningReference.Trim();

        return Task.FromResult(new AmlScreeningProviderResult(
            request.ProviderName,
            reference,
            request.ManualHits));
    }
}
