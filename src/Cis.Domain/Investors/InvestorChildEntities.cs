using Cis.Domain.Common;

namespace Cis.Domain.Investors;

public sealed class BeneficialOwner : Entity
{
    private BeneficialOwner()
    {
    }

    private BeneficialOwner(Guid investorId, string fullName, string identityNumber, decimal ownershipPercentage, bool isPoliticallyExposed)
    {
        InvestorId = investorId;
        FullName = InvestorValidation.Required(fullName, nameof(fullName), 200);
        IdentityNumber = InvestorValidation.Required(identityNumber, nameof(identityNumber), 100).ToUpperInvariant();
        OwnershipPercentage = InvestorValidation.Percentage(ownershipPercentage, nameof(ownershipPercentage));
        IsPoliticallyExposed = isPoliticallyExposed;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string FullName { get; private set; } = string.Empty;

    public string IdentityNumber { get; private set; } = string.Empty;

    public decimal OwnershipPercentage { get; private set; }

    public bool IsPoliticallyExposed { get; private set; }

    public static BeneficialOwner Create(Guid investorId, string fullName, string identityNumber, decimal ownershipPercentage, bool isPoliticallyExposed)
    {
        return new BeneficialOwner(investorId, fullName, identityNumber, ownershipPercentage, isPoliticallyExposed);
    }
}

public sealed class InvestorBankAccount : Entity
{
    private InvestorBankAccount()
    {
    }

    private InvestorBankAccount(Guid investorId, string bankName, string accountNumber, string accountName, string currency, string? swiftCode, bool highRiskFlag)
    {
        InvestorId = investorId;
        BankName = InvestorValidation.Required(bankName, nameof(bankName), 200);
        AccountNumber = InvestorValidation.Required(accountNumber, nameof(accountNumber), 100);
        AccountName = InvestorValidation.Required(accountName, nameof(accountName), 200);
        Currency = InvestorValidation.Currency(currency, nameof(currency));
        SwiftCode = InvestorValidation.Optional(swiftCode, 20);
        IsActive = true;
        HighRiskFlag = highRiskFlag;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string BankName { get; private set; } = string.Empty;

    public string AccountNumber { get; private set; } = string.Empty;

    public string AccountName { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public string? SwiftCode { get; private set; }

    public bool IsActive { get; private set; }

    public bool HighRiskFlag { get; private set; }

    public static InvestorBankAccount Create(
        Guid investorId,
        string bankName,
        string accountNumber,
        string accountName,
        string currency,
        string? swiftCode,
        bool highRiskFlag)
    {
        return new InvestorBankAccount(investorId, bankName, accountNumber, accountName, currency, swiftCode, highRiskFlag);
    }
}

public sealed class InvestorTaxProfile : Entity
{
    private InvestorTaxProfile()
    {
    }

    private InvestorTaxProfile(Guid investorId, string taxNumber, string countryOfTaxResidence)
    {
        InvestorId = investorId;
        TaxNumber = InvestorValidation.Required(taxNumber, nameof(taxNumber), 100).ToUpperInvariant();
        CountryOfTaxResidence = InvestorValidation.Required(countryOfTaxResidence, nameof(countryOfTaxResidence), 100);
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string TaxNumber { get; private set; } = string.Empty;

    public string CountryOfTaxResidence { get; private set; } = string.Empty;

    public static InvestorTaxProfile Create(Guid investorId, string taxNumber, string countryOfTaxResidence)
    {
        return new InvestorTaxProfile(investorId, taxNumber, countryOfTaxResidence);
    }
}

public sealed class InvestorContact : Entity
{
    private InvestorContact()
    {
    }

    private InvestorContact(Guid investorId, string contactType, string value, bool isPrimary)
    {
        InvestorId = investorId;
        ContactType = InvestorValidation.Required(contactType, nameof(contactType), 50);
        Value = InvestorValidation.Required(value, nameof(value), 300);
        IsPrimary = isPrimary;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string ContactType { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    public static InvestorContact Create(Guid investorId, string contactType, string value, bool isPrimary)
    {
        return new InvestorContact(investorId, contactType, value, isPrimary);
    }
}

public sealed class InvestorMandate : Entity
{
    private InvestorMandate()
    {
    }

    private InvestorMandate(Guid investorId, string mandateType, string signingAuthority, BusinessDate effectiveFrom)
    {
        InvestorId = investorId;
        MandateType = InvestorValidation.Required(mandateType, nameof(mandateType), 100);
        SigningAuthority = InvestorValidation.Required(signingAuthority, nameof(signingAuthority), 200);
        EffectiveFrom = effectiveFrom;
        IsActive = true;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string MandateType { get; private set; } = string.Empty;

    public string SigningAuthority { get; private set; } = string.Empty;

    public BusinessDate EffectiveFrom { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public bool IsActive { get; private set; }

    public static InvestorMandate Create(Guid investorId, string mandateType, string signingAuthority, BusinessDate effectiveFrom)
    {
        return new InvestorMandate(investorId, mandateType, signingAuthority, effectiveFrom);
    }
}

public sealed class InvestorChangeLog : Entity
{
    private InvestorChangeLog()
    {
    }

    private InvestorChangeLog(
        Guid investorId,
        string changeType,
        string beforeJson,
        string afterJson,
        bool highRiskFlag,
        string changedByUserId,
        DateTime changedAtUtc,
        string? reason)
    {
        InvestorValidation.EnsureUtc(changedAtUtc, nameof(changedAtUtc));
        InvestorId = investorId;
        ChangeType = InvestorValidation.Required(changeType, nameof(changeType), 100);
        BeforeJson = string.IsNullOrWhiteSpace(beforeJson) ? "{}" : beforeJson;
        AfterJson = InvestorValidation.Required(afterJson, nameof(afterJson), 4000);
        HighRiskFlag = highRiskFlag;
        ChangedByUserId = InvestorValidation.Required(changedByUserId, nameof(changedByUserId), 200);
        ChangedAtUtc = changedAtUtc;
        Reason = InvestorValidation.Optional(reason, 1000);
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string ChangeType { get; private set; } = string.Empty;

    public string BeforeJson { get; private set; } = "{}";

    public string AfterJson { get; private set; } = "{}";

    public bool HighRiskFlag { get; private set; }

    public string ChangedByUserId { get; private set; } = string.Empty;

    public DateTime ChangedAtUtc { get; private set; }

    public string? Reason { get; private set; }

    public static InvestorChangeLog Create(
        Guid investorId,
        string changeType,
        string beforeJson,
        string afterJson,
        bool highRiskFlag,
        string changedByUserId,
        DateTime changedAtUtc,
        string? reason)
    {
        return new InvestorChangeLog(investorId, changeType, beforeJson, afterJson, highRiskFlag, changedByUserId, changedAtUtc, reason);
    }
}
