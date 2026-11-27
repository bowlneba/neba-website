using System.Security.Cryptography;

using Microsoft.Extensions.Options;

namespace Neba.Api.Encryption;

/// <summary>
/// Checks every key's settings at startup. Messages name the setting and never its value.
/// </summary>
internal sealed class EncryptionKeySettingsValidator(IHostEnvironment environment)
    : IValidateOptions<EncryptionKeySettings>
{
    private const int MinimumHashKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, EncryptionKeySettings options)
    {
        var section = $"Encryption:Keys:{name}";
        var failures = new List<string>();

        if (!IsValidHashKey(options.HashKey))
        {
            failures.Add($"{section}:HashKey must be Base64 of at least {MinimumHashKeyBytes} bytes.");
        }

        var hasKeyVaultUri = !string.IsNullOrWhiteSpace(options.KeyVaultKeyUri);
        var hasLocalKey = !string.IsNullOrWhiteSpace(options.LocalPrivateKey);

        if (hasKeyVaultUri && !KeyVaultKeyId.TryParse(options.KeyVaultKeyUri, out _))
        {
            failures.Add($"{section}:KeyVaultKeyUri must be https://{{vault}}/keys/{{name}}/{{version}}.");
        }

        if (!environment.IsDevelopment())
        {
            if (!hasKeyVaultUri)
            {
                failures.Add($"{section}:KeyVaultKeyUri is required outside Development.");
            }

            if (hasLocalKey)
            {
                failures.Add($"{section}:LocalPrivateKey is not allowed outside Development.");
            }
        }
        else if (!hasKeyVaultUri && !IsValidPrivateKey(options.LocalPrivateKey))
        {
            failures.Add($"{section}:LocalPrivateKey must be a Base64 PKCS#8 RSA private key.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidHashKey(string value)
    {
        var buffer = new byte[value.Length];

        return Convert.TryFromBase64String(value, buffer, out var written) && written >= MinimumHashKeyBytes;
    }

    private static bool IsValidPrivateKey(string value)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(value), out _);

            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            // The validation failure the caller adds is the report; the exception adds nothing and
            // startup has no logger yet.
            return false;
        }
    }
}