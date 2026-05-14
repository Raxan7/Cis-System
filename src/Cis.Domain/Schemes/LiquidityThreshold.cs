using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class LiquidityThreshold : Entity
{
    private LiquidityThreshold()
    {
    }

    private LiquidityThreshold(Guid schemeId, decimal minimumLiquidAssetRatio, decimal warningThreshold, decimal breachThreshold)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        MinimumLiquidAssetRatio = SchemeValidation.Percentage(minimumLiquidAssetRatio, nameof(minimumLiquidAssetRatio));
        WarningThreshold = SchemeValidation.Percentage(warningThreshold, nameof(warningThreshold));
        BreachThreshold = SchemeValidation.Percentage(breachThreshold, nameof(breachThreshold));
        if (BreachThreshold > WarningThreshold || WarningThreshold > MinimumLiquidAssetRatio)
        {
            throw new ArgumentException("Liquidity thresholds must be ordered breach <= warning <= minimum liquid asset ratio.");
        }
    }

    public Guid SchemeId { get; private set; }

    public decimal MinimumLiquidAssetRatio { get; private set; }

    public decimal WarningThreshold { get; private set; }

    public decimal BreachThreshold { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static LiquidityThreshold Create(Guid schemeId, decimal minimumLiquidAssetRatio, decimal warningThreshold, decimal breachThreshold)
    {
        return new LiquidityThreshold(schemeId, minimumLiquidAssetRatio, warningThreshold, breachThreshold);
    }
}
