namespace Cis.Api.Security;

internal static class PermissionPolicyName
{
    public const string Prefix = "Permission:";

    public static string For(string permission)
    {
        return $"{Prefix}{permission}";
    }
}
