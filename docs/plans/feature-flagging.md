# Feature Flagging Plan

## Overview

Adopt `Microsoft.FeatureManagement` as the feature-flagging library for both `Neba.Api` and `Neba.Website.Server`. Flags are defined in `appsettings.json` (no Azure App Configuration for now — revisit if runtime toggling without redeploy becomes a real need). This plan covers two steps:

1. **Infrastructure**: bring in the library, wire up configuration binding, register feature management in both the API and the Website composition roots, and establish the pattern (custom filters, naming, config shape) that all future flags will follow.
2. **First flag — `UserRegistration`**: gate the existing `POST register` endpoint (`src/Neba.Api/Security/Register/RegisterEndpoint.cs`) so that only a *caller* whose JWT `email` claim is `tech@bowlneba.com` can invoke it. There is no UI for registration yet, so this is API-only enforcement.

**Why Microsoft.FeatureManagement**: first-party library, integrates natively with `IConfiguration` (matches this repo's existing options pattern), supports custom `IFeatureFilter` implementations for the email-based targeting we need, and has an ASP.NET Core package (`Microsoft.FeatureManagement.AspNetCore`) with `IVariantFeatureManager` for use in FastEndpoints and Blazor alike.

---

## Gating mechanism: optional-auth claim check, `AllowAnonymous()` stays as-is

`RegisterEndpoint` keeps `AllowAnonymous()` — no policy requirement is added, and the route remains reachable without a token. This is intentional and treated as an **interim** approach until real self-service registration rules exist: the endpoint still runs ASP.NET Core's authentication middleware even under `AllowAnonymous()` (that attribute only skips *authorization*, not authentication), so if the caller happens to send a valid bearer token, `User` is populated with its claims; if no token is sent, `User.Identity?.IsAuthenticated` is `false` and there are no claims.

The feature check reads the caller's JWT `email` claim (`User.FindFirstValue(JwtRegisteredClaimNames.Email)`) when present and compares it to the allow-list:

- Caller sends a valid token with `email == tech@bowlneba.com` → flag enabled, registration proceeds.
- Caller sends no token, an invalid token, or a token with a different email → flag disabled → 404.

`JwtTokenService.CreateTokenPair` (`src/Neba.Api/Security/JwtTokenService.cs:25`) already emits `JwtRegisteredClaimNames.Email` on every token, so no token-shape change is needed. This is a stopgap: it relies on `tech@bowlneba.com` already being a logged-in user calling an anonymous endpoint with their token attached, which is unusual but requires no endpoint/policy changes and is easy to relax later (e.g. once a real "who can invite new members" rule exists).

---

## Phase 1: Bring in Microsoft.FeatureManagement + basic structure

### 1.1 Package references

Add to `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.FeatureManagement" Version="4.0.0" />
<PackageVersion Include="Microsoft.FeatureManagement.AspNetCore" Version="4.0.0" />
```

(Pin to whatever the latest stable 4.x release is at implementation time — confirm compatibility with `net10.0`.)

Add `<PackageReference Include="Microsoft.FeatureManagement.AspNetCore" />` to:

- `src/Neba.Api/Neba.Api.csproj`
- `src/Neba.Website.Server/Neba.Website.Server.csproj`

### 1.2 Configuration shape

Standard `Microsoft.FeatureManagement` config section, in each project's `appsettings.json`:

```json
{
  "FeatureManagement": {
    "UserRegistration": {
      "EnabledFor": [
        {
          "Name": "AllowedEmail",
          "Parameters": {
            "AllowedEmails": [ "tech@bowlneba.com" ]
          }
        }
      ]
    }
  }
}
```

Flag names are `PascalCase` and match a `static class FeatureFlags` constants holder (avoid magic strings), mirroring how `SecurityConfiguration.AuthenticatedPolicy` is a const today.

- `src/Neba.Api.Contracts/FeatureFlags.cs` — `public static class FeatureFlags { public const string UserRegistration = "UserRegistration"; }`. **Public and in `Contracts`, not `internal` in `Neba.Api`** — `Neba.Website.Server` already references `Neba.Api.Contracts` (`Neba.Website.Server.csproj:18`), and flag names need to be usable from both processes so a UI check and an API check for the same flag can't drift.

### 1.3 Custom feature filter — `AllowedEmailFilter`

**Same sharing concern applies to the filter, not just the flag name.** `Microsoft.FeatureManagement` filters are registered into each process's own DI container — `Neba.Api` and `Neba.Website.Server` are separate processes with separate `appsettings.json` and separate `AddFeatureManagement()` calls. If a Blazor page ever wants to gate on "is the current caller's email on the allow-list" (e.g. hiding an admin nav link), the Website needs the *same filter type* registered in its own container — reusing `Neba.Api`'s `internal` filter wouldn't be visible across the assembly boundary. So `AllowedEmailFilter` (and its context/settings types) live in `Neba.Api.Contracts` too, next to `FeatureFlags`, as `public`:

Location: `src/Neba.Api.Contracts/FeatureManagement/AllowedEmailFilter.cs` (new `FeatureManagement` folder in `Contracts` — shared, not owned by either process).

`Neba.Api.Contracts.csproj` needs a new `<PackageReference Include="Microsoft.FeatureManagement" />` (the core package, not `.AspNetCore` — `Contracts` shouldn't depend on ASP.NET Core hosting types).

```csharp
namespace Neba.Api.Contracts.FeatureManagement;

[FilterAlias("AllowedEmail")]
public sealed class AllowedEmailFilter : IContextualFeatureFilter<AllowedEmailContext>
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, AllowedEmailContext appContext)
    {
        if (string.IsNullOrEmpty(appContext.Email))
            return Task.FromResult(false);

        var settings = context.Parameters.Get<AllowedEmailFilterSettings>() ?? new AllowedEmailFilterSettings();
        var allowed = settings.AllowedEmails.Contains(appContext.Email, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(allowed);
    }
}

public sealed class AllowedEmailContext
{
    public string? Email { get; init; }
}

public sealed record AllowedEmailFilterSettings
{
    public IReadOnlyCollection<string> AllowedEmails { get; init; } = [];
}
```

`Email` is nullable because `RegisterEndpoint` stays `AllowAnonymous()` (see Phase 2) — an unauthenticated caller has no claim to supply. All four types are `public` (not `internal`) since they cross the `Neba.Api` / `Neba.Website.Server` assembly boundary via `Neba.Api.Contracts`.

Using `IContextualFeatureFilter<TContext>` (not the plain `IFeatureFilter`) lets the endpoint pass the caller's email explicitly — pulled from `User` claims when a token is present, or `null` when the request is fully anonymous (see Phase 2).

### 1.4 Registration

Both processes register `Microsoft.FeatureManagement` plus the shared `AllowedEmailFilter` from `Neba.Api.Contracts` — this is not forward-looking scaffolding, it's needed now so the Website *can* reuse the same filter/flag the moment a UI check is added, without a second implementation to keep in sync.

`src/Neba.Api/Infrastructure/FeatureManagement/FeatureManagementConfiguration.cs`, following the existing `extension` syntax used by `InfrastructureConfiguration.cs`:

```csharp
using Neba.Api.Contracts.FeatureManagement;

namespace Neba.Api.Infrastructure.FeatureManagement;

internal static class FeatureManagementConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddFeatureManagement()
        {
            builder.Services
                .AddFeatureManagement(builder.Configuration.GetSection("FeatureManagement"))
                .AddFeatureFilter<AllowedEmailFilter>();

            return builder;
        }
    }
}
```

Wire into `src/Neba.Api/Program.cs` alongside the existing chain:

```csharp
builder.AddInfrastructure().AddSecurity().AddFeatureManagement();
```

`src/Neba.Website.Server/Program.cs` gets the equivalent call (mirroring whatever local extension-method convention `Neba.Website.Server` already uses for its own registrations, e.g. `AddApiServices`/`AddAccountServices`), registering `Microsoft.FeatureManagement` against its own `appsettings.json` `FeatureManagement` section and the same `AllowedEmailFilter`:

```csharp
builder.Services
    .AddFeatureManagement(builder.Configuration.GetSection("FeatureManagement"))
    .AddFeatureFilter<AllowedEmailFilter>();
```

Each process still evaluates the flag independently against its *own* `appsettings.json` — the config section (allow-list) needs to be kept in sync between `src/Neba.Api/appsettings.json` and `src/Neba.Website.Server/appsettings.json` by hand for now (no shared/centralized config source yet — see the Azure App Configuration note below).

### 1.5 Testing

- Unit test `AllowedEmailFilterTests` — evaluate with matching email (true), non-matching (false), case-insensitive match, empty `AllowedEmails` (false). Use `[UnitTest]`/`[Component("FeatureManagement")]` traits per repo conventions.
- No integration test needed for Phase 1 registration wiring itself (covered indirectly by Phase 2's endpoint tests).

---

## Phase 2: First flag — `UserRegistration`

### 2.1 Where the check happens

Per the endpoint-level decision: check the flag inside `RegisterEndpoint.HandleAsync`, before dispatching to `RegisterCommandHandler`. `Configure()` is unchanged — `AllowAnonymous()` stays. The email passed to the filter comes from the caller's claims if a token was sent, or `null` otherwise (no token → no claim → filter evaluates false). On a disallowed/missing email, return **404 Not Found** (hide that the endpoint exists — there's no UI and it isn't meant to be discoverable yet).

```csharp
public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
{
    var callerEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email);
    var context = new AllowedEmailContext { Email = callerEmail };
    if (!await _featureManager.IsEnabledAsync(FeatureFlags.UserRegistration, context))
    {
        await Send.NotFoundAsync(ct);
        // Stryker disable once Statement
        return;
    }

    var command = new RegisterCommand { Email = req.Input.Email, Password = req.Input.Password };
    // ...unchanged...
}
```

`AllowedEmailContext.Email` becomes nullable (`string?`) to accommodate the no-token case, and `AllowedEmailFilter.EvaluateAsync` treats `null`/empty as never matching (no allocation of a "null" comparison edge case — `Contains` against a null needle should short-circuit to `false` explicitly rather than relying on `Contains(null)` behavior).

`RegisterEndpoint` gains a constructor dependency on `IVariantFeatureManager` (or `IFeatureManager` — confirm which is idiomatic for contextual filters in the pinned package version during implementation).

### 2.2 Files touched

- `src/Neba.Api/Security/Register/RegisterEndpoint.cs` — add flag check (as above); `Configure()` untouched
- `src/Neba.Api/Security/Register/RegisterEndpointTests.cs` (or wherever existing endpoint tests live) — add cases: no bearer token → 404 before handler is ever invoked; token present but email doesn't match → 404; token present with `email == tech@bowlneba.com` → falls through to existing success/error branches. `MockBehavior.Strict` on the command handler mock proves it's never invoked in the blocked cases.
- `src/Neba.Api.Contracts/FeatureManagement/AllowedEmailFilter.cs` — null/empty-email handling (see 2.1); this file is created in Phase 1, not new in Phase 2
- `src/Neba.Api/appsettings.json` — add the `FeatureManagement:UserRegistration` section (see 1.2)
- `src/Neba.Website.Server/appsettings.json` — add the same `FeatureManagement:UserRegistration` section, kept in sync by hand (see 1.4)
- `src/Neba.Api/Security/Register/RegisterSummary.cs` — add `ProducesProblemDetails(StatusCodes.Status404NotFound)` (or equivalent `Produces(404)`) to the OpenAPI description per the API Endpoint Checklist

### 2.3 Out of scope for this phase

- No UI page for registration yet — nothing changes in `Neba.Website.Server`
- No Azure App Configuration — flags are redeploy-to-change for now
- No new authorization policy or `Configure()` changes — this is deliberately a stopgap built on optional auth, not a real authorization rule (see the gating-mechanism section above)
- No admin UI to toggle flags at runtime

---

## Future: UI feature flagging (not in this plan's scope, noted for follow-up)

Phase 1 registers `Microsoft.FeatureManagement` and the shared `AllowedEmailFilter` in `Neba.Website.Server` so both the package wiring and the filter logic already exist there — a Blazor page can call `IVariantFeatureManager.IsEnabledAsync(FeatureFlags.UserRegistration, new AllowedEmailContext { Email = ... })` today using the current user's email from the Blazor auth cookie claims (`identity-implementation.md` confirms the cookie already carries an `email` claim). What's still undesigned is the *Blazor-specific ergonomics* around that call:

- No `<FeatureGate>`-style component wrapping `IVariantFeatureManager` yet — each page/component would call the feature manager directly for now.
- `Neba.Website.Server` is server-rendered, so `IVariantFeatureManager` is trivially available via DI in `@code` blocks. A client-visible flag (interactive WASM component in `Neba.Website.Client`) is a different problem — `Neba.Website.Client` can't read server-side `appsettings.json` or resolve server-only services directly, so that case would need the flag state passed down via a cascading parameter or fetched from an API endpoint. Out of scope until a client-rendered flag is actually needed.
- Revisit whether Azure App Configuration becomes worthwhile once flags need to change without a redeploy across both API and UI simultaneously — right now `appsettings.json` in each project must be kept in sync by hand (Phase 1, 1.4).

---

## Summary of new/changed files

**New:**

- `src/Neba.Api.Contracts/FeatureFlags.cs` — already created (public flag-name constants, shared by both processes)
- `src/Neba.Api.Contracts/FeatureManagement/AllowedEmailFilter.cs` — filter + `AllowedEmailContext` + `AllowedEmailFilterSettings`, public, shared by both processes
- `src/Neba.Api/Infrastructure/FeatureManagement/FeatureManagementConfiguration.cs`
- `tests/Neba.Api.Tests/Infrastructure/FeatureManagement/AllowedEmailFilterTests.cs` (or under a `Neba.Api.Contracts.Tests` project if one exists — confirm at implementation time)

**Changed:**

- `Directory.Packages.props` — add `Microsoft.FeatureManagement` + `.AspNetCore`
- `src/Neba.Api.Contracts/Neba.Api.Contracts.csproj` — `Microsoft.FeatureManagement` package reference (core package only)
- `src/Neba.Api/Neba.Api.csproj` — `Microsoft.FeatureManagement.AspNetCore` package reference
- `src/Neba.Website.Server/Neba.Website.Server.csproj` — `Microsoft.FeatureManagement.AspNetCore` package reference
- `src/Neba.Api/Program.cs` — `.AddFeatureManagement()` in the builder chain
- `src/Neba.Website.Server/Program.cs` — equivalent `AddFeatureManagement()` registration, using the shared `AllowedEmailFilter`
- `src/Neba.Api/appsettings.json` — `FeatureManagement` section
- `src/Neba.Website.Server/appsettings.json` — same `FeatureManagement` section, kept in sync by hand
- `src/Neba.Api/Security/Register/RegisterEndpoint.cs` — flag check + 404 path
- `src/Neba.Api/Security/Register/RegisterSummary.cs` — document 404 response
- Register endpoint test file — new test cases for gated/ungated email
