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

- `src/Neba.Api/FeatureFlags.cs` — `internal static class FeatureFlags { internal const string UserRegistration = "UserRegistration"; }`
- Mirror in `Neba.Website.Server` once a UI flag exists (Phase 1 sets up the pattern; no UI flags yet in this plan — see "UI feature flagging" note below).

### 1.3 Custom feature filter — `AllowedEmailFilter`

Location: `src/Neba.Api/Infrastructure/FeatureManagement/AllowedEmailFilter.cs` (new `Infrastructure/FeatureManagement` folder — cross-cutting, not owned by any single feature).

```csharp
[FilterAlias("AllowedEmail")]
internal sealed class AllowedEmailFilter : IContextualFeatureFilter<AllowedEmailContext>
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

internal sealed class AllowedEmailContext
{
    public string? Email { get; init; }
}

internal sealed record AllowedEmailFilterSettings
{
    public IReadOnlyCollection<string> AllowedEmails { get; init; } = [];
}
```

`Email` is nullable because `RegisterEndpoint` stays `AllowAnonymous()` (see Phase 2) — an unauthenticated caller has no claim to supply.

Using `IContextualFeatureFilter<TContext>` (not the plain `IFeatureFilter`) lets the endpoint pass the caller's email explicitly — pulled from `User` claims when a token is present, or `null` when the request is fully anonymous (see Phase 2).

### 1.4 Registration

New `src/Neba.Api/Infrastructure/FeatureManagement/FeatureManagementConfiguration.cs`, following the existing `extension` syntax used by `InfrastructureConfiguration.cs`:

```csharp
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

Same pattern added to `Neba.Website.Server/Program.cs` (`builder.Services.AddFeatureManagement(...)`) even though Phase 2 has no UI flag yet — this satisfies "keep in mind we'll want feature flagging on the UI as well" by establishing the wiring now rather than bolting it on later.

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
- `src/Neba.Api/Infrastructure/FeatureManagement/AllowedEmailFilter.cs` — null/empty-email handling (see 2.1)
- `src/Neba.Api/appsettings.json` — add the `FeatureManagement:UserRegistration` section (see 1.2)
- `src/Neba.Api/Security/Register/RegisterSummary.cs` — add `ProducesProblemDetails(StatusCodes.Status404NotFound)` (or equivalent `Produces(404)`) to the OpenAPI description per the API Endpoint Checklist

### 2.3 Out of scope for this phase

- No UI page for registration yet — nothing changes in `Neba.Website.Server`
- No Azure App Configuration — flags are redeploy-to-change for now
- No new authorization policy or `Configure()` changes — this is deliberately a stopgap built on optional auth, not a real authorization rule (see the gating-mechanism section above)
- No admin UI to toggle flags at runtime

---

## Future: UI feature flagging (not in this plan's scope, noted for follow-up)

Phase 1 registers `Microsoft.FeatureManagement` in `Neba.Website.Server` so the wiring exists, but no Blazor-specific pattern (e.g. a `<FeatureGate>` component wrapping `IVariantFeatureManager`, or flag-aware `AuthenticationStateProvider` claims) is designed yet. When the first UI flag is needed:

- Decide whether Blazor components check `IVariantFeatureManager` directly (server-rendered, simplest) or need a client-visible flag (WASM/interactive client component in `Neba.Website.Client` — would need the flag state passed down via a Blazor cascading parameter or fetched from an API endpoint, since `Neba.Website.Client` can't read server-side `appsettings.json` directly).
- Revisit whether Azure App Configuration becomes worthwhile once flags need to change without a redeploy across both API and UI simultaneously.

---

## Summary of new/changed files

**New:**

- `src/Neba.Api/FeatureFlags.cs`
- `src/Neba.Api/Infrastructure/FeatureManagement/AllowedEmailFilter.cs`
- `src/Neba.Api/Infrastructure/FeatureManagement/FeatureManagementConfiguration.cs`
- `tests/Neba.Api.Tests/Infrastructure/FeatureManagement/AllowedEmailFilterTests.cs`

**Changed:**

- `Directory.Packages.props` — add `Microsoft.FeatureManagement` + `.AspNetCore`
- `src/Neba.Api/Neba.Api.csproj` — package reference
- `src/Neba.Website.Server/Neba.Website.Server.csproj` — package reference
- `src/Neba.Api/Program.cs` — `.AddFeatureManagement()` in the builder chain
- `src/Neba.Website.Server/Program.cs` — `AddFeatureManagement()` registration
- `src/Neba.Api/appsettings.json` — `FeatureManagement` section
- `src/Neba.Website.Server/appsettings.json` — empty/placeholder `FeatureManagement` section
- `src/Neba.Api/Security/Register/RegisterEndpoint.cs` — flag check + 404 path
- `src/Neba.Api/Security/Register/RegisterSummary.cs` — document 404 response
- Register endpoint test file — new test cases for gated/ungated email
