namespace Cis.Application.Common.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Invalid email address or password.")
    {
    }
}
