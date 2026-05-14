using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeEligibilityRule : Entity
{
    private SchemeEligibilityRule()
    {
    }

    private SchemeEligibilityRule(Guid schemeId, string ruleType, string description, string ruleExpressionJson)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        RuleType = SchemeValidation.Required(ruleType, nameof(ruleType), 100);
        Description = SchemeValidation.Required(description, nameof(description), 500);
        RuleExpressionJson = SchemeValidation.Required(ruleExpressionJson, nameof(ruleExpressionJson), 4000);
        Status = RuleStatus.Draft;
    }

    public Guid SchemeId { get; private set; }

    public string RuleType { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string RuleExpressionJson { get; private set; } = string.Empty;

    public RuleStatus Status { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static SchemeEligibilityRule Create(Guid schemeId, string ruleType, string description, string ruleExpressionJson)
    {
        return new SchemeEligibilityRule(schemeId, ruleType, description, ruleExpressionJson);
    }

    public void Activate()
    {
        Status = RuleStatus.Active;
    }
}
