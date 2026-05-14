using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeConfiguration : Entity
{
    private SchemeConfiguration()
    {
    }

    private SchemeConfiguration(Guid schemeId, string navPricingBasis, string incomeRecognitionBasis)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        NavPricingBasis = SchemeValidation.Required(navPricingBasis, nameof(navPricingBasis), 100);
        IncomeRecognitionBasis = SchemeValidation.Required(incomeRecognitionBasis, nameof(incomeRecognitionBasis), 100);
    }

    public Guid SchemeId { get; private set; }

    public string NavPricingBasis { get; private set; } = string.Empty;

    public string IncomeRecognitionBasis { get; private set; } = string.Empty;

    public Scheme? Scheme { get; private set; }

    public static SchemeConfiguration Create(Guid schemeId, string navPricingBasis, string incomeRecognitionBasis)
    {
        return new SchemeConfiguration(schemeId, navPricingBasis, incomeRecognitionBasis);
    }
}
