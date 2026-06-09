# Identity Implementation Plan

## Overview

Identity lives in `Neba.Api` under a top-level `Security/` folder — a sibling to `Features/`, not inside it. The API is the single authentication authority: it issues JWT access tokens and refresh tokens. `Neba.Website.Server` is a consumer — its Blazor Account pages call the API's security endpoints, exchange credentials for a token pair, and store the result in a server-side auth cookie that drives Blazor's `AuthenticationStateProvider`.

**Why API-first over Website-first:**
- Single auth authority for the web frontend, future mobile apps, and third-party API consumers
- Consistent with the existing architecture where the Website is already a thin consumer of the API
- External providers (Google, Facebook) and 2FA can be added to the API independently of the Blazor UI

**Day 1 scope:** email + password, Admin role, admin-created accounts only.  
**Future scope:** Passkeys/WebAuthn (Phase 6), Google/Facebook/Microsoft OAuth (Phase 7), 2FA/TOTP (Phase 8), member self-registration with USBC ID linking (Phase 9).

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

### 1.7 JwtSettings

```csharp
// Security/JwtSettings.cs
namespace Neba.Api.Security;

internal sealed record JwtSettings
{
    public string Issuer                   { get; init; } = string.Empty;
    public string Audience                 { get; init; } = string.Empty;
    public string SigningKey               { get; init; } = string.Empty;
    public int    AccessTokenExpiryMinutes { get; init; } = 15;
    public int    RefreshTokenExpiryDays   { get; init; } = 7;
}
```

### 1.8 SecurityConfiguration.cs

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
                .AddDefaultTokenProviders();

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

### 1.9 Migration commands

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

### 1.10 Email sender (MailKit + Google Workspace SMTP)

**MailKit** is the de facto standard for SMTP in .NET — Microsoft deprecated `System.Net.Mail.SmtpClient` and points users to MailKit in their own docs. It occupies the same "obvious choice" position that FluentValidation holds for validation.

Email is not security-specific — tournament confirmations, notifications, and other future use cases will all share the same sending infrastructure. Following the same pattern as `Storage/`, `Clock/`, and `Caching/`, the email concern lives at the top level of `Neba.Api` as a shared infrastructure folder, not inside `Security/`.

#### Folder placement

```
Neba.Api/
├── Email/                               ← sibling to Storage/, Clock/, Caching/
│   ├── IEmailSender.cs                  ← general-purpose abstraction (application-layer equivalent)
│   ├── EmailMessage.cs                  ← value type carrying to/subject/htmlBody
│   ├── EmailSettings.cs                 ← SMTP configuration options
│   ├── GoogleWorkspaceEmailSender.cs    ← MailKit implementation (infrastructure-layer equivalent)
│   └── IdentityEmailSenderAdapter.cs   ← adapts IEmailSender<ApplicationUser> → IEmailSender
```

In Clean Architecture terms the interface is Application, the implementation is Infrastructure. In this single-project VSA, both live in `Email/` — the folder boundary carries the same conceptual weight as the layer boundary did.

#### Google Workspace admin setup (one-time, before first deploy)

**1. Create the `noreply@bowlneba.com` mailbox**

Create it as a real mailbox — not a dead address. Receiving servers and bounce processors expect to be able to deliver to the envelope sender. A vanished address causes bounces to disappear silently.

- Google Admin Console → Directory → Users → Add user → `noreply@bowlneba.com`
- Alternatively, create a Google Group at `noreply@bowlneba.com` with "Message moderation: Reject" — this satisfies delivery expectations while discarding all inbound.
- If using a mailbox, add a Gmail filter: matches `to:noreply@bowlneba.com` → Delete it. This keeps the mailbox from filling up.

**2. Generate an app password**

- Sign in to `noreply@bowlneba.com` at `myaccount.google.com`
- Security → 2-Step Verification → enable it
- Security → App passwords → name it "BowlNeba API" → copy the 16-character password
- Store in Azure Key Vault as `EmailSettings--AppPassword`

**3. Enable DKIM signing (Google Admin Console)**

- Apps → Google Workspace → Gmail → Authenticate email
- Select `bowlneba.com` → Generate new record (2048-bit)
- Copy the TXT record value — you'll add it to DNS in the next step
- After DNS propagates, return here and click "Start authentication"

#### DNS records (bowlneba.com — add at your registrar)

These three records are what receiving servers (Gmail, Outlook, etc.) check before deciding whether to deliver or drop your email. DKIM and SPF failing against a strict DMARC policy means silent drops.

| Name | Type | Value |
|------|------|-------|
| `@` | TXT | `v=spf1 include:_spf.google.com ~all` — authorizes Google's mail servers to send for your domain. Google Workspace may have already added this when you verified the domain; confirm it exists. |
| `google._domainkey` | TXT | The value generated in the DKIM step above. |
| `_dmarc` | TXT | `v=DMARC1; p=quarantine; rua=mailto:admin@bowlneba.com` — `p=quarantine` sends failing mail to spam rather than rejecting outright; `rua` sends aggregate reports so you can see what's happening. Tighten to `p=reject` once you've confirmed all legitimate senders are covered by SPF/DKIM. |

> **Note:** SPF covers the sending IP (Google's servers); DKIM covers the message content signature. DMARC ties them together. All three must pass for reliable inbox delivery to strict domains.

#### NuGet packages

Add to `Directory.Packages.props` and `Neba.Api.csproj`:

| Package | Purpose |
|---------|---------|
| `MailKit` | SMTP client (brings in `MimeKit` transitively) |

#### IEmailSender (general abstraction)

```csharp
// Email/IEmailSender.cs
namespace Neba.Api.Email;

internal interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
```

#### EmailMessage

```csharp
// Email/EmailMessage.cs
namespace Neba.Api.Email;

// ReplyTo overrides the default from EmailSettings when set — use for transactional
// emails where replies should route to a specific address (e.g. support@bowlneba.com).
internal sealed record EmailMessage(string To, string Subject, string HtmlBody, string? ReplyTo = null);
```

#### EmailSettings

```csharp
// Email/EmailSettings.cs
namespace Neba.Api.Email;

internal sealed class EmailSettings
{
    public string Host           { get; init; } = "smtp.gmail.com";
    public int    Port           { get; init; } = 587;
    public string UserName       { get; init; } = string.Empty; // e.g. noreply@bowlneba.com
    public string AppPassword    { get; init; } = string.Empty; // Google Workspace app password
    public string FromAddress    { get; init; } = string.Empty;
    public string FromName       { get; init; } = "BowlNeba";
    // Default reply-to for all outbound mail. Bounces and replies from users who hit
    // "Reply" will route here instead of the dead noreply inbox.
    public string ReplyToAddress { get; init; } = string.Empty; // e.g. support@bowlneba.com
    public string ReplyToName    { get; init; } = "BowlNeba Support";
}
```

#### GoogleWorkspaceEmailSender

```csharp
// Email/GoogleWorkspaceEmailSender.cs
namespace Neba.Api.Email;

internal sealed class GoogleWorkspaceEmailSender(
    IOptions<EmailSettings> options,
    ILogger<GoogleWorkspaceEmailSender> logger)
    : IEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        // Reply-To: per-message override wins; fall back to the settings default.
        // This routes user replies away from the dead noreply inbox.
        var replyToAddress = message.ReplyTo ?? _settings.ReplyToAddress;
        if (!string.IsNullOrEmpty(replyToAddress))
            mimeMessage.ReplyTo.Add(new MailboxAddress(_settings.ReplyToName, replyToAddress));

        mimeMessage.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, ct);
        await client.SendAsync(mimeMessage, ct);
        await client.DisconnectAsync(quit: true, ct);

        logger.LogEmailSent(message.To, message.Subject);
    }
}

internal static partial class GoogleWorkspaceEmailSenderLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {ToAddress}: {Subject}")]
    public static partial void LogEmailSent(this ILogger logger, string toAddress, string subject);
}
```

#### IdentityEmailSenderAdapter

Adapts ASP.NET Core Identity's `IEmailSender<TUser>` to the general `IEmailSender`. Security injects the adapter; everything else injects `IEmailSender` directly.

```csharp
// Email/IdentityEmailSenderAdapter.cs
namespace Neba.Api.Email;

internal sealed class IdentityEmailSenderAdapter(IEmailSender sender)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        sender.SendAsync(new EmailMessage(
            email,
            "Confirm your BowlNeba account",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        sender.SendAsync(new EmailMessage(
            email,
            "Reset your BowlNeba password",
            $"Reset your password by <a href='{resetLink}'>clicking here</a>."));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        sender.SendAsync(new EmailMessage(
            email,
            "Your BowlNeba password reset code",
            $"Your password reset code is: {resetCode}"));
}
```

#### Registration in InfrastructureConfiguration

Email is infrastructure — it registers alongside `Storage`, `Caching`, etc., not in `SecurityConfiguration`.

```csharp
// InfrastructureConfiguration.cs (existing file — add alongside other infrastructure registrations)
builder.Services
    .Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"))
    .AddTransient<IEmailSender, GoogleWorkspaceEmailSender>()
    .AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();
```

#### RequireConfirmedEmail

`RequireConfirmedEmail` stays `false` for Day 1 (admin-created accounts, no self-registration). It is flipped to `true` in Phase 3 when `ForgotPassword` / `ConfirmEmail` endpoints are wired. The sender will already be registered by then.

#### Azure Key Vault secrets (additions)

| Secret | Purpose |
|--------|---------|
| `EmailSettings--UserName` | Google Workspace sending address (`noreply@bowlneba.com`) |
| `EmailSettings--AppPassword` | Google Workspace app password (16-char, generated per mailbox) |
| `EmailSettings--FromAddress` | Display from address (matches `UserName`) |
| `EmailSettings--ReplyToAddress` | Address user replies route to (`support@bowlneba.com`) |

#### User secrets (local dev, Neba.Api)

Not needed for email when running via Aspire — Mailpit handles it (see below).

---

#### Local dev — Mailpit via Aspire

[Mailpit](https://github.com/axllent/mailpit) is a local SMTP sink: it accepts all outbound mail without delivering it and provides a web UI to inspect what was sent. It replaces user-secrets SMTP credentials in local dev entirely.

**How it fits the existing Aspire pattern**: identical to how `postgres` and `storage` are dev-only containers wired to the API via `WithReference`. In publish mode Mailpit is not added — prod uses Google Workspace SMTP via Key Vault secrets.

##### AppHost changes

Add to `Directory.Packages.props` and `Neba.AppHost.csproj`:

| Package | Purpose |
|---------|---------|
| `CommunityToolkit.Aspire.Hosting.Mailpit` | Mailpit container resource for AppHost |

```csharp
// AppHost.cs — add before the publish-mode block, outside IsPublishMode check
if (!builder.ExecutionContext.IsPublishMode)
{
    var mailpit = builder.AddMailpit("mailpit");
    api.WithReference(mailpit).WaitFor(mailpit);
}
```

`WithReference(mailpit)` injects `ConnectionStrings__mailpit` into the API project at runtime. The Aspire dashboard automatically shows the Mailpit web UI (port 8025) as a resource link — no extra `WithUrls` needed.

##### API changes — post-configure EmailSettings from the injected connection string

`InfrastructureConfiguration` already owns all infrastructure registrations. Add this alongside the email registration:

```csharp
// InfrastructureConfiguration.cs
var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");
if (mailpitConnectionString is not null)
{
    // Connection string format: "Endpoint=smtp://localhost:{port}"
    // Override EmailSettings to point at Mailpit; no credentials required.
    var endpoint = new Uri(mailpitConnectionString.Replace("Endpoint=", string.Empty));
    builder.Services.PostConfigure<EmailSettings>(settings =>
    {
        settings.Host        = endpoint.Host;
        settings.Port        = endpoint.Port;
        settings.UserName    = string.Empty;
        settings.AppPassword = string.Empty;
    });
}
```

`PostConfigure` runs after all `Configure` calls, so it cleanly overrides whatever was set by `GetSection("EmailSettings")` without touching the `EmailSettings` options class or the sender.

##### Sender — skip auth when credentials are absent

Mailpit accepts connections without authentication. One-line guard in `GoogleWorkspaceEmailSender`:

```csharp
await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
if (!string.IsNullOrEmpty(_settings.AppPassword))
    await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, ct);
await client.SendAsync(mimeMessage, ct);
await client.DisconnectAsync(quit: true, ct);
```

##### Dev flow

1. `aspire run` starts Mailpit alongside Postgres and Storage Emulator
2. Any code path that sends email (password reset, confirm email, future tournament notifications) delivers to Mailpit instead of the real inbox
3. Open the Mailpit resource link in the Aspire dashboard to inspect the rendered email
4. No user secrets needed for email in local dev

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
| `POST` | `/security/passkeys/creation-options` | Authenticated | Phase 6 |
| `POST` | `/security/passkeys/register` | Authenticated | Phase 6 |
| `POST` | `/security/passkeys/request-options` | Anonymous | Phase 6 |
| `POST` | `/security/passkeys/login` | Anonymous | Phase 6 |

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
- Passkey management section — Phase 6

---

## Future Phases

### Phase 6: Passkeys / WebAuthn

Passkeys are built into ASP.NET Core Identity — no extra NuGet package. The implementation does **not** use the CQRS command/handler pattern; endpoints call `SignInManager`/`UserManager` methods directly and pass raw WebAuthn JSON strings.

- `Configure<IdentityPasskeyOptions>` in `SecurityConfiguration` — set `ServerDomain = "bowlneba.com"` explicitly (inferring from Host header is a credential-scoping attack vector)
- Four endpoints under `Security/Passkeys/`: `creation-options` (auth), `register` (auth), `request-options` (anon), `login` (anon)
- Each Blazor passkey page needs a `.razor.js` companion for `navigator.credentials.create()` / `navigator.credentials.get()` — see the Blazor Web App template's `PasskeySubmit.razor.js` as the reference
- Passkey info is stored in the Identity tables in the `security` schema — no new migration
- Limitations: no default attestation validation; passkeys are a primary factor only (no 2FA)
- Add `Manage/Passkeys.razor` + `Manage/RenamePasskey.razor` under `Account/Pages/Manage/` for passkey management

### Phase 7: External Login Providers

- `AddGoogle()`, `AddFacebook()`, `AddMicrosoftAccount()` on Identity builder
- `Account/Pages/ExternalLogin.razor` — handles callback, creates/links `ApplicationUser`
- `Account/Pages/Manage/ExternalLogins.razor` — manage linked providers

### Phase 8: 2FA / TOTP

- `AddTwoFactorTokenProvider<T>()` on Identity builder
- `Account/Pages/LoginWith2fa.razor`
- `Account/Pages/Manage/TwoFactorAuthentication.razor` — enable/disable, show recovery codes
- `Account/Pages/Manage/EnableAuthenticator.razor`
- `Account/Pages/Manage/GenerateRecoveryCodes.razor`

### Phase 9: Member Self-Registration + USBC ID Linking

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

- [ ] **Phase 1** — `ApplicationUser`, `ApplicationRole`, `Roles`, `SecurityDbContext`, `SecurityDbContextDesignTimeFactory`, `SecurityConfiguration` wired into `Program.cs`, initial migration, roles + admin seed; `GoogleWorkspaceEmailSender` registered (1.9)
- [ ] **Phase 2** — full CQRS stack (Command/CommandHandler/Dto or Query/QueryHandler/Dto) for `Login`, `Register`, `Refresh`, `Logout`, `Me`, `ChangePassword`
- [ ] **Contracts** — `Neba.Api.Contracts/Security/` with `ISecurityApi` + per-operation request/response types in subfolders
- [ ] **Phase 3** — `AccountConfiguration`, `Login.razor`, `Register.razor`, `Manage/Index.razor`, `BearerTokenHandler`, `Routes.razor` updated to `AuthorizeRouteView`; `ISecurityApi` registered in `ApiServicesConfiguration`
- [ ] **Phase 4** — all existing endpoints annotated with `AllowAnonymous()` or `Roles()`; `UseSecurityInfrastructure()` in `Program.cs`
- [ ] **Phase 5** — NavMenu auth links, `[Authorize(Roles = Roles.Admin)]` on admin pages
