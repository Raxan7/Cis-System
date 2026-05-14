using Cis.Domain.Common;

namespace Cis.Domain.Dealing;

public sealed class Lien : AuditableAggregateRoot
{
    private Lien()
    {
    }

    private Lien(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        decimal? units,
        decimal? amount,
        string currency,
        string documentationReference,
        string approverEvidenceReference,
        string placedByUserId,
        DateTime placedAtUtc)
    {
        DealingValidation.EnsureUtc(placedAtUtc, nameof(placedAtUtc));
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        Units = units.HasValue ? DealingValidation.Positive(units.Value, nameof(units)) : null;
        Amount = amount.HasValue ? DealingValidation.Positive(amount.Value, nameof(amount)) : null;
        Currency = DealingValidation.Currency(currency, nameof(currency));
        DocumentationReference = DealingValidation.Required(documentationReference, nameof(documentationReference), 500);
        ApproverEvidenceReference = DealingValidation.Required(approverEvidenceReference, nameof(approverEvidenceReference), 500);
        Status = LienStatus.Placed;
        PlacedByUserId = DealingValidation.Required(placedByUserId, nameof(placedByUserId), 200);
        PlacedAtUtc = placedAtUtc;
        MarkCreated(placedByUserId, placedAtUtc);

        if (!Units.HasValue && !Amount.HasValue)
        {
            throw new ArgumentException("Lien requires either units or amount.");
        }
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public decimal? Units { get; private set; }

    public decimal? Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string DocumentationReference { get; private set; } = string.Empty;

    public string ApproverEvidenceReference { get; private set; } = string.Empty;

    public LienStatus Status { get; private set; }

    public string PlacedByUserId { get; private set; } = string.Empty;

    public DateTime PlacedAtUtc { get; private set; }

    public string? ReleasedByUserId { get; private set; }

    public DateTime? ReleasedAtUtc { get; private set; }

    public string? ReleaseEvidenceReference { get; private set; }

    public static Lien Create(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        decimal? units,
        decimal? amount,
        string currency,
        string documentationReference,
        string approverEvidenceReference,
        string placedByUserId,
        DateTime placedAtUtc)
    {
        return new Lien(investorId, schemeId, schemeClassId, units, amount, currency, documentationReference, approverEvidenceReference, placedByUserId, placedAtUtc);
    }

    public void Release(string releasedByUserId, DateTime releasedAtUtc, string releaseEvidenceReference)
    {
        DealingValidation.EnsureUtc(releasedAtUtc, nameof(releasedAtUtc));
        if (Status == LienStatus.Released)
        {
            throw new InvalidOperationException("Lien is already released.");
        }

        Status = LienStatus.Released;
        ReleasedByUserId = DealingValidation.Required(releasedByUserId, nameof(releasedByUserId), 200);
        ReleasedAtUtc = releasedAtUtc;
        ReleaseEvidenceReference = DealingValidation.Required(releaseEvidenceReference, nameof(releaseEvidenceReference), 500);
    }
}
