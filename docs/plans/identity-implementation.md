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
│   │   ├── ApplicationUser.cs                      ← IdentityUser<Ulid> + UsbcId
│   │   ├── ApplicationRole.cs                      ← IdentityRole<Ulid>
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
│   ├── SecurityDbContext.cs                        ← new IdentityDbContext<ApplicationUser, ApplicationRole, Ulid>
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

> **Status: Complete** (SeedSecurityAsync pending — see 1.8)

### 1.1 NuGet packages ✅

Add to `Directory.Packages.props` and `Neba.Api.csproj`:

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core store |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT bearer middleware |
| `System.IdentityModel.Tokens.Jwt` | JWT creation / signing |

### 1.2 ApplicationUser ✅

```csharp
// Security/Domain/ApplicationUser.cs
namespace Neba.Api.Security.Domain;

public sealed class ApplicationUser : IdentityUser<Ulid>
{
    // Nullable: set when the user links their account to a bowler in the system.
    // Matches Bowler.UsbcId — not a FK across DbContexts; enforced at the application layer.
    public string? UsbcId { get; set; }
}
```

### 1.3 ApplicationRole ✅

```csharp
// Security/Domain/ApplicationRole.cs
namespace Neba.Api.Security.Domain;

public sealed class ApplicationRole : IdentityRole<Ulid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
```

### 1.4 Roles constants ✅

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

### 1.5 SecurityDbContext ✅

```csharp
// Database/SecurityDbContext.cs
namespace Neba.Api.Database;

internal sealed class SecurityDbContext(DbContextOptions<SecurityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Ulid>(options)
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

### 1.6 SecurityDbContextDesignTimeFactory ✅

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

### 1.7 JwtSettings ✅

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

### 1.8 SecurityConfiguration.cs ✅ (SeedSecurityAsync pending)

Registers Identity, JWT bearer auth, SecurityDbContext, and seeds roles + initial admin on startup.

`RequireConfirmedEmail` is already `true` — the email sender was wired as part of this phase, so there was no reason to defer it. JwtSettings is validated eagerly at startup (throws `InvalidOperationException` if missing or the signing key is empty) to catch misconfiguration before the app accepts traffic.

```csharp
// Security/SecurityConfiguration.cs
namespace Neba.Api.Security;

internal static class SecurityConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddSecurity()
        {
            builder.Services.AddDbContext<SecurityDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options
                    .UseNpgsql(dataSource, npgsql => npgsql
                        .MigrationsHistoryTable(
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
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<SecurityDbContext>()
                .AddDefaultTokenProviders();

            var jwtSettings = builder.Configuration
                .GetSection("JwtSettings")
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
                throw new InvalidOperationException("JwtSettings:SigningKey must not be empty.");

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

        // TODO: implement — seeds roles (Admin, ScoreKeeper, Member) and the initial admin account.
        // Call from Program.cs after app.Build(), before app.RunAsync().
        public static async Task SeedSecurityAsync(WebApplication app) { ... }
    }
}
```

### 1.9 Migration commands ✅

`Security_Init` migration applied — tables live in the `security` schema.

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

### 1.10 Email sender (MailKit + Google Workspace SMTP) ✅

**MailKit** is the de facto standard for SMTP in .NET — Microsoft deprecated `System.Net.Mail.SmtpClient` and points users to MailKit in their own docs. It occupies the same "obvious choice" position that FluentValidation holds for validation.

Email is not security-specific — tournament confirmations, notifications, and other future use cases will all share the same sending infrastructure. Following the same pattern as `Storage/`, `Clock/`, and `Caching/`, the email concern lives at the top level of `Neba.Api` as a shared infrastructure folder, not inside `Security/`.

#### Folder placement

```
Neba.Api/
├── Email/                               ← sibling to Storage/, Clock/, Caching/
│   ├── IEmailSender.cs                  ← general-purpose abstraction (application-layer equivalent)
│   ├── EmailMessage.cs                  ← value type carrying to/subject/htmlBody
│   ├── EmailSettings.cs                 ← SMTP configuration options
│   ├── EmailConfiguration.cs            ← registers IEmailSender, IEmailSender<ApplicationUser>, Mailpit overrides
│   ├── GoogleWorkspaceEmailSender.cs    ← MailKit implementation (infrastructure-layer equivalent)
│   └── IdentityEmailSenderAdapter.cs   ← adapts IEmailSender<ApplicationUser> → IEmailSender
```

In Clean Architecture terms the interface is Application, the implementation is Infrastructure. In this single-project VSA, both live in `Email/` — the folder boundary carries the same conceptual weight as the layer boundary did.

#### Google Workspace admin setup (one-time, before first deploy)

**1. `noreply@bowlneba.com` — Google Group (done)**

`noreply@bowlneba.com` is a Google Group with `tech@bowlneba.com` as its only member. Groups cannot authenticate with SMTP, so `tech@bowlneba.com` is the SMTP auth account. The `From:` header still shows `noreply@bowlneba.com` — outbound mail authenticates as `tech@bowlneba.com` but is displayed as coming from `noreply@bowlneba.com`. Receiving servers care about the authenticated sending domain (`bowlneba.com`), not the authenticated user, so DKIM/SPF/DMARC remain unaffected.

Any inbound delivery to `noreply@bowlneba.com` lands in `tech@bowlneba.com`'s inbox via group membership. Add a Gmail filter on `tech@bowlneba.com`: matches `to:noreply@bowlneba.com` → Skip Inbox + Delete. This silences the noise without losing bounces before they're inspected.

**2. Generate an app password on `tech@bowlneba.com`**

- Sign in to `tech@bowlneba.com` at `myaccount.google.com`
- Security → 2-Step Verification → enable it (if not already enabled)
- Security → App passwords → name it "BowlNeba API" → copy the 16-character password
- Store in Azure Key Vault as `EmailSettings--AppPassword`
- `EmailSettings--UserName` is `tech@bowlneba.com` (the SMTP auth account); `EmailSettings--FromAddress` is `noreply@bowlneba.com` (the display address)

#### 3. DKIM signing — already configured

DKIM is already enabled for `bowlneba.com` (confirmed working via `info@bowlneba.com`). No action needed.

#### DNS records (bowlneba.com — add at your registrar)

These three records are what receiving servers (Gmail, Outlook, etc.) check before deciding whether to deliver or drop your email. DKIM and SPF failing against a strict DMARC policy means silent drops.

| Name | Type | Value |
|------|------|-------|
| `@` | TXT | `v=spf1 include:_spf.google.com ~all` — authorizes Google's mail servers to send for your domain. Google Workspace may have already added this when you verified the domain; confirm it exists. |
| `google._domainkey` | TXT | The value generated in the DKIM step above. |
| `_dmarc` | TXT | `v=DMARC1; p=quarantine; rua=mailto:tech@bowlneba.com` — `p=quarantine` sends failing mail to spam rather than rejecting outright; `rua` sends aggregate reports so you can see what's happening. Tighten to `p=reject` once you've confirmed all legitimate senders are covered by SPF/DKIM. |

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
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
```

#### EmailMessage

`EmailMessage` uses `required` init properties rather than a positional record constructor — keeps object initializer syntax consistent with how it's used in handlers.

```csharp
// Email/EmailMessage.cs
namespace Neba.Api.Email;

public sealed record EmailMessage
{
    public required string To       { get; init; }
    public required string Subject  { get; init; }
    public required string HtmlBody { get; init; }
    // ReplyTo overrides the default from EmailSettings when set.
    public string? ReplyTo { get; init; }
}
```

#### EmailSettings

`Host`, `Port`, `UserName`, `AppPassword`, and `TlsMode` are mutable (`set` not `init`) so `PostConfigure` can override them for Mailpit without touching a separate type. `TlsMode` (`SecureSocketOptions`) is the MailKit enum — it defaults to `StartTls` for prod; Mailpit overrides it to `None`.

```csharp
// Email/EmailSettings.cs
namespace Neba.Api.Email;

internal sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host        { get; set; } = "smtp.gmail.com";
    public int    Port        { get; set; } = 587;
    public string UserName    { get; set; } = string.Empty; // SMTP auth account: tech@bowlneba.com
    public string AppPassword { get; set; } = string.Empty; // app password for tech@bowlneba.com
    public string FromAddress { get; init; } = string.Empty; // display from: noreply@bowlneba.com
    public string FromName    { get; init; } = "BowlNEBA";
    // Default reply-to for all outbound mail.
    public string ReplyToAddress { get; init; } = string.Empty; // e.g. support@bowlneba.com
    public string ReplyToName    { get; init; } = "BowlNEBA Support";
    public SecureSocketOptions TlsMode { get; set; } = SecureSocketOptions.StartTls;
}
```

#### GoogleWorkspaceEmailSender

Injects `EmailSettings` directly as a singleton (registered by `EmailConfiguration`) rather than `IOptions<EmailSettings>` — avoids the options indirection since settings are fixed after startup. Auth is skipped when `UserName` is empty (Mailpit dev path).

```csharp
// Email/GoogleWorkspaceEmailSender.cs
namespace Neba.Api.Email;

internal sealed class GoogleWorkspaceEmailSender(
    EmailSettings emailSettings,
    ILogger<GoogleWorkspaceEmailSender> logger)
    : IEmailSender
{
    private readonly EmailSettings _settings = emailSettings;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var replyToAddress = message.ReplyTo ?? _settings.ReplyToAddress;
        if (!string.IsNullOrEmpty(replyToAddress))
            mimeMessage.ReplyTo.Add(new MailboxAddress(_settings.ReplyToName, replyToAddress));

        mimeMessage.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.TlsMode, cancellationToken);
        if (!string.IsNullOrEmpty(_settings.UserName))
            await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogEmailSent(message.To, message.Subject);
    }
}

internal static partial class GoogleWorkspaceEmailSenderLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {ToAddress}: {Subject}")]
    public static partial void LogEmailSent(this ILogger<GoogleWorkspaceEmailSender> logger, string toAddress, string subject);
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
        sender.SendAsync(new EmailMessage
        {
            To       = email,
            Subject  = "Confirm your BowlNEBA Account",
            HtmlBody = $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."
        }, CancellationToken.None);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        sender.SendAsync(new EmailMessage
        {
            To       = email,
            Subject  = "Reset your BowlNEBA Password",
            HtmlBody = $"Reset your password by <a href='{resetLink}'>clicking here</a>."
        }, CancellationToken.None);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        sender.SendAsync(new EmailMessage
        {
            To       = email,
            Subject  = "Reset your BowlNEBA Password",
            HtmlBody = $"Your password reset code is: {resetCode}"
        }, CancellationToken.None);
}
```

#### Registration — EmailConfiguration.cs

Email registration lives in its own `EmailConfiguration.cs` inside `Email/`, called from `InfrastructureConfiguration.AddInfrastructure()` via `builder.AddEmail()`. `EmailSettings` is registered as both an options instance (for `PostConfigure`) and as a direct singleton (for injection into `GoogleWorkspaceEmailSender`).

```csharp
// Email/EmailConfiguration.cs
namespace Neba.Api.Email;

internal static class EmailConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public void AddEmail()
        {
            builder.Services
                .Configure<EmailSettings>(options =>
                    builder.Configuration.GetSection(EmailSettings.SectionName).Bind(options))
                .AddTransient<IEmailSender, GoogleWorkspaceEmailSender>()
                .AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();

            builder.Services.AddSingleton(sp =>
                sp.GetRequiredService<IOptions<EmailSettings>>().Value);

            var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");
            if (mailpitConnectionString is not null)
            {
                var endpoint = new Uri(mailpitConnectionString.Replace(
                    "Endpoint=", string.Empty, StringComparison.Ordinal));
                builder.Services.PostConfigure<EmailSettings>(settings =>
                {
                    settings.Host        = endpoint.Host;
                    settings.Port        = endpoint.Port;
                    settings.UserName    = string.Empty;
                    settings.AppPassword = string.Empty;
                    settings.TlsMode     = MailKit.Security.SecureSocketOptions.None;
                });
            }
        }
    }
}
```

#### Azure Key Vault secrets (additions)

| Secret | Purpose |
|--------|---------|
| `EmailSettings--UserName` | SMTP auth account: `tech@bowlneba.com` |
| `EmailSettings--AppPassword` | App password generated on `tech@bowlneba.com` (16-char) |
| `EmailSettings--FromAddress` | Display from address: `noreply@bowlneba.com` (the Google Group) |
| `EmailSettings--ReplyToAddress` | Address user replies route to (`support@bowlneba.com`) |

#### User secrets (local dev, Neba.Api)

Not needed for email when running via Aspire — Mailpit handles it (see below).

---

#### Local dev — Mailpit via Aspire

[Mailpit](https://github.com/axllent/mailpit) is a local SMTP sink: it accepts all outbound mail without delivering it and provides a web UI to inspect what was sent. It replaces user-secrets SMTP credentials in local dev entirely.

**How it fits the existing Aspire pattern**: identical to how `postgres` and `storage` are dev-only containers wired to the API via `WithReference`. In publish mode Mailpit is not added — prod uses Google Workspace SMTP via Key Vault secrets.

##### AppHost changes ✅

`CommunityToolkit.Aspire.Hosting.Mailpit` added to `Directory.Packages.props` and `Neba.AppHost.csproj`. Mailpit is wired in the `else` branch of the publish-mode check alongside postgres and storage:

```csharp
// AppHost.cs
else
{
    var mailpit = builder.AddMailPit("mailpit");

    api
        .WithReference(mailpit)
        .WaitFor(mailpit);
}
```

`WithReference(mailpit)` injects `ConnectionStrings__mailpit` into the API project at runtime. The Aspire dashboard automatically shows the Mailpit web UI (port 8025) as a resource link — no extra `WithUrls` needed.

##### API changes — post-configure EmailSettings from the injected connection string ✅

Handled inside `Email/EmailConfiguration.cs` (not `InfrastructureConfiguration`). `PostConfigure` additionally sets `TlsMode = SecureSocketOptions.None` so MailKit doesn't attempt STARTTLS against Mailpit's plain SMTP listener:

```csharp
var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");
if (mailpitConnectionString is not null)
{
    var endpoint = new Uri(mailpitConnectionString.Replace(
        "Endpoint=", string.Empty, StringComparison.Ordinal));
    builder.Services.PostConfigure<EmailSettings>(settings =>
    {
        settings.Host        = endpoint.Host;
        settings.Port        = endpoint.Port;
        settings.UserName    = string.Empty;
        settings.AppPassword = string.Empty;
        settings.TlsMode     = MailKit.Security.SecureSocketOptions.None;
    });
}
```

##### Sender — skip auth when credentials are absent ✅

Auth is skipped when `UserName` is empty (Mailpit path). `TlsMode` is driven by the `EmailSettings` property rather than a hard-coded `StartTls`:

```csharp
await client.ConnectAsync(_settings.Host, _settings.Port, _settings.TlsMode, cancellationToken);
if (!string.IsNullOrEmpty(_settings.UserName))
    await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, cancellationToken);
await client.SendAsync(mimeMessage, cancellationToken);
await client.DisconnectAsync(quit: true, cancellationToken);
```

##### Dev flow

1. `aspire run` starts Mailpit alongside Postgres and Storage Emulator
2. Any code path that sends email (password reset, confirm email, future tournament notifications) delivers to Mailpit instead of the real inbox
3. Open the Mailpit resource link in the Aspire dashboard to inspect the rendered email
4. No user secrets needed for email in local dev

---

## Phase 2: Neba.Api — Auth Endpoints

All endpoints live under `Neba.Api/Security/`, follow the same FastEndpoints conventions as `Features/`, and are versioned consistently with the rest of the API. Build one endpoint at a time in the order below — each section lists every file that needs to exist before moving to the next.

### Endpoint overview

| Verb | Route | Auth | Status |
| ---- | ----- | ---- | ------ |
| `POST` | `/security/register` | Anonymous | Day 1 |
| `POST` | `/security/login` | Anonymous | Day 1 |
| `POST` | `/security/refresh` | Anonymous + refresh token | Day 1 |
| `POST` | `/security/logout` | Authenticated | Day 1 |
| `GET`  | `/security/me` | Authenticated | Day 1 |
| `POST` | `/security/password/change` | Admin only | Day 1 |
| `POST` | `/security/password/forgot` | Anonymous | Phase 3 |
| `POST` | `/security/password/reset` | Anonymous + reset token | Phase 3 |
| `POST` | `/security/email/confirm` | Anonymous + confirm token | Phase 3 |
| `POST` | `/security/email/resend` | Anonymous | Phase 3 |

**Refresh token storage:** Raw token is 64 random bytes, base64-encoded. SHA-256 hash + `IssuedAt` are serialized as JSON and stored in `AspNetUserTokens` (the `security` schema) under provider `"RefreshToken"`, name `"RefreshToken"`. On refresh, re-hash the incoming token and do a fixed-time byte comparison. Tokens expire after 7 days (`JwtSettings.RefreshTokenExpiryDays`). Issuing new tokens also rotates the stored refresh token.

**`RefreshTokenRequest` includes `UserId`** so the handler can look up the user before fetching the stored token — avoids a full table scan.

**ChangePassword** is admin-initiated: no current password required. Uses `UserManager.RemovePasswordAsync` + `AddPasswordAsync`.

**Register auto-confirms email** (`EmailConfirmed = true`) for admin-created accounts. Phase 3 introduces email confirmation for self-registration.

---

### 2.0 Shared infrastructure (build first)

These files have no endpoint — they support everything that follows.

#### `Security/SecurityErrors.cs`

```csharp
using ErrorOr;

namespace Neba.Api.Security;

internal static class SecurityErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Security.InvalidCredentials", "The email or password is incorrect.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("Security.InvalidRefreshToken", "The refresh token is invalid or has expired.");

    public static Error UserNotFound(Ulid userId) =>
        Error.NotFound("Security.UserNotFound", $"No user with ID '{userId}' was found.");
}
```

#### `Security/TokenPair.cs`

```csharp
namespace Neba.Api.Security;

internal sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
```

#### `Security/IJwtTokenService.cs`

```csharp
using Neba.Api.Security.Domain;

namespace Neba.Api.Security;

internal interface IJwtTokenService
{
    TokenPair CreateTokenPair(ApplicationUser user, IList<string> roles);
}
```

#### `Security/JwtTokenService.cs`

Creates the JWT access token (signed, 15-min expiry) and a raw refresh token (64 random bytes). Does not touch the database — callers are responsible for storing the refresh token via `UserManager`.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using Neba.Api.Security.Domain;

namespace Neba.Api.Security;

internal sealed class JwtTokenService(JwtSettings settings) : IJwtTokenService
{
    public TokenPair CreateTokenPair(ApplicationUser user, IList<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (user.UsbcId is not null)
            claims.Add(new Claim("usbc_id", user.UsbcId));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(accessToken, refreshToken, expiresAt);
    }
}
```

#### `Security/StoredRefreshToken.cs`

Value serialized as JSON into `AspNetUserTokens.Value`.

```csharp
namespace Neba.Api.Security;

internal sealed record StoredRefreshToken(string Hash, DateTimeOffset IssuedAt);
```

#### `Security/SecurityEndpointGroup.cs`

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Security;

internal sealed class SecurityEndpointGroup : SubGroup<BaseEndpointGroup>
{
    public SecurityEndpointGroup()
    {
        VersionSets.CreateApi("Security", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("security", endpoint => endpoint
            .Description(description => description
                .WithTags("Security")
                .ProducesProblemDetails(500)));
    }
}
```

#### `Security/SecurityConfiguration.cs` — additions

After reading `jwtSettings` (already present), register it and `JwtTokenService` so handlers can inject them:

```csharp
// Add after the existing jwtSettings validation block, before AddAuthentication:
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
```

---

### 2.1 Register

**`Neba.Api.Contracts/Security/Register/RegisterRequest.cs`**

```csharp
namespace Neba.Api.Contracts.Security.Register;

/// <summary>Registers a new user account. Day 1: admin-created only; self-registration is Phase 8.</summary>
public sealed record RegisterRequest
{
    /// <summary>The new user's email address. Used as both username and login identifier.</summary>
    public required string Email { get; init; }

    /// <summary>The initial password. Must meet the API's password policy (8+ chars, at least one digit).</summary>
    public required string Password { get; init; }
}
```

**`Neba.Api.Contracts/Security/Register/RegisterResponse.cs`**

```csharp
namespace Neba.Api.Contracts.Security.Register;

/// <summary>Returned on successful registration.</summary>
public sealed record RegisterResponse
{
    /// <summary>The ID of the newly created user.</summary>
    public required string UserId { get; init; }
}
```

**`Security/Register/RegisterCommand.cs`**

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Security.Register;

internal sealed record RegisterCommand : ICommand<string>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
```

**`Security/Register/RegisterCommandHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Register;

internal sealed class RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<RegisterCommand, string>
{
    public async Task<ErrorOr<string>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            EmailConfirmed = true, // admin-created; Phase 3 adds confirmation email for self-registration
        };

        var result = await userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var isDuplicate = result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");
            if (isDuplicate)
                return Error.Conflict("Register.DuplicateEmail", "An account with this email already exists.");

            return result.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();
        }

        return user.Id.ToString();
    }
}
```

**`Security/Register/RegisterEndpoint.cs`**

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.Register;
using Neba.Api.Messaging;

namespace Neba.Api.Security.Register;

internal sealed class RegisterEndpoint(ICommandHandler<RegisterCommand, string> commandHandler)
    : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly ICommandHandler<RegisterCommand, string> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("register");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("Register")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterCommand { Email = req.Email, Password = req.Password };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
                AddError(error.Description);

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.CreatedAtAsync("GetCurrentUser", routeValues: null, responseBody: new RegisterResponse { UserId = result.Value }, cancellation: ct);
    }
}
```

**`Security/Register/RegisterRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.Register;

namespace Neba.Api.Security.Register;

internal sealed class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .WithErrorCode("RegisterRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("RegisterRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.Password)
            .NotEmpty()
            .WithErrorCode("RegisterRequest.PasswordRequired")
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithErrorCode("RegisterRequest.PasswordTooShort")
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("RegisterRequest.PasswordRequiresDigit")
            .WithMessage("Password must contain at least one digit.");
    }
}
```

**`Security/Register/RegisterSummary.cs`**

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.Register;

namespace Neba.Api.Security.Register;

internal sealed class RegisterSummary : Summary<RegisterEndpoint>
{
    public RegisterSummary()
    {
        Summary = "Registers a new user account.";
        Description = "Creates a new user account with the given email and password. Day 1: admin-created accounts only. Email is auto-confirmed — the user can log in immediately.";

        Response(201, "The account was created successfully.",
            contentType: MediaTypeNames.Application.Json,
            example: new RegisterResponse { UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX" });

        Response(409, "An account with this email already exists.");
        Response(422, "Validation failed (invalid email format, password too short, etc.).");
    }
}
```

---

### 2.2 Login

**`Neba.Api.Contracts/Security/Login/LoginRequest.cs`**

```csharp
namespace Neba.Api.Contracts.Security.Login;

/// <summary>Authenticates with email and password, returning a JWT access token and refresh token.</summary>
public sealed record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
```

**`Neba.Api.Contracts/Security/Login/LoginResponse.cs`**

```csharp
namespace Neba.Api.Contracts.Security.Login;

/// <summary>Returned on successful login or token refresh.</summary>
public sealed record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string UserId { get; init; }
    public required string Email { get; init; }
}
```

**`Security/Login/LoginDto.cs`**

```csharp
namespace Neba.Api.Security.Login;

internal sealed record LoginDto
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required Ulid UserId { get; init; }
    public required string Email { get; init; }
}
```

**`Security/Login/LoginCommand.cs`**

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Security.Login;

internal sealed record LoginCommand : ICommand<LoginDto>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
```

**`Security/Login/LoginCommandHandler.cs`**

Returns a generic "invalid credentials" error for both unknown email and wrong password — never leaks whether the email exists.

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Login;

internal sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<LoginCommand, LoginDto>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<LoginDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
            return SecurityErrors.InvalidCredentials;

        var passwordValid = await userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordValid)
            return SecurityErrors.InvalidCredentials;

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles);

        await StoreRefreshTokenAsync(user, tokenPair.RefreshToken);

        return new LoginDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!,
        };
    }

    private async Task StoreRefreshTokenAsync(ApplicationUser user, string rawToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var stored = new StoredRefreshToken(hash, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(stored);
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, json);
    }
}
```

**`Security/Login/LoginEndpoint.cs`**

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.Login;
using Neba.Api.Messaging;

namespace Neba.Api.Security.Login;

internal sealed class LoginEndpoint(ICommandHandler<LoginCommand, LoginDto> commandHandler)
    : Endpoint<LoginRequest, LoginResponse>
{
    private readonly ICommandHandler<LoginCommand, LoginDto> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("login");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("Login")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var command = new LoginCommand { Email = req.Email, Password = req.Password };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new LoginResponse
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpiresAt = dto.ExpiresAt,
            UserId = dto.UserId.ToString(),
            Email = dto.Email,
        }, ct);
    }
}
```

**`Security/Login/LoginRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.Login;

namespace Neba.Api.Security.Login;

internal sealed class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty()
            .WithErrorCode("LoginRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("LoginRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.Password)
            .NotEmpty()
            .WithErrorCode("LoginRequest.PasswordRequired")
            .WithMessage("Password is required.");
    }
}
```

**`Security/Login/LoginSummary.cs`**

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.Login;

namespace Neba.Api.Security.Login;

internal sealed class LoginSummary : Summary<LoginEndpoint>
{
    public LoginSummary()
    {
        Summary = "Authenticates a user and returns tokens.";
        Description = "Validates email and password. Returns a signed JWT access token (15 min) and an opaque refresh token (7 days). Always returns 401 for both wrong password and unknown email to avoid user enumeration.";

        Response(200, "Login successful.",
            contentType: MediaTypeNames.Application.Json,
            example: new LoginResponse
            {
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                RefreshToken = "abc123...",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
            });

        Response(401, "Invalid email or password.");
        Response(422, "Validation failed (missing email, missing password).");
    }
}
```

---

### 2.3 Refresh Token

**`Neba.Api.Contracts/Security/RefreshToken/RefreshTokenRequest.cs`**

```csharp
namespace Neba.Api.Contracts.Security.RefreshToken;

/// <summary>Exchanges a valid refresh token for a new token pair. Rotates the refresh token on each use.</summary>
public sealed record RefreshTokenRequest
{
    /// <summary>The ID of the user whose token is being refreshed. Provided in the original LoginResponse.</summary>
    public required string UserId { get; init; }

    /// <summary>The opaque refresh token received from login or a previous refresh.</summary>
    public required string RefreshToken { get; init; }
}
```

**`Neba.Api.Contracts/Security/RefreshToken/RefreshTokenResponse.cs`**

Same shape as `LoginResponse` — client should update all stored token values on refresh. Roles are in the JWT claims, not the response body.

```csharp
namespace Neba.Api.Contracts.Security.RefreshToken;

/// <summary>Returned on successful token refresh. Contains a new access token and rotated refresh token.</summary>
public sealed record RefreshTokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string UserId { get; init; }
    public required string Email { get; init; }
}
```

**`Security/RefreshToken/RefreshTokenCommand.cs`**

```csharp
using Neba.Api.Messaging;
using Neba.Api.Security.Login;

namespace Neba.Api.Security.RefreshToken;

// Reuses LoginDto — the response shape is identical to login.
internal sealed record RefreshTokenCommand : ICommand<LoginDto>
{
    public required Ulid UserId { get; init; }
    public required string RefreshToken { get; init; }
}
```

**`Security/RefreshToken/RefreshTokenCommandHandler.cs`**

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    JwtSettings jwtSettings)
    : ICommandHandler<RefreshTokenCommand, LoginDto>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<LoginDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            return SecurityErrors.InvalidRefreshToken;

        var storedJson = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (storedJson is null)
            return SecurityErrors.InvalidRefreshToken;

        StoredRefreshToken stored;
        try
        {
            stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson)
                ?? throw new InvalidOperationException("Null deserialization result.");
        }
        catch (Exception)
        {
            return SecurityErrors.InvalidRefreshToken;
        }

        var incomingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.RefreshToken)));

        // Fixed-time comparison to resist timing attacks.
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(stored.Hash),
            Convert.FromHexString(incomingHash)))
            return SecurityErrors.InvalidRefreshToken;

        if (DateTimeOffset.UtcNow > stored.IssuedAt.AddDays(jwtSettings.RefreshTokenExpiryDays))
            return SecurityErrors.InvalidRefreshToken;

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles);

        await StoreRefreshTokenAsync(user, tokenPair.RefreshToken);

        return new LoginDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!,
        };
    }

    private async Task StoreRefreshTokenAsync(ApplicationUser user, string rawToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var stored = new StoredRefreshToken(hash, DateTimeOffset.UtcNow);
        var json = JsonSerializer.Serialize(stored);
        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, json);
    }
}
```

**`Security/RefreshToken/RefreshTokenEndpoint.cs`**

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Messaging;
using Neba.Api.Security.Login;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenEndpoint(ICommandHandler<RefreshTokenCommand, LoginDto> commandHandler)
    : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly ICommandHandler<RefreshTokenCommand, LoginDto> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("refresh");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("RefreshToken")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var command = new RefreshTokenCommand { UserId = Ulid.Parse(req.UserId), RefreshToken = req.RefreshToken };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new RefreshTokenResponse
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpiresAt = dto.ExpiresAt,
            UserId = dto.UserId.ToString(),
            Email = dto.Email,
        }, ct);
    }
}
```

**`Security/RefreshToken/RefreshTokenRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequest.UserIdRequired")
            .WithMessage("User ID is required.");

        RuleFor(r => r.RefreshToken)
            .NotEmpty()
            .WithErrorCode("RefreshTokenRequest.RefreshTokenRequired")
            .WithMessage("Refresh token is required.");
    }
}
```

**`Security/RefreshToken/RefreshTokenSummary.cs`**

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenSummary : Summary<RefreshTokenEndpoint>
{
    public RefreshTokenSummary()
    {
        Summary = "Exchanges a refresh token for a new token pair.";
        Description = "Validates the refresh token (7-day window, hashed at rest) and issues a new access token + rotated refresh token. Always returns 401 for invalid or expired tokens.";

        Response(200, "Token refresh successful.",
            contentType: MediaTypeNames.Application.Json,
            example: new RefreshTokenResponse
            {
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                RefreshToken = "xyz789...",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
            });

        Response(401, "Invalid, expired, or already-rotated refresh token.");
        Response(422, "Validation failed (missing UserId or RefreshToken).");
    }
}
```

---

### 2.4 Logout

No request body — the user ID is read from the authenticated JWT claims. Deletes the stored refresh token so existing tokens can no longer be refreshed.

**`Security/Logout/LogoutCommand.cs`**

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Security.Logout;

internal sealed record LogoutCommand : ICommand
{
    public required Ulid UserId { get; init; }
}
```

**`Security/Logout/LogoutCommandHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<LogoutCommand>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<Success>> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is not null)
            await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);

        return Result.Success;
    }
}
```

**`Security/Logout/LogoutEndpoint.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Messaging;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutEndpoint(ICommandHandler<LogoutCommand> commandHandler)
    : EndpointWithoutRequest
{
    private readonly ICommandHandler<LogoutCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("logout");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization());

        Description(description => description
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userIdString is not null && Ulid.TryParse(userIdString, out var userId))
        {
            var command = new LogoutCommand { UserId = userId };
            await _commandHandler.HandleAsync(command, ct);
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

**`Security/Logout/LogoutSummary.cs`**

```csharp
using FastEndpoints;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutSummary : Summary<LogoutEndpoint>
{
    public LogoutSummary()
    {
        Summary = "Logs the current user out.";
        Description = "Revokes the stored refresh token for the authenticated user. The access token remains valid until it expires (15 min). Always returns 204, even if the user was already logged out.";

        Response(204, "Logout successful — refresh token revoked.");
        Response(401, "No valid bearer token provided.");
    }
}
```

---

### 2.5 Me

No request body — the user ID is read from JWT claims. Returns live profile data from Identity.

**`Neba.Api.Contracts/Security/Me/MeResponse.cs`**

```csharp
namespace Neba.Api.Contracts.Security.Me;

/// <summary>The current authenticated user's profile.</summary>
public sealed record MeResponse
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public string? UsbcId { get; init; }
}
```

**`Security/Me/UserDto.cs`**

```csharp
namespace Neba.Api.Security.Me;

internal sealed record UserDto
{
    public required Ulid UserId { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public string? UsbcId { get; init; }
}
```

**`Security/Me/GetCurrentUserQuery.cs`**

```csharp
using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Security.Me;

internal sealed record GetCurrentUserQuery : IQuery<ErrorOr<UserDto>>
{
    public required Ulid UserId { get; init; }
}
```

**`Security/Me/GetCurrentUserQueryHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Me;

internal sealed class GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>
{
    public async Task<ErrorOr<UserDto>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(query.UserId.ToString());
        if (user is null)
            return SecurityErrors.UserNotFound(query.UserId);

        var roles = await userManager.GetRolesAsync(user);

        return new UserDto
        {
            UserId = user.Id,
            Email = user.Email!,
            Roles = [.. roles],
            UsbcId = user.UsbcId,
        };
    }
}
```

**`Security/Me/MeEndpoint.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.Me;
using Neba.Api.Messaging;

namespace Neba.Api.Security.Me;

internal sealed class MeEndpoint(IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>> queryHandler)
    : EndpointWithoutRequest<MeResponse>
{
    private readonly IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("me");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization());

        Description(description => description
            .WithName("GetCurrentUser")
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Ulid.TryParse(userIdString, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var result = await _queryHandler.HandleAsync(new GetCurrentUserQuery { UserId = userId }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        // Stryker disable once Statement
        await Send.OkAsync(new MeResponse
        {
            UserId = dto.UserId.ToString(),
            Email = dto.Email,
            Roles = dto.Roles,
            UsbcId = dto.UsbcId,
        }, ct);
    }
}
```

**`Security/Me/MeSummary.cs`**

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.Me;

namespace Neba.Api.Security.Me;

internal sealed class MeSummary : Summary<MeEndpoint>
{
    public MeSummary()
    {
        Summary = "Returns the current user's profile.";
        Description = "Reads live data from Identity — not cached. Use after login to confirm roles or check USBC ID linkage.";

        Response(200, "Profile retrieved.",
            contentType: MediaTypeNames.Application.Json,
            example: new MeResponse
            {
                UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX",
                Email = "admin@bowlneba.com",
                Roles = ["Admin"],
                UsbcId = null,
            });

        Response(401, "No valid bearer token provided.");
        Response(404, "Authenticated user ID not found in Identity (should not occur in normal operation).");
    }
}
```

---

### 2.6 Change Password

Admin resets any user's password. No current password required. User must be looked up by ID.

**`Neba.Api.Contracts/Security/ChangePassword/ChangePasswordRequest.cs`**

```csharp
namespace Neba.Api.Contracts.Security.ChangePassword;

/// <summary>Admin-initiated password reset. No current password required.</summary>
public sealed record ChangePasswordRequest
{
    /// <summary>The ID of the user whose password is being reset.</summary>
    public required string UserId { get; init; }

    /// <summary>The new password. Must meet the API's password policy (8+ chars, at least one digit).</summary>
    public required string NewPassword { get; init; }
}
```

**`Security/Password/ChangePassword/ChangePasswordCommand.cs`**

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed record ChangePasswordCommand : ICommand
{
    public required Ulid UserId { get; init; }
    public required string NewPassword { get; init; }
}
```

**`Security/Password/ChangePassword/ChangePasswordCommandHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            return SecurityErrors.UserNotFound(command.UserId);

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return removeResult.Errors
                .Select(e => Error.Failure(e.Code, e.Description))
                .ToList();

        var addResult = await userManager.AddPasswordAsync(user, command.NewPassword);
        if (!addResult.Succeeded)
            return addResult.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();

        return Result.Success;
    }
}
```

**`Security/Password/ChangePassword/ChangePasswordEndpoint.cs`**

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ChangePassword;
using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordEndpoint(ICommandHandler<ChangePasswordCommand> commandHandler)
    : Endpoint<ChangePasswordRequest>
{
    private readonly ICommandHandler<ChangePasswordCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/change");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Roles(Domain.Roles.Admin);

        Description(description => description
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var command = new ChangePasswordCommand { UserId = Ulid.Parse(req.UserId), NewPassword = req.NewPassword };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
                AddError(error.Description);

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

**`Security/Password/ChangePassword/ChangePasswordRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordRequestValidator : Validator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("ChangePasswordRequest.UserIdRequired")
            .WithMessage("User ID is required.");

        RuleFor(r => r.NewPassword)
            .NotEmpty()
            .WithErrorCode("ChangePasswordRequest.NewPasswordRequired")
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithErrorCode("ChangePasswordRequest.NewPasswordTooShort")
            .WithMessage("New password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("ChangePasswordRequest.NewPasswordRequiresDigit")
            .WithMessage("New password must contain at least one digit.");
    }
}
```

**`Security/Password/ChangePassword/ChangePasswordSummary.cs`**

```csharp
using FastEndpoints;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordSummary : Summary<ChangePasswordEndpoint>
{
    public ChangePasswordSummary()
    {
        Summary = "Resets a user's password (admin only).";
        Description = "Admin-initiated password reset. No current password required. Uses Identity's RemovePasswordAsync + AddPasswordAsync.";

        Response(204, "Password reset successfully.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Caller does not have the Admin role.");
        Response(404, "No user with the given ID was found.");
        Response(422, "New password does not meet policy requirements.");
    }
}
```

---

### 2.7 Contracts — `ISecurityApi` and registration

**`Neba.Api.Contracts/Security/ISecurityApi.cs`**

```csharp
using Neba.Api.Contracts.Security.ChangePassword;
using Neba.Api.Contracts.Security.Login;
using Neba.Api.Contracts.Security.Me;
using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Contracts.Security.Register;

using Refit;

namespace Neba.Api.Contracts.Security;

/// <summary>Defines the Security API contract for authentication and account management.</summary>
public interface ISecurityApi
{
    /// <summary>Registers a new user account.</summary>
    [Post("/security/register")]
    Task<IApiResponse<RegisterResponse>> RegisterAsync([Body] RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Authenticates with email and password, returning a JWT and refresh token.</summary>
    [Post("/security/login")]
    Task<IApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exchanges a valid refresh token for a new token pair.</summary>
    [Post("/security/refresh")]
    Task<IApiResponse<RefreshTokenResponse>> RefreshTokenAsync([Body] RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the current user's refresh token.</summary>
    [Post("/security/logout")]
    Task<IApiResponse> LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the current authenticated user's profile.</summary>
    [Get("/security/me")]
    Task<IApiResponse<MeResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Resets any user's password (Admin only).</summary>
    [Post("/security/password/change")]
    Task<IApiResponse> ChangePasswordAsync([Body] ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
```

**`Neba.Website.Server/Services/ApiServicesConfiguration.cs` — add one line**

```csharp
// Add alongside the existing RegisterApiEndpoint<...> calls:
services.RegisterApiEndpoint<ISecurityApi>();
```

Also add the using at the top:

```csharp
using Neba.Api.Contracts.Security;
```

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

- [x] **Phase 1 (infrastructure)** — `ApplicationUser`, `ApplicationRole`, `Roles`, `SecurityDbContext`, `SecurityDbContextDesignTimeFactory`, `SecurityConfiguration` wired into `Program.cs`, initial migration applied, `GoogleWorkspaceEmailSender` + `IdentityEmailSenderAdapter` registered, Mailpit wired in AppHost
- [ ] **Phase 2 — shared infrastructure** — `SecurityErrors`, `TokenPair`, `IJwtTokenService`, `JwtTokenService`, `StoredRefreshToken`, `SecurityEndpointGroup`; add `AddSingleton(jwtSettings)` + `AddSingleton<IJwtTokenService, JwtTokenService>()` to `SecurityConfiguration.AddSecurity()`
- [ ] **Phase 2 — Register** — `RegisterRequest`, `RegisterResponse` (contracts); `RegisterCommand`, `RegisterCommandHandler`, `RegisterEndpoint`, `RegisterRequestValidator`, `RegisterSummary`
- [ ] **Phase 2 — Login** — `LoginRequest`, `LoginResponse` (contracts); `LoginDto`, `LoginCommand`, `LoginCommandHandler`, `LoginEndpoint`, `LoginRequestValidator`, `LoginSummary`
- [ ] **Phase 2 — RefreshToken** — `RefreshTokenRequest`, `RefreshTokenResponse` (contracts); `RefreshTokenCommand`, `RefreshTokenCommandHandler`, `RefreshTokenEndpoint`, `RefreshTokenRequestValidator`, `RefreshTokenSummary`
- [ ] **Phase 2 — Logout** — `LogoutCommand`, `LogoutCommandHandler`, `LogoutEndpoint`, `LogoutSummary`
- [ ] **Phase 2 — Me** — `MeResponse` (contracts); `UserDto`, `GetCurrentUserQuery`, `GetCurrentUserQueryHandler`, `MeEndpoint`, `MeSummary`
- [ ] **Phase 2 — ChangePassword** — `ChangePasswordRequest` (contracts); `ChangePasswordCommand`, `ChangePasswordCommandHandler`, `ChangePasswordEndpoint`, `ChangePasswordRequestValidator`, `ChangePasswordSummary`
- [ ] **Phase 2 — Contracts** — `ISecurityApi` in `Neba.Api.Contracts/Security/`; add `RegisterApiEndpoint<ISecurityApi>()` to `ApiServicesConfiguration`
- [ ] **Phase 3** — `AccountConfiguration`, `Login.razor`, `Register.razor`, `Manage/Index.razor`, `BearerTokenHandler`, `Routes.razor` updated to `AuthorizeRouteView`
- [ ] **Phase 4** — all existing endpoints annotated with `AllowAnonymous()` or `Roles()`; `UseSecurityInfrastructure()` already in `Program.cs` ✓
- [ ] **Phase 5** — NavMenu auth links, `[Authorize(Roles = Roles.Admin)]` on admin pages
