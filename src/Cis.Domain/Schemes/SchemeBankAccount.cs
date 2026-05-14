using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeBankAccount : Entity
{
    private SchemeBankAccount()
    {
    }

    private SchemeBankAccount(Guid schemeId, string bankName, string accountNumber, string accountName, string currency, string? swiftCode)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        BankName = SchemeValidation.Required(bankName, nameof(bankName), 200);
        AccountNumber = SchemeValidation.Required(accountNumber, nameof(accountNumber), 100);
        AccountName = SchemeValidation.Required(accountName, nameof(accountName), 200);
        Currency = SchemeValidation.Currency(currency, nameof(currency));
        SwiftCode = SchemeValidation.Optional(swiftCode, 20);
        IsActive = true;
    }

    public Guid SchemeId { get; private set; }

    public string BankName { get; private set; } = string.Empty;

    public string AccountNumber { get; private set; } = string.Empty;

    public string AccountName { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public string? SwiftCode { get; private set; }

    public bool IsActive { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static SchemeBankAccount Create(Guid schemeId, string bankName, string accountNumber, string accountName, string currency, string? swiftCode)
    {
        return new SchemeBankAccount(schemeId, bankName, accountNumber, accountName, currency, swiftCode);
    }
}
