using Cis.Domain.Common;

namespace Cis.Domain.Investors;

public sealed class InvestorProfileIndividual : Entity
{
    private InvestorProfileIndividual()
    {
    }

    private InvestorProfileIndividual(Guid investorId, string firstName, string lastName, string identityNumber, DateOnly dateOfBirth, string nationality)
    {
        InvestorId = investorId;
        FirstName = InvestorValidation.Required(firstName, nameof(firstName), 100);
        LastName = InvestorValidation.Required(lastName, nameof(lastName), 100);
        IdentityNumber = InvestorValidation.Required(identityNumber, nameof(identityNumber), 100).ToUpperInvariant();
        DateOfBirth = dateOfBirth;
        Nationality = InvestorValidation.Required(nationality, nameof(nationality), 100);
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string IdentityNumber { get; private set; } = string.Empty;

    public DateOnly DateOfBirth { get; private set; }

    public string Nationality { get; private set; } = string.Empty;

    public static InvestorProfileIndividual Create(Guid investorId, string firstName, string lastName, string identityNumber, DateOnly dateOfBirth, string nationality)
    {
        return new InvestorProfileIndividual(investorId, firstName, lastName, identityNumber, dateOfBirth, nationality);
    }
}

public sealed class InvestorProfileCorporate : Entity
{
    private InvestorProfileCorporate()
    {
    }

    private InvestorProfileCorporate(Guid investorId, string registeredName, string registrationNumber, DateOnly incorporationDate)
    {
        InvestorId = investorId;
        RegisteredName = InvestorValidation.Required(registeredName, nameof(registeredName), 200);
        RegistrationNumber = InvestorValidation.Required(registrationNumber, nameof(registrationNumber), 100).ToUpperInvariant();
        IncorporationDate = incorporationDate;
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string RegisteredName { get; private set; } = string.Empty;

    public string RegistrationNumber { get; private set; } = string.Empty;

    public DateOnly IncorporationDate { get; private set; }

    public static InvestorProfileCorporate Create(Guid investorId, string registeredName, string registrationNumber, DateOnly incorporationDate)
    {
        return new InvestorProfileCorporate(investorId, registeredName, registrationNumber, incorporationDate);
    }
}

public sealed class InvestorProfileJoint : Entity
{
    private InvestorProfileJoint()
    {
    }

    private InvestorProfileJoint(Guid investorId, string jointName, string primaryIdentityNumber, string secondaryIdentityNumber)
    {
        InvestorId = investorId;
        JointName = InvestorValidation.Required(jointName, nameof(jointName), 200);
        PrimaryIdentityNumber = InvestorValidation.Required(primaryIdentityNumber, nameof(primaryIdentityNumber), 100).ToUpperInvariant();
        SecondaryIdentityNumber = InvestorValidation.Required(secondaryIdentityNumber, nameof(secondaryIdentityNumber), 100).ToUpperInvariant();
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string JointName { get; private set; } = string.Empty;

    public string PrimaryIdentityNumber { get; private set; } = string.Empty;

    public string SecondaryIdentityNumber { get; private set; } = string.Empty;

    public static InvestorProfileJoint Create(Guid investorId, string jointName, string primaryIdentityNumber, string secondaryIdentityNumber)
    {
        return new InvestorProfileJoint(investorId, jointName, primaryIdentityNumber, secondaryIdentityNumber);
    }
}

public sealed class InvestorProfileGroup : Entity
{
    private InvestorProfileGroup()
    {
    }

    private InvestorProfileGroup(Guid investorId, string groupName, string registrationNumber, string contactPersonName)
    {
        InvestorId = investorId;
        GroupName = InvestorValidation.Required(groupName, nameof(groupName), 200);
        RegistrationNumber = InvestorValidation.Required(registrationNumber, nameof(registrationNumber), 100).ToUpperInvariant();
        ContactPersonName = InvestorValidation.Required(contactPersonName, nameof(contactPersonName), 200);
    }

    public Guid InvestorId { get; private set; }

    public Investor Investor { get; private set; } = null!;

    public string GroupName { get; private set; } = string.Empty;

    public string RegistrationNumber { get; private set; } = string.Empty;

    public string ContactPersonName { get; private set; } = string.Empty;

    public static InvestorProfileGroup Create(Guid investorId, string groupName, string registrationNumber, string contactPersonName)
    {
        return new InvestorProfileGroup(investorId, groupName, registrationNumber, contactPersonName);
    }
}
