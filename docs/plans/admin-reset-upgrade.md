# Admin Reset Upgrade (#84)

Repoints the existing admin-triggered password reset (`ResetPasswordCommandHandler`) from emailing a
plaintext temporary password to generating a password-set token and emailing a link to the
already-built `/account/set-password` page — the same token-consumption flow built for staff invites
(PR #109/#111). This is step 4 of `docs/staff-user-creation.md`'s Delivery Plan.

## Decisions locked in during scoping

- **Backend-only change — no UI added.** No admin-facing UI calls the reset endpoint today, and none is
  added by this sub-feature (confirmed with user). Phase 2 below states explicitly there is no UI work.
- **New permission `System.ResetUserPassword` replaces `Roles(SecurityRoles.Admin)`** on
  `ResetPasswordEndpoint`. This is deliberately distinct from the (future, unbuilt) self-service
  "forgot password" flow — resetting *someone else's* password is a separate capability.
  - Follows the existing `Permission.cs` `#region System` pattern (same shape as `System.CreateUser`
    from PR #111) — standalone permission, no management-collection/`CanManage*` const needed.
  - **Granted to Admin + Webmaster.** Admin gets it automatically via `Permissions.List` (no code
    change). Webmaster needs an explicit addition to `SecurityRoleSeeder.RolePermissions`.
  - `docs/policies/README.md` needs no new row — the generic `Permission:{value}` row already covers
    any standalone single-permission policy (confirmed precedent: `System.CreateUser` added no row).
- **Reuse existing scaffolding built ahead of this work**: `SetPasswordFromTokenCommand` (PR #109,
  anonymous + token, already sets `EmailConfirmed = true` on success) and `ResetPasswordLinkEmail`
  (`src/Neba.Api/Security/Emails/ResetPasswordLinkEmail.cs`) — both currently unreferenced but built
  for exactly this repoint. The emailed link targets `/account/set-password?userId=&token=`, same page
  PR #111's invite flow already uses.
- **Removed**: `GenerateTempPassword` helper (private method on `ResetPasswordCommandHandler`, no other
  callers) and `AdminResetPasswordEmail` template + its dedicated test — both fully superseded.
- **Unchanged**: `ResetPasswordEndpoint`'s route (`POST security/password/reset`), request contract
  (`{ UserId }` in), and response shape (204/422/404) — this is an internal implementation swap only.

## Phase 1: API

New template `AdminResetPasswordLinkEmail` carries admin-reset copy (distinct from the self-service
"forgot password" phrasing already used by the unreferenced `ResetPasswordLinkEmail`), confirmed.

### Contracts

```csharp
// src/Neba.Api.Contracts/Security/Permission.cs — inside #region System, next to CreateUser

/// <summary>
/// Permission to reset another user's password.
/// </summary>
public static readonly Permissions ResetUserPassword = new("System.ResetUserPassword", "Reset User Password");
```

### New — `src/Neba.Api/Security/Emails/AdminResetPasswordLinkEmail.cs`

```csharp
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class AdminResetPasswordLinkEmail(string resetLink)
{
    public string ToHtmlBody()
    {
        var link = WebUtility.HtmlEncode(resetLink);
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">Your password has been reset</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              An administrator has reset the password for your BowlNEBA account. Click the button below to choose a new password.
            </p>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              If you weren't expecting this, contact BowlNEBA support (website@bowlneba.com) before continuing.
            </p>
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" align="center" style="margin:36px auto 28px;">
              <tr>
                <td style="background:#1a3a6e;border-radius:4px;">
                  <a href="{link}" style="display:block;color:#ffffff;text-decoration:none;padding:14px 40px;font-size:15px;font-weight:700;">
                    Set Your Password
                  </a>
                </td>
              </tr>
            </table>
            <hr style="border:none;border-top:1px solid #ececec;margin:28px 0;" />
            <p style="font-size:12px;color:#999;line-height:1.6;margin:0;word-break:break-all;overflow-wrap:break-word;">
              If the button above does not work, copy and paste this link into your browser:<br />
              <a href="{link}" style="color:#1a3a6e;word-break:break-all;overflow-wrap:break-word;">{link}</a>
            </p>
            """);
    }
}
```

### Edit — `src/Neba.Api/Security/Password/ResetPassword/ResetPasswordCommandHandler.cs`

```csharp
using System.Net;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    WebsiteSettings websiteSettings)
        : ICommandHandler<ResetPasswordCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return ResetPasswordErrors.UserNotFound;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{websiteSettings.BaseUrl}/account/set-password?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Your BowlNEBA password has been reset",
            HtmlBody = new AdminResetPasswordLinkEmail(resetLink).ToHtmlBody()
        }, cancellationToken);

        return Result.Success;
    }
}
```

`GenerateTempPassword` and its `System.Security.Cryptography` usage are removed entirely — no other
callers. The handler no longer calls `ResetPasswordAsync` itself (and so can no longer produce Identity
`IdentityResult` validation errors) — the password write now happens when the user submits the
set-password form, already handled by `SetPasswordFromTokenCommandHandler`.

### Edit — `src/Neba.Api/Security/Password/ResetPassword/ResetPasswordEndpoint.cs`

```csharp
using System.Globalization;

using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.ResetPassword;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Security.Password.ResetPassword;

internal sealed class ResetPasswordEndpoint(Messaging.ICommandHandler<ResetPasswordCommand> commandHandler)
    : Endpoint<ResetPasswordRequest>
{
    private readonly Messaging.ICommandHandler<ResetPasswordCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/reset");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ResetUserPassword.PolicyName);

        Description(description => description
            .WithName("ResetPassword")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var command = new ResetPasswordCommand { UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture) };
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

(`HandleAsync` body is unchanged — the 422 branch is now unreachable in practice since the handler can
no longer return validation errors, but it's kept since `ICommandHandler<ResetPasswordCommand>` still
returns `ErrorOr<Success>` generically and dropping the branch isn't worth the churn for this change.)

### Edit — `src/Neba.Api/Security/Password/ResetPassword/ResetPasswordSummary.cs`

```csharp
public ResetPasswordSummary()
{
    Summary = "Resets a user's password by emailing them a password-set link.";
    Description = "Generates a password-set token, emails the user a link to choose a new password, "
        + "and invalidates their current password immediately. Requires the System.ResetUserPassword permission.";

    Response(204, "Password reset initiated — a set-password link was emailed to the user.");
    Response(401, "No valid bearer token provided.");
    Response(403, "Authenticated user does not have the System.ResetUserPassword permission.");
    Response(404, "No user found with the given user ID.");
    Response(422, "Validation failed (missing or invalid user ID format).");
}
```

### Edit — `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`

```csharp
[Roles.Webmaster] =
[
    Permissions.CreateUser,
    Permissions.ResetUserPassword,

    Permissions.CreateArticle,
    Permissions.EditArticle,
    Permissions.DeleteArticle,

    Permissions.CreateSponsor,
    Permissions.EditSponsor,

    Permissions.CreateTournament,
    Permissions.EditTournament,
    Permissions.ManageTournamentSponsors,
    Permissions.DeleteTournament
],
```

(Admin needs no change — already `Permissions.List`.)

### Removed

- `src/Neba.Api/Security/Emails/AdminResetPasswordEmail.cs`
- `tests/Neba.Api.Tests/Security/Emails/AdminResetPasswordEmailTests.cs`

### Tests — `ResetPasswordCommandHandlerIntegrationTests.cs`

Replace the temp-password-specific facts with token-link equivalents:

```csharp
private const string BaseUrl = "https://bowlneba.com";

private static ResetPasswordCommandHandler CreateHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
    => new(userManager, emailSender, new WebsiteSettings { BaseUrl = BaseUrl });
```

(Matches the pattern `CreateUserCommandHandlerIntegrationTests` already uses — constructed directly, no
factory needed.)

- `HandleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist` — unchanged.
- `HandleAsync_ShouldReturnSuccess_WhenUserExists` — unchanged (update `CreateHandler` call site).
- `HandleAsync_ShouldSendEmail_ToUserEmailAddress` — unchanged (update `CreateHandler` call site).
- Replace `HandleAsync_ShouldEmbedTempPassword_InEmailBody` with
  `HandleAsync_ShouldEmbedSetPasswordLink_InEmailBody`: assert `sentMessage.HtmlBody` contains
  `"/account/set-password?userId="` and the URL-encoded token, instead of extracting/verifying a temp
  password via `CheckPasswordAsync`.
- Replace `HandleAsync_ShouldInvalidateOriginalPassword_AfterReset` with
  `HandleAsync_ShouldNotChangePassword_UntilSetPasswordTokenIsConsumed`: assert
  `CheckPasswordAsync(freshUser, RegisterRequestFactory.ValidPassword)` is still **true** immediately
  after `HandleAsync` — the whole point of the new flow is that the old password remains valid until the
  user actually completes the set-password step (unlike the old temp-password flow, which invalidated it
  immediately). This is a real behavior change worth calling out explicitly, not just a renamed test.
- Remove the now-unused `ExtractTempPasswordFromBody` helper.

### Tests — `ResetPasswordEndpointTests.cs`

Rename `Configure_ShouldRegisterAuthenticatedPostRoute_ContainingPasswordReset`, matching the exact
pattern `CreateUserEndpointTests.Configure_ShouldRegisterPermissionProtectedPostRoute_UnderSecurityPath`
already uses for its own `Policies(...)`-based endpoint (FastEndpoints unit tests can't introspect the
specific policy name off `Definition` any more than they can a role — see CLAUDE.md's "FastEndpoints
Unit Test Limitations" — so the check stays `AnonymousVerbs.ShouldBeNull()`, same as the old `Roles(...)`
version):

```csharp
[Fact(DisplayName = "Configure should register permission-protected POST route containing 'password/reset'")]
public void Configure_ShouldRegisterPermissionProtectedPostRoute_ContainingPasswordReset()
{
    // Arrange
    var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ResetPasswordCommand>>(MockBehavior.Strict);
    var endpoint = Factory.Create<ResetPasswordEndpoint>(commandHandlerMock.Object);

    // Assert
    endpoint.Definition.Verbs.ShouldContain("POST");
    endpoint.Definition.Routes.ShouldContain(r => r.Contains("password/reset"), "should include a 'password/reset' route");
    endpoint.Definition.AnonymousVerbs.ShouldBeNull();
}
```

### Tests — `ResetPasswordSummaryTests.cs`

No structural change — existing assertions (`ShouldNotBeNullOrWhiteSpace`, `ShouldContainKey` for each
status code) already pass against the new text unchanged.

### Tests — `SecurityRoleSeederTests.cs`

- Line ~52 (`SetupOtherRolesAlreadySynced`'s Webmaster branch): add `Permissions.ResetUserPassword,`
  alongside `Permissions.CreateUser,`.
- `SeedAsync_ShouldCreateWebmasterRoleAndAddExpectedClaims_WhenRoleDoesNotExist` (line 228): add
  `Permissions.ResetUserPassword` to `expectedPermissions`, and update the `DisplayName` string to
  mention "ResetUserPassword" alongside the existing permission list.

No new test factory needed — `ResetPasswordRequestFactory` is unaffected (request shape unchanged).

### Deferred / out of scope

- No `docs/policies/README.md` change — the generic `Permission:{value}` row already documents any
  standalone single-permission policy (confirmed precedent: `System.CreateUser` added no row).
- No change to `PolicyExtensions.cs`/`AddNebaPolicies()` or the Blazor-side `AccountConfiguration.cs` —
  both already handle dynamic `Permission:{value}` policies generically.
- Any admin-facing UI to trigger this endpoint — explicitly out of scope per Phase 2 below.

## Phase 2: UI

No UI work. This sub-feature is a backend-only repoint of `ResetPasswordCommandHandler`'s internals —
no admin-facing UI calls the reset endpoint today, and none is added here (confirmed with user during
scoping). The endpoint's route, request contract, and response shape are all unchanged, so no existing
UI needs updating either.
