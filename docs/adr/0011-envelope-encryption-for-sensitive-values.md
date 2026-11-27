# ADR-0011: Envelope Encryption and a Second Key Vault for Sensitive Values

## Status

Accepted

## Context

The website must hold bowler SSNs, starting with those the management software stores today under a weak scheme. We want:

- one key per data boundary (`BowlerPii` is the first),
- only `api` able to decrypt,
- a browser able to encrypt a value before it leaves the page,
- a fresh clone that runs and tests without Azure.

`web` holds Crypto User and Secrets User on the existing vault (`keyvault`) and loads every secret in it at startup. Data Protection already uses a key there to protect the shared sign-in cookie keys.

### Options considered

1. **ASP.NET Core Data Protection.** Rejected: its key ring is shared with `web`, it is built for short-lived payloads, and a browser cannot encrypt for it.
2. **A new key in the existing vault.** Rejected: `web` could use it, and narrowing `web`'s roles would change how sign-in works.
3. **Encrypting each value directly with RSA.** Rejected: it limits the value's size and offers no integrity check beyond padding.
4. **A second vault and envelope encryption.** Chosen.

## Decision

### Second vault

`piivault` is referenced by `api` only, with Secrets User and Crypto User. It holds the RSA 2048 wrapping key (`wrapKey` and `unwrapKey` only), the HMAC hash key and the legacy passphrase. Purge protection is on in production, because losing the key loses the data, and off in staging so the environment can be destroyed.

### Envelope encryption

Each value gets a new AES-256 key and nonce, is encrypted with AES-GCM, and the AES key is wrapped with RSA-OAEP (SHA-256). Stored and browser values are Base64 of:

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

The AES-GCM additional data is `v1|{keyName}|{purpose}|{keyVersion}`, where `purpose` is `storage` or `wire`. A value fails to decrypt under another key name or purpose.

### Code

`IEncryptor<TKey>` and `IHasher<TKey>` live in `Neba.Api/Encryption/`. The key type stops one key's values from reaching another key's encryptor. Only the key wrapper differs by environment: Key Vault when deployed, a throwaway RSA key from `appsettings.Development.json` in Development. Outside Development, startup fails if a setting is missing or a local key is set.

There is no EF value converter: handlers and jobs encrypt, and entities hold only ciphertext. That avoids a Key Vault call per loaded row and keeps plaintext out of change auditing.

An entity holds an `EncryptedValue<TKey>` (ciphertext and hash together), mapped with `HasEncryptedValue(expression, name)` to `{name}_encrypted` and `{name}_hash`, with `HasUniqueHash()` for the unique index. It is an owned type rather than a complex property, because EF Core 10 cannot index a property of a complex type.

### Hashing for uniqueness

Ciphertext differs every time, so duplicates are found with HMAC-SHA256 under a secret hash key. The hash never leaves `api`.

### Adding a key

Add a key when its data differs from an existing key's in rules, access, lifecycle or the damage a leak would do. It takes one marker type, one Bicep key, one config section and one registration.

## Consequences

### Positive

- `web` cannot read the keys or secrets, without any change to its permissions.
- A browser value stays encrypted through `web`, SignalR and API request bodies.
- Local development and tests need no Azure resources.

### Negative

- The format does not prove who wrote a value, since the public key is public. Treat all browser input as untrusted.
- A 9-digit value takes about 450 characters.
- Rotation is manual: deploys do not create key versions, old versions must stay enabled, and a value moves to a new version only when it is encrypted again.
- Changing the hash key means decrypting every value and hashing it again.
- Browser encryption does not protect against a compromised browser or a `web` server that serves altered script.
- The Key Vault wrapper has no local emulator; wrap and unwrap are verified in staging.
- Whether a compliance rule requires FIPS-validated algorithms or modules is still open.

## Related Decisions

- [ADR-0002](0002-azure-key-vault-for-secrets-management.md): Azure Key Vault for secrets management
