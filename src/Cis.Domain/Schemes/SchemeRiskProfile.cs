using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeRiskProfile : Entity
{
    private SchemeRiskProfile()
    {
    }

    private SchemeRiskProfile(Guid schemeId, string riskRating, decimal maxSingleIssuerExposure)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        RiskRating = SchemeValidation.Required(riskRating, nameof(riskRating), 100);
        MaxSingleIssuerExposure = SchemeValidation.Percentage(maxSingleIssuerExposure, nameof(maxSingleIssuerExposure));
    }

    public Guid SchemeId { get; private set; }

    public string RiskRating { get; private set; } = string.Empty;

    public decimal MaxSingleIssuerExposure { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static SchemeRiskProfile Create(Guid schemeId, string riskRating, decimal maxSingleIssuerExposure)
    {
        return new SchemeRiskProfile(schemeId, riskRating, maxSingleIssuerExposure);
    }
}
