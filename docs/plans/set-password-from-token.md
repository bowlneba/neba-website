# Set Password From Token — Plan

Shared, anonymous, token-based endpoint that lets a user set a new password (proving ownership of
the account via a token they received by email) and marks their email confirmed in the same
operation. Sub-branch 1 of `docs/plans/staff-user-creation.md` — foundational infrastructure with
no UI of its own; consumed later by the staff-invite flow (sub-branch 2/3) and the admin-reset
upgrade (sub-branch 4, issue #84).

## Decisions locked in during scoping

- **Naming**: `Security/Password/SetPasswordFromToken/` folder, `SetPasswordFromTokenCommand`,
  route `POST security/password/set-from-token` — matches the name already used in the parent plan
  document.
- **Permission**: anonymous (`AllowAnonymous`) — the token itself is the credential, same pattern as
  `RefreshTokenEndpoint`/`RegisterEndpoint`/`LoginEndpoint`. No new authorization policy needed.
- **Error behavior**: unlike the existing admin-only `password/reset` endpoint (which returns a
  distinguishable 404 for an unknown user id), this endpoint is anonymous-facing, so an unknown user
  id and an invalid/expired token both return the **same generic 422** ("invalid or expired token")
  — prevents using this endpoint to enumerate valid user ids.
- **No UI surface for this sub-branch** — confirmed in the parent plan's Delivery Plan (step 1 is
  "No UI"). The `/account/set-password` Blazor page that calls this endpoint is built in sub-branch
  3 (`create-user-ui`), once there's a real invite email pointing at it.
- Repo review found two dead-code email templates already sitting in `Security/Emails/`
  (`ResetPasswordLinkEmail.cs`, `ResetPasswordCodeEmail.cs`) with no callers anywhere — leftovers
  from the prior identity plan referenced in the parent doc. Not used by this sub-branch (no email is
  sent here — the caller of this endpoint, e.g. the invite flow, owns the email). Worth a cleanup
  pass later, but out of scope for this plan.
- `UserManager<ApplicationUser>`-based handlers in this codebase are tested as **integration** tests
  against a real `SecurityDbContextFixture` (see `RegisterCommandHandlerIntegrationTests`,
  `ResetPasswordCommandHandlerIntegrationTests`), not unit tests with a mocked `UserManager` — this
  plan follows the same pattern.

## Phase 1: API

### Contracts (`Neba.Api.Contracts`)

```csharp
// Security/SetPasswordFromToken/SetPasswordFromTokenRequest.cs
namespace Neba.Api.Contracts.Security.SetPasswordFromToken;

/// <summary>
/// Sets a new password for a user, proving ownership via a token received by email
/// (invite, admin reset, or forgot-password). Confirms the user's email in the same operation.
/// </summary>
public sealed record SetPasswordFromTokenRequest
{
    /// <summary>The id of the user the token was issued for.</summary>
    public required string UserId { get; init; }

    /// <summary>The opaque token issued by <c>UserManager.GeneratePasswordResetTokenAsync</c>.</summary>
    public required string Token { get; init; }

    /// <summary>The new password to set.</summary>
    public required string NewPassword { get; init; }
}
```

```csharp
// Security/ISecurityApi.cs — add alongside ResetPasswordAsync
/// <summary>Sets a new password using a token (invite/reset), and confirms the user's email. Anonymous.</summary>
[Post("/security/password/set-from-token")]
Task<IApiResponse> SetPasswordFromTokenAsync([Body] SetPasswordFromTokenRequest request, CancellationToken cancellationToken = default);
```

### API (`Neba.Api/Security/Password/SetPasswordFromToken/`)

```csharp
// SetPasswordFromTokenCommand.cs
using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed record SetPasswordFromTokenCommand
    : ICommand
{
    public required Ulid UserId { get; init; }

    public required string Token { get; init; }

    public required string NewPassword { get; init; }
}
```

```csharp
// SetPasswordFromTokenErrors.cs
using ErrorOr;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal static class SetPasswordFromTokenErrors
{
    public static Error InvalidOrExpiredToken =>
        Error.Validation("Security.InvalidOrExpiredToken", "This link is invalid or has expired.");
}
```

```csharp
// SetPasswordFromTokenCommandHandler.cs
using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<SetPasswordFromTokenCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(SetPasswordFromTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return SetPasswordFromTokenErrors.InvalidOrExpiredToken;
        }

        var resetResult = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

        if (!resetResult.Succeeded)
        {
            if (resetResult.Errors.Any(error => error.Code == "InvalidToken"))
            {
                return SetPasswordFromTokenErrors.InvalidOrExpiredToken;
            }

            return resetResult.Errors
                .Select(error => Error.Validation($"SetPasswordFromToken.{error.Code}", error.Description))
                .ToList();
        }

        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        return Result.Success;
    }
}
```

```csharp
// SetPasswordFromTokenRequestValidator.cs
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenRequestValidator : Validator<SetPasswordFromTokenRequest>
{
    public SetPasswordFromTokenRequestValidator()
    {
        RuleFor(r => r.UserId)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.UserIdRequired")
            .WithMessage("User ID is required.")
            .Must(id => Ulid.TryParse(id, out _))
            .WithErrorCode("SetPasswordFromTokenRequest.UserIdInvalid")
            .WithMessage("User ID must be a valid ULID.");

        RuleFor(r => r.Token)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.TokenRequired")
            .WithMessage("Token is required.");

        RuleFor(r => r.NewPassword)
            .NotEmpty()
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordRequired")
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordTooShort")
            .WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d")
            .WithErrorCode("SetPasswordFromTokenRequest.NewPasswordRequiresDigit")
            .WithMessage("Password must contain at least one digit.");
    }
}
```

```csharp
// SetPasswordFromTokenEndpoint.cs
using System.Globalization;

using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenEndpoint(Messaging.ICommandHandler<SetPasswordFromTokenCommand> commandHandler)
    : Endpoint<SetPasswordFromTokenRequest>
{
    private readonly Messaging.ICommandHandler<SetPasswordFromTokenCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("password/set-from-token");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("SetPasswordFromToken")
            .WithTags("Public")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(SetPasswordFromTokenRequest req, CancellationToken ct)
    {
        var command = new SetPasswordFromTokenCommand
        {
            UserId = Ulid.Parse(req.UserId, CultureInfo.InvariantCulture),
            Token = req.Token,
            NewPassword = req.NewPassword
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
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

```csharp
// SetPasswordFromTokenSummary.cs
using FastEndpoints;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenSummary : Summary<SetPasswordFromTokenEndpoint>
{
    public SetPasswordFromTokenSummary()
    {
        Summary = "Sets a new password using a token, and confirms the user's email.";
        Description = "Anonymous, token-based endpoint used by invite links, admin-triggered resets, and (eventually) forgot-password. Successfully consuming the token proves ownership of the email address, so EmailConfirmed is set to true in the same operation. An unknown user id and an invalid/expired token are indistinguishable in the response, to prevent user-id enumeration.";

        Response(204, "Password was set successfully and the account is now email-confirmed.");
        Response(422, "The token is invalid or expired, the user id is unrecognized, or the new password failed validation.");
    }
}
```

Route note: no `ProducesProblemDetails(401/403)` — this endpoint is anonymous by design, so those
responses never occur here (unlike `ResetPasswordEndpoint`, which requires the Admin role).

### Database

- No schema changes — operates entirely on the existing ASP.NET Identity tables
  (`ApplicationUser`/`UserManager`), same as `Register`/`ResetPassword`.

### Authorization

- `AllowAnonymous()` on the endpoint. No new policy — the token in the request body is the
  credential.

### Tests

```csharp
// tests/Neba.TestFactory/Security/SetPasswordFromTokenRequestFactory.cs
using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.TestFactory.Security;

public static class SetPasswordFromTokenRequestFactory
{
    public const string ValidToken = "valid-token";
    public const string ValidNewPassword = "NewPassword1";

    public static SetPasswordFromTokenRequest Create(
        string? userId = null,
        string? token = null,
        string? newPassword = null)
        => new()
        {
            UserId = userId ?? Ulid.NewUlid().ToString(),
            Token = token ?? ValidToken,
            NewPassword = newPassword ?? ValidNewPassword
        };
}
```

- **New** `SetPasswordFromTokenCommandHandlerIntegrationTests.cs` (Integration,
  `Component("Security")`, `SecurityDbContextFixture`) — mirrors
  `ResetPasswordCommandHandlerIntegrationTests`/`RegisterCommandHandlerIntegrationTests`. Seed a user
  via `RegisterCommandHandler`, then get a real token via
  `userManager.GeneratePasswordResetTokenAsync(user)`:
  - `HandleAsync_ShouldReturnSuccess_WhenTokenAndPasswordAreValid`
  - `HandleAsync_ShouldSetEmailConfirmedTrue_WhenTokenAndPasswordAreValid`
  - `HandleAsync_ShouldAllowLoginWithNewPassword_WhenTokenAndPasswordAreValid` (via
    `CheckPasswordAsync`)
  - `HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenUserDoesNotExist` (random `Ulid`)
  - `HandleAsync_ShouldReturnInvalidOrExpiredToken_WhenTokenIsMalformed` (garbage string, not a real
    token)
  - `HandleAsync_ShouldReturnValidationError_WhenIdentityPasswordPolicyRejectsNewPassword` (a password
    the FluentValidation rules would let through but Identity's own configured policy rejects, if the
    two diverge in this codebase's Identity options — otherwise this case can be dropped, confirm
    against `IdentityConfiguration`/`Program.cs` password options during implementation)
- **New** `SetPasswordFromTokenRequestValidatorTests.cs` (Unit) — one theory/fact per rule: missing
  `UserId`, invalid ULID `UserId`, missing `Token`, missing/short/no-digit `NewPassword`.
- **New** `SetPasswordFromTokenEndpointTests.cs` (Unit, `Factory.Create<SetPasswordFromTokenEndpoint>`)
  — success branch (204) and error branch (422), per the FastEndpoints unit-test patterns already
  documented in CLAUDE.md (`Description`/`Options`/`Get`/`Version` in `ignore-methods`, `return;`
  after error/success sends marked `// Stryker disable once Statement`).
- **New** `SetPasswordFromTokenSummaryTests.cs` (Unit) — mirrors `RegisterSummaryTests`.

### Deferred / out of scope for this sub-branch

- The invite email that links to this endpoint's eventual UI page (sub-branch 2, `create-user`).
- The `/account/set-password` Blazor page itself (sub-branch 3, `create-user-ui`).
- Repointing the existing admin `ResetPasswordCommandHandler` to use this endpoint instead of emailing
  a plaintext temp password (sub-branch 4, issue #84).
- Cleaning up the dead `ResetPasswordLinkEmail`/`ResetPasswordCodeEmail` templates noted above.

## Phase 2: UI

*(Not yet drafted — no UI surface in this sub-branch; see "Decisions locked in during scoping"
above.)*
