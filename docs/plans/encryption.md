# Sensitive Data Encryption

## Goal

Set the pattern for encrypting sensitive values, starting with bowler SSNs:

- One key per data boundary. `BowlerPii` is the first.
- Only `api` can decrypt.
- Legacy SSNs from the management software can be decrypted and encrypted again with the new key.
- A browser can encrypt a value before it leaves the page.
- Anyone who clones the repo can run and test it locally.

This is separate from ASP.NET Core Data Protection, which protects the shared sign-in cookie keys. Do not reuse that key or mechanism.

## Scope

| Iteration 1 (this plan) | Iteration 2 | With the first SSN entry page |
| --- | --- | --- |
| Second Key Vault, key and secrets | `Bowler` columns and unique index | Public key endpoint |
| Encryptor and hasher | SSN in the legacy sync jobs | Page wiring for the browser module |
| Legacy decryptor | One-off backfill | API handling of browser input |
| Browser encryption module and wire format | | |
| Tests | | |

Iteration 1 adds no columns, endpoints or pages, and moves no SSN data.

## When to add a key

Add a key when any of these differ from an existing key's data:

- **Rules:** for example, SSNs and payment cards have different audit needs.
- **Access:** who may decrypt.
- **Lifecycle:** the data must be rotated or purged on its own.
- **Damage:** a leaked key should expose only one set of data.

Fields that share all four can share a key. `StaffPii` is a hypothetical second key; do not build it now. For payment cards, prefer a processor's tokens over storing card numbers.

## Algorithms

Envelope encryption:

1. Make a new AES-256 key and nonce for each value.
2. Encrypt the value with AES-GCM.
3. Wrap the AES key with the RSA key, using RSA-OAEP with SHA-256.

Only the wrap and unwrap steps use the RSA key. Anyone with the public key can encrypt; only the holder of the private key can decrypt.

## Stored and wire format

Base64 of these bytes:

| Field | Size |
| --- | --- |
| Format version (`1`) | 1 byte |
| Key version length | 1 byte |
| Key version (UTF-8) | variable |
| Wrapped key length (big-endian) | 2 bytes |
| Wrapped key | 256 bytes for RSA 2048 |
| Nonce | 12 bytes |
| Tag | 16 bytes |
| Ciphertext | rest |

The AES-GCM additional data is the UTF-8 text `v1|{keyName}|{purpose}|{keyVersion}`. `purpose` is `storage` or `wire`.

- A value fails to decrypt under another key name or purpose. A stored value copied from the database cannot be submitted as browser input.
- The format does not prove who wrote a value, since the public key is public. Treat all browser input as untrusted.
- A 9-digit value takes about 450 characters. Use a `text` column.

WebCrypto returns the tag appended to the ciphertext, so the browser module must split them.

## Code shape

```csharp
internal interface IEncryptionKey
{
    static abstract string Name { get; }
}

internal sealed class BowlerPii : IEncryptionKey
{
    public static string Name => "BowlerPii";
}

internal sealed record Encrypted<TKey>(string Value) where TKey : IEncryptionKey;

internal interface IEncryptor<TKey> where TKey : IEncryptionKey
{
    Task<Encrypted<TKey>> EncryptAsync(string plaintext, CancellationToken ct);
    Task<string> DecryptAsync(Encrypted<TKey> value, CancellationToken ct);
    Task<ErrorOr<string>> DecryptFromClientAsync(string wireValue, CancellationToken ct);
}
```

- A class asks for the key it needs: `IEncryptor<BowlerPii>`. The compiler stops a `BowlerPii` value from going to a `StaffPii` encryptor.
- `DecryptAsync` throws on a bad stored value. `DecryptFromClientAsync` returns one general validation error, with no detail about which step failed.
- One encryptor holds the envelope logic. Only the key wrapper differs by environment: Key Vault or a local RSA key.
- The methods are async because Key Vault calls go over the network.
- No EF value converter. Handlers and jobs encrypt, and entities only hold ciphertext. This avoids a Key Vault call per loaded row, keeps plaintext out of change auditing and leaves `AppDbContext` pooling as it is.

Adding a key means one marker type, one Bicep key, one config section and one registration.

### Where it goes

| Piece | Location |
| --- | --- |
| Keys, encryptor, wrappers, hasher | `src/Neba.Api/Encryption/` |
| Registration | `AddEncryption()` in the `AddInfrastructure()` chain, C# 14 `extension` syntax |
| Legacy decryptor and its setting (`LegacySettings`) | `src/Neba.Api/Legacy/` |
| Browser module and Jest test | `src/Neba.Website.Server/wwwroot/js/` |
| Tests | `tests/Neba.Api.Tests/`, under the matching folders |

The key wrapper is a small internal interface: wrap returns the key version and wrapped key; unwrap takes both. `api` may need a direct package reference to `Azure.Security.KeyVault.Keys`.

Load the `piivault` secrets on their own connection string check. `AddKeyVault()` returns early when the `keyvault` string is blank.

## Hashing for uniqueness

Encrypting the same SSN twice gives different output, so ciphertext cannot enforce uniqueness. Store a second value: HMAC-SHA256 of the 9 digits with a secret hash key.

```csharp
internal interface IHasher<TKey> where TKey : IEncryptionKey
{
    Hashed<TKey> Hash(string value);
}
```

`Hashed<TKey>` holds the Base64 hash. A record prints its values by default, so override `ToString()`.

- The hash has no per-row salt, or duplicates would not match. The secret key protects it. A plain SHA hash would be useless, since every possible SSN can be tried quickly.
- The hash key is separate from the RSA key and never sits in the database.
- Changing the hash key means decrypting every value and hashing it again.
- No search by SSN is planned.

The hash must never leave `api`:

- Its type is internal, its `ToString()` is redacted, and JSON serialization of it throws.
- An architecture test fails if a type in `Neba.Api.Contracts` or an endpoint response has an SSN hash property.
- It stays out of logs, caches and audit records.

## SSN rules

- Stored value: exactly 9 digits (`[0-9]`), no dashes. Browser and legacy input drop dashes and spaces first.
- Check this right after decryption, in the handler or job. Request validators only check the shape of a request and do not decrypt.
- Put `[PrivateData]` on any request property or `[LoggerMessage]` parameter that carries an SSN, encrypted or not.

## Keys and secrets

### A second Key Vault

`web` has Crypto User and Secrets User on the whole existing vault, and loads every secret at startup. A new vault that only `api` references keeps `web` out without changing its permissions.

In `AppHost.cs`, publish mode only:

- Add `piivault`.
- Give `api` a reference to it with Secrets User and Crypto Service Encryption User. Do not reference it from `web`.
- Give the deploy principal Secrets Officer, as on the existing vault.
- Add `bowlerpii-key.bicep`, following `dataprotectionkey.bicep`: RSA 2048, `wrapKey` and `unwrapKey` only, output `keyUriWithVersion`.
- Pass the output to `api` as `Encryption__Keys__BowlerPii__KeyVaultKeyUri`.
- `api` loads the vault's secrets with `AddAzureKeyVaultSecrets("piivault")`.

Turn on purge protection in production, because losing the key loses the data. Leave it off in staging so `destroy-staging.yml` can still purge the vault.

### Secrets

| Vault secret | Aspire parameter | GitHub secret | Use |
| --- | --- | --- | --- |
| `Encryption--Keys--BowlerPii--HashKey` | `bowlerPiiHashKey` | `BOWLER_PII_HASH_KEY` | HMAC key, 32 random bytes in Base64 |
| `Legacy--SsnPassphrase` | `legacySsnPassphrase` | `LEGACY_SSN_PASSPHRASE` | Legacy decryption |

Map both parameters in `cd.yml`, `staging.yml` and `hotfix.yml`. Set both GitHub secrets in the `Production` and `Staging` environments before the first deploy. Staging gets its own hash key.

### Key rotation

The configured key URI names the version used to encrypt. Decryption uses the version stored in the value, so keep old versions enabled. Confirm in staging that a new version reaches `api` on the next deploy.

## Local development

`appsettings.Development.json` holds throwaway values, committed so a fresh clone works:

- `Encryption:Keys:BowlerPii:LocalPrivateKey`: an RSA private key (Base64 PKCS#8). Key version is `local`.
- `Encryption:Keys:BowlerPii:HashKey`
- `Legacy:SsnPassphrase`: a fake value. Use user secrets for the real one.

GitHub secret scanning may flag the private key. Add a comment that it is for development only.

Outside Development, startup fails if the Key Vault key URI, hash key or legacy passphrase is missing, or if a local private key is set. No environment falls back to a local key.

## Legacy decryptor

Lives in `Neba.Api.Legacy`, so it is deleted with the rest of the folder when the management software retires.

Source: `Common/NEBA.Common/Encryption.cs` in nebamgmt-v3.

- Base64 input: 16-byte salt, 16-byte IV, ciphertext
- Key: PBKDF2 with SHA-1, 1,000 iterations, 16 bytes
- AES-128-CBC with PKCS7 padding
- Empty input gives empty output

On 2026-11-27 all 402 stored SSNs were 64 characters, which matches this format. `Reporting/Queries/SSN.linq` uses a different 256-bit block format that .NET 10 cannot read; no stored value uses it.

**The passphrase is weak.** The old code derives it from the GUID of a built-in .NET type, so anyone with the source can work it out. Treat legacy SSN data and its backups as barely protected.

**Capture the passphrase on .NET Framework** (for example LINQPad 5), using the same expression as the old code. .NET 10 is likely to give a different value. Check it by decrypting one real value, and do not write the passphrase or the SSN into the repo, logs or chat.

## Browser encryption module

`wwwroot/js/field-encryption.js`, using WebCrypto only. It takes a value, a public key (SPKI), a key name and a key version, and returns the wire value.

When the first page uses it:

- Encrypt on blur, then show a mask. Editing means typing the value again.
- Never bind the input with `@bind` or `@oninput`, or the plaintext travels over SignalR.
- Only the encrypted value reaches .NET.
- `web` gets the public key from `api` and gets no access to `piivault`.
- `api` decrypts, validates, hashes and encrypts again for storage. It never stores the wire value.

This keeps plaintext out of `web`, SignalR and API request bodies. It does not protect against a compromised browser or a `web` server that serves altered script.

## Tests

Use fake SSNs only (numbers starting with `9`).

### Encryptor

- A value survives encrypt then decrypt.
- The same input gives different output each time.
- A change to any field makes decryption fail.
- Another key cannot decrypt the value.
- A `wire` value fails as `storage`, and the reverse.
- A value from an older key version still decrypts.

### Hasher

- The same input and key give the same hash.
- A different key gives a different hash.
- Serialization throws, and `ToString()` is redacted.

### Legacy decryptor

- Reads samples made by the old code on .NET Framework with a fake passphrase and fake SSNs. Samples made by new code would only test it against itself.

Get the samples before writing these tests: write a LINQPad 5 script that runs the old `Encrypt` code with a made-up passphrase over made-up SSNs, and ask the user to run it and paste the output. Never use the real passphrase. Until the samples arrive, report the decryptor as unproven.

### Browser to API

- A committed sample made by the browser module decrypts in a C# test.
- Jest checks the layout of both that sample and fresh output, so a format change fails the build. Use the `node` test environment for WebCrypto.

### Startup and structure

- Startup fails outside Development when a setting is missing.
- The local settings in the repo load and work.
- The contract architecture test described under hashing.

The Key Vault wrapper has no local emulator. Test its version and URI handling in unit tests, then verify wrap and unwrap in staging.

## Done when

- Unit and integration tests, Jest tests and the build pass.
- A fresh clone runs with `aspire run` and no extra setup.
- Existing tests that start the API still pass with the new startup checks.
- `cd.yml`, `staging.yml` and `hotfix.yml` map both new parameters.
- The AppHost publish output shows `piivault` referenced by `api` only.
- A new ADR records the second vault and envelope encryption.

The user verifies in staging: deploy, then wrap and unwrap with the real vault. Iteration 1 has no code that calls the encryptor, so a healthy deploy proves only that the settings load. This check waits for iteration 2 (see below).

## Iteration 2

- Add `ssn_encrypted` and `ssn_hash` to `bowlers`, always set together, with a unique index on `ssn_hash` where it is not null.
- Exclude both from change auditing.
- Add SSN to `LegacyBowlerRow` and all three paths: new bowler, update and weekly sync.
- Decrypt with the legacy code, check the 9 digits, then encrypt and hash.
- A blank legacy value keeps what the website has.
- A bad value raises an alert, and the rest of the bowler still saves.
- Backfill all bowlers once and always overwrite. The weekly sync only reads recently changed rows.
- The 402 legacy SSNs were unique on 2026-11-27.

### From the iteration 1 review

- Add a check constraint so the two columns are both set or both null: `(ssn_encrypted IS NULL) = (ssn_hash IS NULL)`. Today a half-filled row can be saved.
- Quote the column name in the `HasUniqueHash()` index filter. Without quotes, a prefix with capitals or other special characters would not match its column.
- Test replacing a saved value (also with the same hash) and clearing it, saving and reloading after each.
- Map the test entity with `ConfigureShadowId()`, or cover that key pattern in the `Bowler` mapping test.
- Prove wrap and unwrap in staging before the backfill. Proposed: a health check on `api` that encrypts and decrypts a made-up value and keeps the result for some hours, so probes do not call Key Vault each time. This also proves the Crypto Service Encryption User role is enough.

## After the first production deploy

- Add `microsoft.keyvault/vaults` to the protected types in `.github/scripts/whatif-guard.sh` and cover it in `whatif-guard.test.sh`, so a deploy cannot create a new, empty `piivault` unnoticed. Adding it before the vault exists would block that first deploy.

## Open questions

- Does any compliance rule require FIPS-validated algorithms or modules?
- Iteration 2: how should bowler merge and delete treat SSN, so a removed record does not hold a hash that blocks the surviving one?
- Iteration 2: should the weekly sync rewrite SSN on every run, or only when the hash differs? Rewriting every time creates a change and an audit entry each run.
