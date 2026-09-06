# Sensitive Column Encryption

## Problem

Upcoming features will store sensitive values on domain entities that must be encrypted at
rest — starting with bowler SSNs, and later potentially payment card numbers. There is
currently no pattern in the codebase for encrypting a column's value before it hits the
database and decrypting it on the way out.

This doc captures the design so it isn't lost before an issue/implementation picks it up.

## Non-goals / out of scope for v1

- Only one encryption key exists to start (for SSNs). The design supports multiple keys, but
  a second key (e.g. for card numbers) is not being built now — see "Multiple keys" below for
  how to add one later.
- This is not a replacement for ASP.NET Core Data Protection. Data Protection
  (`AddSharedDataProtection` in `Neba.Api`/`Neba.Website.Server`) exists solely to protect the
  shared auth-cookie key ring and is unrelated to this feature — do not reuse that key or that
  mechanism for business data.
- Not using Key Vault's direct RSA `Encrypt`/`Decrypt` on the plaintext. Payload size is capped
  (~190 bytes for a 2048-bit RSA-OAEP key) and it doesn't generalize past small fields. Using
  envelope encryption instead — see below.

## Key strategy: one key per compliance/access boundary

Rule of thumb: **one key per compliance/access boundary, not one key per field or table.**

Split into a new key when any of these differ from an existing key's data:

- **Regulatory regime** — e.g. SSN/PII vs. PCI-scoped card data are different audit and
  rotation requirements; mixing them means every PCI audit has to review everything else that
  key touches.
- **Access boundary** — if the set of services/identities that need to decrypt differs at all,
  split. E.g. only `api` should ever decrypt an SSN; `web` has no reason to hold that access.
- **Lifecycle** — data that might need independent bulk rotation/purge from other data should
  be on its own key.
- **Blast radius** — a suspected key compromise should only force re-encryption of the data
  under that one key, not everything.

It's fine to share a key across fields that share all of the above — e.g. SSN and driver's
license number could reasonably sit under one `BowlerPii` key if both are only ever decrypted
by the same service and rotate together.

For now: **one key, `BowlerPii`**, used for the SSN column. A `PaymentCard` key would be added
later, if/when card data is actually stored directly (note: for PCI, tokenizing through a
processor like Stripe and never storing the raw card number is usually the better call, and
would avoid needing this key at all).

## Provisioning the key (Azure/Aspire)

No typed Aspire/`Azure.Provisioning` resource exists for Key Vault *keys* (only secrets) — same
situation as the existing Data Protection key. Follow the same handwritten-Bicep pattern as
`src/Neba.AppHost/dataprotectionkey.bicep`:

1. New Bicep file, e.g. `bowlerpii-key.bicep`, creating an RSA key scoped to **wrap/unwrap**
   operations (envelope encryption, not direct encrypt):

   ```bicep
   @description('Name of the existing Key Vault to create the key in.')
   param keyVaultName string

   @description('Name of the RSA key used to wrap/unwrap the per-value AES key for Bowler PII encryption.')
   param keyName string = 'bowlerpii-key'

   @description('Unused: Key Vault keys are not a regional resource in this template. Declared because Aspire passes a location parameter to every Bicep module by convention.')
   param location string = resourceGroup().location

   resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
     name: keyVaultName
   }

   resource bowlerPiiKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
     parent: keyVault
     name: keyName
     properties: {
       kty: 'RSA'
       keySize: 2048
       keyOps: [
         'wrapKey'
         'unwrapKey'
       ]
     }
   }

   output keyUri string = bowlerPiiKey.properties.keyUriWithVersion
   ```

2. Wire it into `AppHost.cs`, alongside the existing `dataProtectionKey`:

   ```csharp
   var bowlerPiiKey = builder.AddBicepTemplate("bowlerpiikey", "bowlerpii-key.bicep")
       .WithParameter("keyVaultName", keyVault.Resource.NameOutputReference);
   ```

3. Pass the key URI only to the app(s) that need it — `api` only, not `web`, since only `api`
   should decrypt SSNs:

   ```csharp
   api.WithEnvironment("Encryption__Keys__BowlerPii__KeyVaultKeyUri", bowlerPiiKey.GetOutput("keyUri"));
   ```

4. Role assignment: scope `KeyVaultCryptoUser` to just this key for `api`'s managed identity,
   rather than reusing the vault-wide grant already used for the Data Protection key — this
   keeps SSN decryption access separate and auditable from cookie key-ring access.

## Runtime design

### `IEncryptor`

```csharp
public interface IEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string cipherText);
}
```

### Envelope format

Both implementations produce/consume the same self-describing, length-prefixed, Base64-encoded
envelope (`EncryptedEnvelope.Pack`/`Unpack`): `wrappedKey | nonce | tag | ciphertext`. The
production implementation populates `wrappedKey`; the local implementation leaves it empty.
This keeps `Decrypt` environment-agnostic — it doesn't need to know which implementation wrote
the value.

### Production — `KeyVaultEnvelopeEncryptor`

Generates a random AES-256 key per value, encrypts the plaintext with AES-GCM, then wraps the
AES key with the Key Vault RSA key (`WrapKey`/`UnwrapKey`, `RsaOaep256`). Only the small AES key
touches Key Vault — the plaintext itself never leaves the process.

```csharp
internal sealed class KeyVaultEnvelopeEncryptor(Uri keyUri, TokenCredential credential) : IEncryptor
{
    private readonly CryptographyClient _cryptographyClient = new(keyUri, credential);

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();

        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];

        using (var aesGcm = new AesGcm(aes.Key, AesGcm.TagByteSizes.MaxSize))
        {
            aesGcm.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);
        }

        var wrapped = _cryptographyClient.WrapKey(KeyWrapAlgorithm.RsaOaep256, aes.Key);

        return EncryptedEnvelope.Pack(wrapped.EncryptedKey, nonce, tag, ciphertext);
    }

    public string Decrypt(string cipherText)
    {
        var (wrappedKey, nonce, tag, ciphertext) = EncryptedEnvelope.Unpack(cipherText);

        var unwrapped = _cryptographyClient.UnwrapKey(KeyWrapAlgorithm.RsaOaep256, wrappedKey);

        var plaintextBytes = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(unwrapped.Key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
```

### Local dev — `ConfiguredKeyEncryptor`

No Key Vault locally, so no wrap/unwrap step — just AES-GCM directly with a fixed key read from
config.

```csharp
internal sealed class ConfiguredKeyEncryptor(byte[] key) : IEncryptor
{
    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[Encoding.UTF8.GetByteCount(plaintext)];

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, Encoding.UTF8.GetBytes(plaintext), ciphertext, tag);

        return EncryptedEnvelope.Pack(wrappedKey: [], nonce, tag, ciphertext);
    }

    public string Decrypt(string cipherText)
    {
        var (_, nonce, tag, ciphertext) = EncryptedEnvelope.Unpack(cipherText);

        var plaintextBytes = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
```

`appsettings.Development.json`:

```json
"Encryption": {
  "Keys": {
    "BowlerPii": { "Key": "<base64 32-byte value>" }
  }
}
```

### DI registration — keyed services

Registered as a **DI-resolved singleton**, not a static/service-locator singleton — keeps it
swappable for tests and consistent with the rest of the codebase's DI-first conventions.

Multiple keys are supported via .NET's built-in **keyed DI services**
(`AddKeyedSingleton`/`[FromKeyedServices]`), keyed by a `SmartEnum` rather than a raw string or
a plain `enum`. `SmartEnum` was chosen over a plain `enum` so each key can carry its own
metadata — the actual Key Vault key name (Bicep resource names are kebab-case, e.g.
`bowlerpii-key`, which doesn't match a PascalCase enum member) and the expected local AES key
size — right on the value, instead of a separate switch/dictionary elsewhere that could drift
out of sync as keys are added. Same pattern already used for
`DiscordAlertSeverity`/`NotificationColor` (`src/Neba.Api/Discord/DiscordAlert.cs`).

```csharp
public sealed class EncryptionKeyName : SmartEnum<EncryptionKeyName>
{
    public static readonly EncryptionKeyName BowlerPii =
        new(nameof(BowlerPii), 0, keyVaultKeyName: "bowlerpii-key", localKeySizeBytes: 32);
    // PaymentCard, later, if ever needed

    private EncryptionKeyName(string name, int value, string keyVaultKeyName, int localKeySizeBytes)
        : base(name, value)
    {
        KeyVaultKeyName = keyVaultKeyName;
        LocalKeySizeBytes = localKeySizeBytes;
    }

    public string KeyVaultKeyName { get; }
    public int LocalKeySizeBytes { get; }
}
```

```csharp
extension(IServiceCollection services)
{
    public IServiceCollection AddEncryption(IConfiguration configuration)
    {
        foreach (var keyName in EncryptionKeyName.List)
        {
            var section = configuration.GetSection($"Encryption:Keys:{keyName.Name}");
            var keyVaultKeyUri = section["KeyVaultKeyUri"];

            services.AddKeyedSingleton<IEncryptor>(keyName, (_, _) => string.IsNullOrEmpty(keyVaultKeyUri)
                ? new ConfiguredKeyEncryptor(Convert.FromBase64String(
                    section["Key"] ?? throw new InvalidOperationException(
                        $"Encryption:Keys:{keyName.Name}:Key is required when KeyVaultKeyUri is not set.")))
                : new KeyVaultEnvelopeEncryptor(new Uri(keyVaultKeyUri), new DefaultAzureCredential()));
        }

        return services;
    }
}
```

**Why keyed DI instead of a key parameter on `Encrypt`/`Decrypt` itself**: a parameter approach
means a single `IEncryptor` implementation holding every key's credentials internally and
branching on the argument — which defeats the purpose of separate keys (one compromised or
buggy class now touches all of them, tests for one key drag in config for all of them). Keyed
DI keeps each key's encryptor a fully separate, independently swappable instance.

**Why `SmartEnum` instead of a plain `enum`**: a plain `enum` would work fine as a DI key by
itself, but has nowhere to hang per-key metadata. `SmartEnum` is only worth it here because a
second key is already anticipated (`PaymentCard`) with its own Key Vault resource name and
possibly its own local key size — if this were permanently a single key with no per-key
metadata, a plain `enum` would be the right, simpler choice.

### Extension methods

```csharp
public static class StringEncryptionExtensions
{
    extension(string plaintext)
    {
        public string Encrypt(IEncryptor encryptor) => encryptor.Encrypt(plaintext);
    }

    extension(string cipherText)
    {
        public string Decrypt(IEncryptor encryptor) => encryptor.Decrypt(cipherText);
    }
}
```

Takes the encryptor explicitly rather than reaching for ambient/static state — "which key" is
decided once, at DI composition time (which converter/class is asking), not passed around as a
string at every call site.

### EF Core integration

Inject the keyed `IEncryptor` into `AppDbContext` and build a `ValueConverter<string, string>`
in `OnModelCreating`, same shape as the existing converters in `Database/Converters`:

```csharp
public sealed class AppDbContext(
    [FromKeyedServices(EncryptionKeyName.BowlerPii)] IEncryptor bowlerPiiEncryptor,
    DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ssnConverter = new ValueConverter<string, string>(
            plaintext => plaintext.Encrypt(bowlerPiiEncryptor),
            cipherText => cipherText.Decrypt(bowlerPiiEncryptor));

        modelBuilder.Entity<Bowler>().Property(b => b.Ssn).HasConversion(ssnConverter);
    }
}
```

A column is always encrypted/decrypted with the same key by construction (the converter wiring
fixes it) — there's no need to encode "which key" into the ciphertext itself, unlike a generic
secrets store.

## Adding a second key later (e.g. `PaymentCard`)

1. Add the `SmartEnum` value: `EncryptionKeyName.PaymentCard`, with its own
   `keyVaultKeyName`/`localKeySizeBytes`.
2. Add a matching Bicep file/AppHost wiring/config section, scoped to whichever app(s)
   actually need to decrypt it (may differ from `BowlerPii`'s access boundary).
3. `AddEncryption`'s loop over `EncryptionKeyName.List` registers it automatically — no other
   registration code changes.
4. Inject a second `[FromKeyedServices(EncryptionKeyName.PaymentCard)] IEncryptor` parameter
   wherever the card-number converter is configured. A class needing both keys just takes both
   parameters — no other change to the pattern.

## Open questions for implementation time

- Confirm AES-GCM/RSA-OAEP-256 choice against whatever compliance requirement ends up driving
  the SSN feature (some frameworks mandate FIPS-validated algorithms specifically).
- Decide `Bowler.Ssn` column type/length in the DB — the encrypted+Base64 envelope is
  significantly longer than the raw 9/11-character SSN.
- Decide whether searching/filtering by SSN is ever needed — this design has no support for
  equality search on encrypted values (would need a separate blind index / HMAC column if so).
- Confirm no logging/audit path anywhere could log the plaintext SSN before encryption or after
  decryption (see the PII redaction convention in `Neba.Api.Compliance` — `[PrivateData]` should
  be applied to any `[LoggerMessage]` parameter carrying it).
