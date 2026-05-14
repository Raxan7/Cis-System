namespace Cis.Application.Common.Interfaces;

public interface IPasswordPolicyValidator
{
    void Validate(string password, string? email = null);
}
