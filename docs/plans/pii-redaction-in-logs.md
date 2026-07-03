# PII Redaction in Logs — Implementation Plan

GitHub issue: #90 (`enhancement`, `best practice`, `system`, `security`)

## Overview

Adopt `Microsoft.Extensions.Compliance.Redaction` to systematically redact PII in logs, replacing the ad-hoc `MaskEmail` helper in `src/Neba.Api/Email/GoogleWorkspaceEmailSender.cs` (added as a stopgap fix for a CodeQL "Exposure of private information" finding).

**Scope of this codebase audit**: `GoogleWorkspaceEmailSender.LogEmailSent`'s `toAddress` parameter is currently the **only** `[LoggerMessage]` call site anywhere in the repo that carries genuine PII (email/name/phone/address). A full grep of every `[LoggerMessage]` definition in `src/` turned up no other candidates — everything else logs IDs, durations, document/job names, cache keys, or status codes. This is a narrowly-scoped change: build the taxonomy + DI plumbing correctly so it generalizes, but there is only one real call site to migrate today.

One borderline case worth a footnote, not action: `SlowQueryInterceptor` (`src/Neba.Api/Database/Interceptors/SlowQueryInterceptor.cs:78`) logs raw SQL `{CommandText}`, which could contain literal parameter values. Out of scope per the issue ("Redacting data at rest... this issue is log-output only" implies query text isn't the target either), but flag it in the PR description as a known follow-up candidate.

**Why this matters beyond the one call site**: the goal is the *pattern*, not just this fix — every future `[LoggerMessage]` site that touches a bowler's name/email/phone/address should use the classification attribute by convention instead of someone remembering to hand-roll masking again.

---

## Phase 1: Package references

Add to `Directory.Packages.props`, aligned to the `10.7.0` "Microsoft.Extensions.*" servicing train already pinned there (e.g. `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Http.Resilience`) rather than the `9.0.0` version noted (stale) in `CLAUDE.md` for `Microsoft.Extensions.Diagnostics.Testing`:

```xml
<PackageVersion Include="Microsoft.Extensions.Compliance.Abstractions" Version="10.7.0" />
<PackageVersion Include="Microsoft.Extensions.Compliance.Redaction" Version="10.7.0" />
```

Confirm the exact latest stable version for the `net10.0` train at implementation time (`Microsoft.Extensions.Compliance.*` may not have a matching `10.7.0` release — check NuGet; fall back to the closest compatible version and note the discrepancy).

Add `<PackageReference Include="Microsoft.Extensions.Compliance.Redaction" />` to `src/Neba.Api/Neba.Api.csproj` (the only project with a real call site today). `Microsoft.Extensions.Compliance.Abstractions` is a transitive dependency of `Redaction`, but reference it directly if the taxonomy attribute type needs to live somewhere it's the only compliance package required (see Phase 2 placement decision).

---

## Phase 2: Data classification taxonomy

Define a minimal taxonomy — one classification to start, matching the issue's "at minimum a `PrivateData` classification":

**Location**: `src/Neba.Api/Compliance/DataTaxonomy.cs` (new `Compliance` folder in `Neba.Api`, `internal`, since the only consumer is `Neba.Api` today). If a future call site in `Neba.Website.Server` needs the same classification, promote this to `Neba.Api.Contracts` at that time (mirrors the reasoning already used for `FeatureFlags`/`AllowedEmailFilter` in the feature-flagging plan) — don't do it preemptively.

```csharp
namespace Neba.Api.Compliance;

internal static class DataTaxonomy
{
    private const string TaxonomyName = nameof(DataTaxonomy);

    public static DataClassification PrivateData { get; } = new(TaxonomyName, nameof(PrivateData));
}
```

A companion attribute for use directly on `[LoggerMessage]` parameters:

```csharp
namespace Neba.Api.Compliance;

[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class PrivateDataAttribute : DataClassificationAttribute
{
    public PrivateDataAttribute() : base(DataTaxonomy.PrivateData) { }
}
```

This mirrors the `Microsoft.Extensions.Compliance.Classification` pattern from the reference article (custom taxonomy + custom attribute wrapping `DataClassificationAttribute`), rather than using the built-in generic classification directly — keeps the attribute name (`[PrivateData]`) self-documenting at each call site.

---

## Phase 3: DI registration

New file `src/Neba.Api/Compliance/RedactionConfiguration.cs`, following the repo's `extension(WebApplicationBuilder builder)` idiom (same shape as `EmailConfiguration.cs`):

```csharp
namespace Neba.Api.Compliance;

internal static class RedactionConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public void AddRedaction()
        {
            builder.Services.AddRedaction(options =>
                options.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.PrivateData)));
        }
    }
}
```

Start with `ErasingRedactor` (replaces the value entirely, e.g. `<redacted>`) per the issue's stated starting point. `HmacRedactor` (consistent-but-anonymized hash, useful if we ever want to correlate repeated occurrences of the same redacted value without exposing it) is an explicit non-goal for now — revisit only if a concrete correlation need shows up.

Wire into the composition root: `src/Neba.Api/InfrastructureConfiguration.cs`, `AddInfrastructure()` — add `builder.AddRedaction();` alongside the existing `.AddDatabase().AddKeyVault().AddStorage().AddEmail()` chain (call it before `.AddEmail()` since email logging is the first consumer, though ordering doesn't functionally matter here).

---

## Phase 4: Migrate `GoogleWorkspaceEmailSender`

`src/Neba.Api/Email/GoogleWorkspaceEmailSender.cs`:

1. Delete `MaskEmail` (lines 45-52) entirely.
2. Change the call site (lines 39-42) from:
   ```csharp
   if (logger.IsEnabled(LogLevel.Information))
   {
       logger.LogEmailSent(MaskEmail(message.To), message.Subject);
   }
   ```
   to:
   ```csharp
   if (logger.IsEnabled(LogLevel.Information))
   {
       logger.LogEmailSent(message.To, message.Subject);
   }
   ```
3. Update `GoogleWorkspaceEmailSenderLogMessages.LogEmailSent` to classify the parameter:
   ```csharp
   [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {ToAddress}: {Subject}")]
   public static partial void LogEmailSent(
       this ILogger<GoogleWorkspaceEmailSender> logger,
       [PrivateData] string toAddress,
       string subject);
   ```
   The `[LoggerMessage]` source generator recognizes `DataClassificationAttribute`-derived attributes on parameters and routes the value through the registered `IRedactor` for that classification before formatting — this is what makes the redaction automatic at the message-template level, not just a manual masking call.

---

## Phase 5: Confirm redaction reaches structured logging sinks, not just the formatted string

The issue explicitly calls out: *"Confirm redaction applies correctly when structured logging sinks (e.g. Application Insights) serialize log state, not just the formatted message string."*

`[LoggerMessage]`-generated methods with a classified parameter redact the value used to build **both** the formatted `Message` string and the structured state (`LogRecord`/`IReadOnlyList<KeyValuePair<string, object>>` tags) — the redactor runs before the `ILogger.Log` call is made, so whatever value ends up in either representation is already redacted. There isn't a separate code path to configure for App Insights specifically; it consumes whatever the `ILogger` state contains. Verify this empirically in Phase 6 (test asserts against both `Message` and the structured tag) rather than trusting it blind, since this is exactly the assumption the issue asks us to confirm.

---

## Phase 6: Update tests

`tests/Neba.Api.Tests/Email/GoogleWorkspaceEmailSenderTests.cs`, test `SendAsync_ShouldLog_EmailSent_WithRecipientAndSubject` (lines 123-142):

- Current assertions check `logs[0].Message` against the old masked format (`l***@example.com`) — replace with the `ErasingRedactor`'s actual output format (confirm exact string, e.g. likely a fixed placeholder token) for the redacted segment.
- Add an assertion against the structured state (`FakeLogRecord` exposes structured key/value pairs alongside `Message`) confirming the `ToAddress` tag itself is redacted, not just the formatted message — this is the Phase 5 confirmation, expressed as a test rather than a one-off manual check.
- Assert the real recipient address (`log-target@example.com` or whatever the test's fixture email is) does not appear anywhere in either the message string or the structured tags.
- Register `IRedactor`/`ErasingRedactor` in whatever composition is used to construct the `ILogger<GoogleWorkspaceEmailSender>` for this test (currently `FakeLogger<T>` is constructed directly, not via DI at line 18/25) — `[LoggerMessage]`-generated redaction requires resolving an `IRedactorProvider` from somewhere. Confirm whether `FakeLogger<T>` used standalone still triggers the generated redaction call, or whether the test needs to build a minimal `ServiceCollection` with `.AddRedaction(...)` and resolve `ILogger<GoogleWorkspaceEmailSender>` through it. This is the highest-uncertainty step in the plan — spend time here first as a spike before writing the rest of the migration, since if `FakeLogger<T>` doesn't participate in redaction when constructed directly, the existing test pattern for this class needs to change shape (e.g. resolve the logger via a small DI container in the test instead).

---

## Phase 7: Housekeeping

- Fix the stale `CLAUDE.md` reference to `Microsoft.Extensions.Diagnostics.Testing` version `9.0.0` (actual pinned version is `10.7.0` per `Directory.Packages.props`) while touching nearby `Microsoft.Extensions.*` package documentation.
- Add a short `## PII Redaction in Logs` entry under `CLAUDE.md`'s `## Learnings` section once the pattern is proven, documenting: the taxonomy location, the `[PrivateData]` attribute, the DI registration point, and the "redaction requires an `IRedactorProvider` reachable from wherever the logger is constructed" gotcha discovered in Phase 6 — so the next person adding a PII-carrying `[LoggerMessage]` parameter knows the convention without re-deriving it.
- Do not touch `RefitSettings.ExceptionRedactor` in `src/Neba.Website.Server/Services/ApiServicesConfiguration.cs:38` — unrelated pre-existing HTTP-header-scrubbing mechanism, different from this feature despite the similar name.

---

## Out of scope (per issue)

- Redacting data at rest (DB, blob storage).
- General-purpose PII detection/scanning tooling.
- `HmacRedactor` / correlation-preserving redaction (no current use case).
- `SlowQueryInterceptor` raw SQL command text (flagged as a known follow-up, not actioned here).

## Open questions to resolve during implementation

1. Exact latest stable NuGet version for `Microsoft.Extensions.Compliance.Redaction`/`Abstractions` compatible with `net10.0` (may lag behind the `10.7.0` train other `Microsoft.Extensions.*` packages are on).
2. Whether `FakeLogger<T>` constructed directly (no DI) participates in `[LoggerMessage]`-generated redaction, or whether the test needs a DI-resolved logger — resolve via a Phase 6 spike before writing the full test update.
3. Exact string `ErasingRedactor` produces (needed for the updated test assertion).
