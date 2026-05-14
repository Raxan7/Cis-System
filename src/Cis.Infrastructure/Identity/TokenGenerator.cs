using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Cis.Infrastructure.Identity;

internal static class TokenGenerator
{
    public static string NewRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
