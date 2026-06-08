# Identity Implementation Plan

## Overview

Identity lives in `Neba.Api` under a top-level `Security/` folder — a sibling to `Features/`, not inside it. The API is the single authentication authority: it issues JWT access tokens and refresh tokens. `Neba.Website.Server` is a consumer — its Blazor Account pages call the API's security endpoints, exchange credentials for a token pair, and store the result in a server-side auth cookie that drives Blazor's `AuthenticationStateProvider`.

**Why API-first over Website-first:**
- Single auth authority for the web frontend, future mobile apps, and third-party API consumers
- Consistent with the existing architecture where the Website is already a thin consumer of the API
- External providers (Google, Facebook) and 2FA can be added to the API independently of the Blazor UI

**Day 1 scope:** email + password, passkeys (.NET 10 built-in), Admin role, admin-created accounts only.  
**Future scope:** Google/Facebook/Microsoft OAuth (Phase 6), 2FA/TOTP (Phase 7), member self-registration with USBC ID linking (Phase 8).

---

## Authentication Flow

```
bowlneba.com/account/login
        │
        │  POST /security/login (email + password)
        ▼
   Neba.Api Security layer
        │  validates credentials via UserManager
        │  issues { accessToken (JWT, 15 min), refreshToken (7 days) }
        ▼
Neba.Website.Server Login.razor
        │  decodes JWT claims
        │  calls HttpContext.SignInAsync() → sets httpOnly cookie on bowlneba.com
        ▼
Subsequent page loads / API calls
        │  cookie carries claims (userId, email, roles, access_token, refresh_token)
        │  BearerTokenHandler reads access_token claim → sets Authorization: Bearer on Refit calls
        │  on 401: silent refresh via /security/refresh → re-sign-in → retry
        ▼
   Neba.Api (protected endpoints)
        │  validates JWT bearer token
        │  checks required roles
```

---

## Folder Structure

### Neba.Api

Each operation folder mirrors the feature folder pattern: the endpoint maps the contract request to an internal command/query, dispatches to the handler, then maps the internal DTO to the contract response. Commands return `ErrorOr<T>`; queries return DTOs directly.

```
Neba.Api/
├── Security/                                       ← sibling to Features/, NOT inside it
│   ├── Domain/
│   │   ├── ApplicationUser.cs                      ← IdentityUser<Guid> + UsbcId
│   │   ├── ApplicationRole.cs                      ← IdentityRole<Guid>
│   │   └── Roles.cs                                ← static constants: Admin, ScoreKeeper, Member
│   ├── Login/
│   │   ├── LoginCommand.cs                         ← internal: ICommand<LoginDto>
│   │   ├── LoginCommandHandler.cs                  ← internal: ICommandHandler<LoginCommand, LoginDto>
│   │   ├── LoginDto.cs                             ← internal DTO returned by handler
│   │   ├── LoginEndpoint.cs                        ← maps LoginRequest→LoginCommand, LoginDto→LoginResponse
│   │   ├── LoginRequestValidator.cs                ← structural validation only (no DB lookups)
│   │   └── LoginSummary.cs
│   ├── Register/
│   │   ├── RegisterCommand.cs                      ← internal: ICommand
│   │   ├── RegisterCommandHandler.cs
│   │   ├── RegisterEndpoint.cs
│   │   ├── RegisterRequestValidator.cs
│   │   └── RegisterSummary.cs
│   ├── RefreshToken/
│   │   ├── RefreshTokenCommand.cs                  ← internal: ICommand<LoginDto> (reuses LoginDto)
│   │   ├── RefreshTokenCommandHandler.cs
│   │   ├── RefreshTokenEndpoint.cs
│   │   ├── RefreshTokenRequestValidator.cs
│   │   └── RefreshTokenSummary.cs
│   ├── Logout/
│   │   ├── LogoutCommand.cs                        ← internal: ICommand
│   │   ├── LogoutCommandHandler.cs
│   │   ├── LogoutEndpoint.cs
│   │   └── LogoutSummary.cs
│   ├── Me/
│   │   ├── GetCurrentUserQuery.cs                  ← internal: IQuery<UserDto>
│   │   ├── GetCurrentUserQueryHandler.cs
│   │   ├── UserDto.cs                              ← internal DTO
│   │   ├── MeEndpoint.cs                           ← maps UserDto→MeResponse
│   │   └── MeSummary.cs
│   ├── Password/
│   │   ├── ChangePassword/
│   │   │   ├── ChangePasswordCommand.cs            ← internal: ICommand
│   │   │   ├── ChangePasswordCommandHandler.cs
│   │   │   ├── ChangePasswordEndpoint.cs
│   │   │   ├── ChangePasswordRequestValidator.cs
│   │   │   └── ChangePasswordSummary.cs
│   │   ├── ForgotPassword/                         ← Phase 3 (same CQRS structure)
│   │   └── ResetPassword/                          ← Phase 3
│   ├── Email/                                      ← Phase 3
│   │   ├── ConfirmEmail/
│   │   └── ResendConfirmation/
│   └── SecurityConfiguration.cs                    ← AddIdentity, AddJwtBearer, seed roles/admin
│
├── Database/
│   ├── AppDbContext.cs                             ← unchanged
│   ├── SecurityDbContext.cs                        ← new IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
│   ├── SecurityDbContextDesignTimeFactory.cs       ← new (for migrations CLI)
│   ├── Configurations/
│   │   └── (existing app configurations unchanged)
│   └── Migrations/
│       ├── (existing app migrations unchanged)
│       └── Security/                               ← new folder, separate migration history
│           └── SecurityDbContextModelSnapshot.cs
```

### Neba.Website.Server

The Blazor Account pages inject `ISecurityApi` from contracts (same pattern as `ISeasonsApi`, `IBowlersApi`, etc.). No separate Refit interface lives in `Website.Server`.

```
Neba.Website.Server/
├── Account/
│   ├── Pages/
│   │   ├── _Imports.razor
│   │   ├── AccessDenied.razor
│   │   ├── Login.razor
│   │   ├── Logout.razor
│   │   ├── Register.razor
│   │   ├── RegisterConfirmation.razor
│   │   ├── ForgotPassword.razor
│   │   ├── ForgotPasswordConfirmation.razor
│   │   ├── ResetPassword.razor
│   │   ├── ResetPasswordConfirmation.razor
│   │   └── Manage/
│   │       ├── _Imports.razor
│   │       ├── Index.razor                         ← profile: display email, roles, link USBC ID
│   │       ├── ChangePassword.razor
│   │       └── (passkey management — Day 1)
│   ├── Shared/
│   │   ├── ManageLayout.razor
│   │   ├── ManageNavMenu.razor
│   │   └── StatusMessage.razor
│   └── AccountConfiguration.cs                    ← AddAuthentication, cookie options, CascadingAuthState
```

### Neba.Api.Contracts

Shared request/response types and the Refit interface all live here — the same pattern used by every other API surface (`ISeasonsApi`, `IBowlersApi`, etc.). Each operation gets its own subfolder matching the `Security/` folder structure in `Neba.Api`.

```
Neba.Api.Contracts/
└── Security/
    ├── ISecurityApi.cs                             ← Refit interface (follows ISeasonsApi pattern)
    ├── Login/
    │   ├── LoginRequest.cs
    │   └── LoginResponse.cs
    ├── Register/
    │   └── RegisterRequest.cs
    ├── RefreshToken/
    │   ├── RefreshTokenRequest.cs
    │   └── RefreshTokenResponse.cs                 ← same shape as LoginResponse
    ├── ChangePassword/
    │   └── ChangePasswordRequest.cs
    └── Me/
        └── MeResponse.cs
```

---

## Phase 1: Neba.Api — Identity Infrastructure

### 1.1 NuGet packages

Add to `Directory.Packages.props` and `Neba.Api.csproj`:

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core store |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT bearer middleware |
| `System.IdentityModel.Tokens.Jwt` | JWT creation / signing |

### 1.2 ApplicationUser

```csharp
// Security/Domain/ApplicationUser.cs
namespace Neba.Api.Security.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Nullable: set when the user links their account to a bowler in the system.
    // Matches Bowler.UsbcId — not a FK across DbContexts; enforced at the application layer.
    public string? UsbcId { get; set; }
}
```

### 1.3 ApplicationRole

```csharp
// Security/Domain/ApplicationRole.cs
namespace Neba.Api.Security.Domain;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
```

### 1.4 Roles constants

```csharp
// Security/Domain/Roles.cs
namespace Neba.Api.Security.Domain;

public static class Roles
{
    public const string Admin       = "Admin";
    public const string ScoreKeeper = "ScoreKeeper"; // Phase 5+
    public const string Member      = "Member";       // Phase 8
}
```

### 1.5 SecurityDbContext

```csharp
// Database/SecurityDbContext.cs
namespace Neba.Api.Database;

internal sealed class SecurityDbContext(DbContextOptions<SecurityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public const string Schema = "security";
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);
    }
}
```

- Separate schema (`security`) — does not conflict with `app` or `historical` schemas in `AppDbContext`
- Same `NpgsqlDataSource` connection as `AppDbContext`
- Uses `UseSnakeCaseNamingConvention()` (configured in `SecurityConfiguration`)
- Separate migrations history table: `security.__EFMigrationsHistory`

### 1.6 SecurityDbContextDesignTimeFactory

```csharp
// Database/SecurityDbContextDesignTimeFactory.cs
namespace Neba.Api.Database;

internal sealed class SecurityDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SecurityDbContext>
{
    public SecurityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=bowlneba;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable(
                    SecurityDbContext.MigrationsHistoryTableName,
                    SecurityDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SecurityDbContext(options);
    }
}
```

### 1.7 SecurityConfiguration.cs

Registers Identity, JWT bearer auth, SecurityDbContext, and seeds roles + initial admin on startup.

```csharp
// Security/SecurityConfiguration.cs
namespace Neba.Api.Security;

internal static class SecurityConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddSecurity()
        {
            // SecurityDbContext uses same data source as AppDbContext
            builder.Services.AddDbContext<SecurityDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options
                    .UseNpgsql(dataSource, npgsql =>
                        npgsql.MigrationsHistoryTable(
                            SecurityDbContext.MigrationsHistoryTableName,
                            SecurityDbContext.Schema))
                    .UseSnakeCaseNamingConvention();
            });

            builder.Services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.SignIn.RequireConfirmedEmail = false; // enable when email sender is wired
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<SecurityDbContext>()
                .AddDefaultTokenProviders()
                .AddPasskeys(); // .NET 10 built-in passkey support

            var jwtSettings = builder.Configuration
                .GetSection("JwtSettings")
                .Get<JwtSettings>()!;

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = jwtSettings.Issuer,
                        ValidAudience            = jwtSettings.Audience,
                        IssuerSigningKey         = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
                    };
                });

            builder.Services.AddAuthorization();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseSecurityInfrastructure()
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }

        // Called at startup to ensure roles and seed admin exist.
        // Uses a scoped IServiceScope; does not block startup if already seeded.
        public static async Task SeedSecurityAsync(WebApplication app) { ... }
    }
}
```

### 1.8 Migration commands

```bash
# Add initial security migration
dotnet ef migrations add Security_Init \
  --context SecurityDbContext \
  --output-dir Database/Migrations/Security \
  --project src/Neba.Api \
  --startup-project src/Neba.Api

# Apply (dev)
dotnet ef database update --context SecurityDbContext --project src/Neba.Api
```

---

## Phase 2: Neba.Api — Auth Endpoints

All endpoints live under `Neba.Api/Security/`, follow the same FastEndpoints conventions as `Features/`, and are versioned consistently with the rest of the API.

### CQRS layer

Each operation follows the same command/query pattern used throughout `Features/`:

| Operation type | Interfaces | Returns |
|---------------|-----------|---------|
| Write (login, register, etc.) | `ICommand<TResponse>` / `ICommandHandler<TCommand, TResponse>` | `ErrorOr<T>` |
| Write (no result) | `ICommand` / `ICommandHandler<TCommand>` | `ErrorOr<Success>` |
| Read (me) | `IQuery<TDto>` / `IQueryHandler<TQuery, TDto>` | `TDto` directly |

The endpoint is the translation layer:
1. Maps the contract request type (`LoginRequest`) to the internal command (`LoginCommand`)
2. Dispatches to the handler via `ICommandHandler` or `IQueryHandler`
3. Maps the internal DTO (`LoginDto`) to the contract response (`LoginResponse`)
4. Handles `ErrorOr` results using the same error-mapping convention as `Features/`

Internal DTOs (e.g., `LoginDto`, `UserDto`) live in the operation folder inside `Neba.Api/Security/` and are never exposed outside the assembly. Contract types (`LoginRequest`, `LoginResponse`) live in `Neba.Api.Contracts/Security/` and are shared with the Website via Refit.

### Endpoint summary

| Verb | Route | Auth | Day 1 |
|------|-------|------|-------|
| `POST` | `/security/register` | Anonymous | ✓ (admin-created; self-registration Phase 8) |
| `POST` | `/security/login` | Anonymous | ✓ |
| `POST` | `/security/refresh` | Anonymous + refresh token | ✓ |
| `POST` | `/security/logout` | Authenticated | ✓ |
| `GET`  | `/security/me` | Authenticated | ✓ |
| `POST` | `/security/password/change` | Authenticated | ✓ |
| `POST` | `/security/password/forgot` | Anonymous | Phase 3 |
| `POST` | `/security/password/reset` | Anonymous + reset token | Phase 3 |
| `POST` | `/security/email/confirm` | Anonymous + confirm token | Phase 3 |
| `POST` | `/security/email/resend` | Anonymous | Phase 3 |
| `POST` | `/security/passkeys/options` | Anonymous | ✓ (.NET 10) |
| `POST` | `/security/passkeys/register` | Authenticated | ✓ (.NET 10) |
| `POST` | `/security/passkeys/login` | Anonymous | ✓ (.NET 10) |

### JWT access token claims

| Claim | Value |
|-------|-------|
| `sub` | userId (Guid as string) |
| `email` | user's email address |
| `roles` | array of role name strings |
| `usbc_id` | USBC ID if linked (omitted when null) |
| `iss` | `https://api.bowlneba.com` |
| `aud` | `https://bowlneba.com` |
| `iat` / `exp` | issued-at / expiry (15 minutes) |

### Login response shape (Neba.Api.Contracts)

```json
{
  "accessToken":  "eyJ...",
  "refreshToken": "...",
  "expiresAt":    "2026-06-08T14:15:00Z",
  "userId":       "3fa85f64-...",
  "email":        "admin@bowlneba.com",
  "roles":        ["Admin"]
}
```

### Refresh token storage

Stored via `IUserTokenStore` in `AspNetUserTokens` (Identity's built-in token table, in the `security` schema), provider name `"RefreshToken"`. Hashed at rest. 7-day rotation window; issuing a new access token also rotates the refresh token.

---

## Phase 3: Neba.Website.Server — Auth Layer

### 3.1 NuGet packages

No new packages required — cookie auth is built into ASP.NET Core. `ISecurityApi` uses the existing Refit setup.

### 3.2 ISecurityApi (Neba.Api.Contracts)

The Refit interface lives in `Neba.Api.Contracts/Security/ISecurityApi.cs`, following the same pattern as `ISeasonsApi`, `IBowlersApi`, etc. The Website injects it directly — no separate wrapper interface in `Website.Server`.

```csharp
// Neba.Api.Contracts/Security/ISecurityApi.cs
namespace Neba.Api.Contracts.Security;

public interface ISecurityApi
{
    [Post("/security/login")]
    Task<IApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Post("/security/register")]
    Task<IApiResponse> RegisterAsync([Body] RegisterRequest request, CancellationToken ct = default);

    [Post("/security/refresh")]
    Task<IApiResponse<LoginResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request, CancellationToken ct = default);

    [Post("/security/logout")]
    Task<IApiResponse> LogoutAsync(CancellationToken ct = default);

    [Get("/security/me")]
    Task<IApiResponse<MeResponse>> GetCurrentUserAsync(CancellationToken ct = default);

    [Post("/security/password/change")]
    Task<IApiResponse> ChangePasswordAsync([Body] ChangePasswordRequest request, CancellationToken ct = default);
}
```

Registered alongside existing `ISeasonsApi`, `IBowlersApi`, etc. in `ApiServicesConfiguration.cs`.

### 3.4 Cookie auth setup

```csharp
// Account/AccountConfiguration.cs
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath          = "/account/login";
        options.LogoutPath         = "/account/logout";
        options.AccessDeniedPath   = "/account/access-denied";
        options.ExpireTimeSpan     = TimeSpan.FromDays(7);
        options.SlidingExpiration  = true;
        options.Cookie.HttpOnly    = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite    = SameSiteMode.Strict;
    });

builder.Services.AddCascadingAuthenticationState();
```

### 3.5 Login.razor flow

1. User submits email + password (SSR POST via `[SupplyParameterFromForm]`)
2. `Login.razor` calls `ISecurityApi.LoginAsync()`
3. Decode the JWT payload (no signature validation needed — the API already validated it)
4. Extract claims: userId, email, roles, usbc_id
5. Store the raw `accessToken` and `refreshToken` as custom claims in the `ClaimsPrincipal`
6. Call `HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)`
7. Redirect to return URL

The cookie is httpOnly so the access token is never readable by JavaScript.

### 3.6 BearerTokenHandler

Reads the access token from the current user's claims and attaches it to all outbound Refit calls:

```csharp
// Services/BearerTokenHandler.cs
public sealed class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var token = user?.FindFirst("access_token")?.Value;

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Attempt silent refresh, re-sign-in, retry once
        }

        return response;
    }
}
```

Registered on all Refit HTTP clients in `ApiServicesConfiguration.cs`.

### 3.7 Routes.razor — add CascadingAuthenticationState

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(NotFound)">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
    </Router>
</CascadingAuthenticationState>
```

Using `AuthorizeRouteView` instead of `RouteView` enables `[Authorize]` on individual Blazor pages.

---

## Phase 4: Protect API Endpoints

### 4.1 Add UseAuthentication / UseAuthorization to Neba.Api Program.cs

`UseSecurityInfrastructure()` (from `SecurityConfiguration`) inserts `UseAuthentication()` and `UseAuthorization()` before `UseFastEndpoints()`.

### 4.2 FastEndpoints authorization convention

Annotate each existing endpoint explicitly — never implicit:

```csharp
// Existing public query endpoints
public override void Configure()
{
    Get("/tournaments");
    AllowAnonymous();
    // ...
}

// Mutation endpoints — Admin only
public override void Configure()
{
    Post("/seasons/{id}/awards/high-average");
    Roles(Roles.Admin);
    // ...
}
```

This is consistent with CLAUDE.md: *"Authorization explicitly configured (never implicit) — use `AllowAnonymous()`, `Roles()`, or `Policies()`."*

### 4.3 Partial page protection (Phase 5+)

Pages with dual read-only / interactive views use `AuthorizeView`:

```razor
<AuthorizeView Roles="@($"{Roles.ScoreKeeper},{Roles.Admin}")">
    <Authorized>
        <ScoreKeeperView />  @* SignalR (WebSocket) *@
    </Authorized>
    <NotAuthorized>
        <ReadOnlyView />     @* SSE stream *@
    </NotAuthorized>
</AuthorizeView>
```

---

## Phase 5: Blazor UI Integration

### 5.1 NavMenu — login/logout links

```razor
<AuthorizeView>
    <Authorized>
        <li class="neba-nav-item">
            <NavLink class="neba-nav-link" href="account/manage">
                @context.User.FindFirst(ClaimTypes.Email)?.Value
            </NavLink>
        </li>
        <li class="neba-nav-item">
            <NavLink class="neba-nav-link" href="account/logout">Log out</NavLink>
        </li>
    </Authorized>
    <NotAuthorized>
        <li class="neba-nav-item">
            <NavLink class="neba-nav-link" href="account/login">Log in</NavLink>
        </li>
    </NotAuthorized>
</AuthorizeView>
```

### 5.2 Protected admin pages

```razor
@attribute [Authorize(Roles = Roles.Admin)]
```

### 5.3 Manage/Index.razor (profile)

- Display email, roles
- Show USBC ID if linked; provide a form to submit a USBC ID for linking (calls API `PUT /security/me/usbc-id`)
- Passkey management section (Day 1 — list registered passkeys, add/remove)

---

## Future Phases

### Phase 6: External Login Providers

- `AddGoogle()`, `AddFacebook()`, `AddMicrosoftAccount()` on Identity builder
- `Account/Pages/ExternalLogin.razor` — handles callback, creates/links `ApplicationUser`
- `Account/Pages/Manage/ExternalLogins.razor` — manage linked providers

### Phase 7: 2FA / TOTP

- `AddTwoFactorTokenProvider<T>()` on Identity builder
- `Account/Pages/LoginWith2fa.razor`
- `Account/Pages/Manage/TwoFactorAuthentication.razor` — enable/disable, show recovery codes
- `Account/Pages/Manage/EnableAuthenticator.razor`
- `Account/Pages/Manage/GenerateRecoveryCodes.razor`

### Phase 8: Member Self-Registration + USBC ID Linking

- Enable `RequireConfirmedEmail` (requires email sender — add `IEmailSender<ApplicationUser>` backed by SendGrid or Azure Communication Services)
- Open `POST /security/register` to anonymous callers
- USBC ID validation: when a member sets their USBC ID, the API verifies it matches a `Bowler` record and that no other `ApplicationUser` has already claimed that ID
- Role `Member` grants access to personalized stats, tournament entry history, etc.

---

## Configuration

### Azure Key Vault secrets (additions to existing vault)

| Secret | Purpose |
|--------|---------|
| `JwtSettings--SigningKey` | HMAC-SHA256 key for JWT signing (min 32 bytes) |
| `JwtSettings--Issuer` | `https://api.bowlneba.com` |
| `JwtSettings--Audience` | `https://bowlneba.com` |
| `Security--SeedAdminEmail` | Initial admin account email |
| `Security--SeedAdminPassword` | Initial admin account password |

### appsettings.json additions (Neba.Api)

```json
{
  "JwtSettings": {
    "Issuer": "https://api.bowlneba.com",
    "Audience": "https://bowlneba.com",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

### User secrets (local dev, Neba.Api)

```json
{
  "JwtSettings:SigningKey": "dev-only-key-replace-in-prod-at-least-32-bytes",
  "Security:SeedAdminEmail": "admin@bowlneba.com",
  "Security:SeedAdminPassword": "P@ssw0rd!"
}
```

---

## Day 1 Checklist

- [ ] **Phase 1** — `ApplicationUser`, `ApplicationRole`, `Roles`, `SecurityDbContext`, `SecurityDbContextDesignTimeFactory`, `SecurityConfiguration` wired into `Program.cs`, initial migration, roles + admin seed
- [ ] **Phase 2** — full CQRS stack (Command/CommandHandler/Dto or Query/QueryHandler/Dto) for `Login`, `Register`, `Refresh`, `Logout`, `Me`, `ChangePassword`; passkey endpoints (`.NET 10`)
- [ ] **Contracts** — `Neba.Api.Contracts/Security/` with `ISecurityApi` + per-operation request/response types in subfolders
- [ ] **Phase 3** — `AccountConfiguration`, `Login.razor`, `Register.razor`, `Manage/Index.razor`, `BearerTokenHandler`, `Routes.razor` updated to `AuthorizeRouteView`; `ISecurityApi` registered in `ApiServicesConfiguration`
- [ ] **Phase 4** — all existing endpoints annotated with `AllowAnonymous()` or `Roles()`; `UseSecurityInfrastructure()` in `Program.cs`
- [ ] **Phase 5** — NavMenu auth links, `[Authorize(Roles = Roles.Admin)]` on admin pages
