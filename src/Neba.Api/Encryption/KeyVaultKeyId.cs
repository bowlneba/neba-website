using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Neba.Api.Encryption;

/// <summary>
/// A versioned Key Vault key URI: https://{vault}/keys/{name}/{version}.
/// </summary>
internal sealed partial record KeyVaultKeyId(Uri VaultUri, string KeyName, string Version)
{
    public static bool TryParse(string? value, [NotNullWhen(true)] out KeyVaultKeyId? keyId)
    {
        keyId = null;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is not ["keys", var name, var version] || !IsValidVersion(version))
        {
            return false;
        }

        keyId = new KeyVaultKeyId(new Uri(uri.GetLeftPart(UriPartial.Authority)), name, version);

        return true;
    }

    // The version in a stored or wire value goes into a URI, so accept only the Key Vault shape.
    public static bool IsValidVersion(string version) => VersionPattern().IsMatch(version);

    public Uri ForVersion(string version) => new(VaultUri, $"keys/{KeyName}/{version}");

    [GeneratedRegex(@"\A[0-9a-fA-F]{32}\z")]
    private static partial Regex VersionPattern();
}
