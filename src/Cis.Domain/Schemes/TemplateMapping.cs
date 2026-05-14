using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class TemplateMapping : Entity
{
    private TemplateMapping()
    {
    }

    private TemplateMapping(Guid schemeId, string templateType, string templateCode)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        TemplateType = SchemeValidation.Required(templateType, nameof(templateType), 100);
        TemplateCode = SchemeValidation.Required(templateCode, nameof(templateCode), 100);
        IsActive = true;
    }

    public Guid SchemeId { get; private set; }

    public string TemplateType { get; private set; } = string.Empty;

    public string TemplateCode { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static TemplateMapping Create(Guid schemeId, string templateType, string templateCode)
    {
        return new TemplateMapping(schemeId, templateType, templateCode);
    }
}
