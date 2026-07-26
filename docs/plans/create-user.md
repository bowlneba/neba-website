# Create User (Staff Invite)

Lets an Admin or Webmaster create a staff `ApplicationUser` account (webmasters, tournament directors,
journalists, etc.) without ever seeing or setting the new user's password — the invitee sets their own
password via an emailed token link. Combines what `docs/plans/staff-user-creation.md` originally scoped
as two separate sub-branches (`create-user` API, `create-user-ui` UI) into one API+UI plan, since the
foundational `set-password-from-token` endpoint (sub-branch 1) is already merged (#109).

## Decisions locked in during scoping

- **Authorization: new `Permissions.CreateUser` permission** (value `Security.CreateUser`), granted to
  `Roles.Admin` and `Roles.Webmaster` in `SecurityRoleSeeder`. This follows the app's dominant
  Permission-claim pattern (used by every other feature — articles, sponsors, tournaments) rather than
  the raw `Roles(...)` check `ResetPasswordEndpoint` uses. `Roles.Admin` can still never be *granted*
  through this endpoint's `Roles` input, regardless of who holds `Permissions.CreateUser` — that's a
  separate, existing constraint from `docs/plans/staff-user-creation.md`, unaffected by this choice.
- **Invite link base URL**: reuse `JwtSettings.Audience` (already `https://bowlneba.com` in prod,
  environment-specific elsewhere) rather than hardcoding, unlike `EmailLayout`'s hardcoded logo/footer
  links — those are genuinely static brand assets, this is an environment-sensitive route.
- **UI entry point**: added to the existing account dropdown (`Layout/AccountMenu.razor`), permission-gated
  — not a standalone hidden URL with no discoverable entry point. `/account/create-user` itself still has
  no top-level nav link and 404s (via the shared `Pages/NotFound.razor`) for non-permitted visitors who
  navigate to it directly, per the original plan's UI requirement.
- **Post-submit behavior**: `/account/create-user` stays on the same page after a successful invite and
  resets the form to blank, rather than navigating away — avoids accidental resubmission of the same
  invite and there's no natural list page to redirect to yet.
- **`/account/set-password`** is a genuinely new UI page — the API endpoint (`SetPasswordFromToken`) is
  already merged, but no UI ever consumed it.

## Confirmed UI flow

1. **`Layout/AccountMenu.razor`** — "Create User" link added inside the existing dropdown, wrapped in
   `<AuthorizeView Policy="@Permissions.CreateUser.PolicyName">`, shown above the divider/Logout link.
2. **`/account/create-user`** — form: Email (required), Roles (multi-select, all `Roles.*` except Admin),
   UsbcId (optional), Phone (optional). No Claims UI. On success: inline success message + form reset to
   blank. `<NotAuthorized>` renders `Pages/NotFound.razor`.
3. **`/account/set-password?userId=&token=`** — anonymous. New Password + Confirm Password →
   `SetPasswordFromToken`. Success redirects to `/account/login` with a confirmation message; invalid/expired
   token shows an inline error. No `DirtyFormGuard` (credential-only form exclusion).

## Phase 1: API

> Permission scaffolding (`Permissions.CreateUser`, `Roles.All`, `SecurityRoleSeeder` grant) is already
> merged on this branch — shown below as-built, not as originally drafted (the permission key landed as
> `System.CreateUser` under `#region System`, not `Security.CreateUser`).

### Contracts (`Neba.Api.Contracts`)

**New** `Security/CreateUser/CreateUserInput.cs`

```csharp
namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>The fields required to create a new staff user account.</summary>
public sealed record CreateUserInput
{
    /// <summary>The new user's email address. Used as both username and login identifier.</summary>
    public required string Email { get; init; }

    /// <summary>The role(s) to assign the new user. Must not include "Admin".</summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>Optional USBC member ID.</summary>
    public string? UsbcId { get; init; }

    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Additional claims to grant the new user. Empty when none are requested.</summary>
    public IReadOnlyCollection<ClaimInput> Claims { get; init; } = [];
}
```

**New** `Security/CreateUser/ClaimInput.cs`

```csharp
namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>A single claim type/value pair to grant a newly created user.</summary>
public sealed record ClaimInput
{
    public required string Type { get; init; }

    public required string Value { get; init; }
}
```

**New** `Security/CreateUser/CreateUserRequest.cs`

```csharp
namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>Creates a new staff user account. The invitee sets their own password via an emailed token link.</summary>
public sealed record CreateUserRequest
{
    public required CreateUserInput Input { get; init; }
}
```

**New** `Security/CreateUser/CreateUserResponse.cs`

```csharp
namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>Response returned after a successful staff user creation. Contains the new user's unique identifier.</summary>
public sealed record CreateUserResponse
{
    public required string UserId { get; init; }
}
```

**Edit** `Security/ISecurityApi.cs` — add:

```csharp
using Neba.Api.Contracts.Security.CreateUser;

/// <summary>Creates a new staff user account (Admin/Webmaster only). No password set — an invite email is sent.</summary>
[Post("/security/users")]
Task<IApiResponse<CreateUserResponse>> CreateUserAsync([Body] CreateUserRequest request, CancellationToken cancellationToken = default);
```

**`Security/Permission.cs`** — already merged, as-built:

```csharp
#region System

/// <summary>
/// Permission to create a new user in the system.
/// </summary>
public static readonly Permissions CreateUser = new("System.CreateUser", "Create User");

#endregion
```

### API (`Neba.Api/Security/CreateUser/`)

**New** `CreateUserCommand.cs`

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Security.CreateUser;

internal sealed record CreateUserCommand
    : ICommand<Ulid>
{
    public required string Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public string? UsbcId { get; init; }

    public string? PhoneNumber { get; init; }

    public IReadOnlyCollection<(string Type, string Value)> Claims { get; init; } = [];
}
```

**New** `CreateUserErrors.cs`

```csharp
using ErrorOr;

namespace Neba.Api.Security.CreateUser;

internal static class CreateUserErrors
{
    public static Error DuplicateEmail
        => Error.Conflict("CreateUser.DuplicateEmail", "An account with this email already exists.");
}
```

**New** `CreateUserCommandHandler.cs`

```csharp
using System.Net;
using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Email;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Emails;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    JwtSettings jwtSettings)
        : ICommandHandler<CreateUserCommand, Ulid>
{
    public async Task<ErrorOr<Ulid>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Ulid.NewUlid(),
            UserName = command.Email,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            UsbcId = command.UsbcId,
            EmailConfirmed = false
        };

        var createResult = await userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            var isDuplicate = createResult.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName");

            return isDuplicate
                ? CreateUserErrors.DuplicateEmail
                : createResult.Errors
                    .Select(error => Error.Validation($"CreateUser.{error.Code}", error.Description))
                    .ToList();
        }

        await userManager.AddToRolesAsync(user, command.Roles);

        if (command.Claims.Count > 0)
        {
            var claims = command.Claims.Select(c => new Claim(c.Type, c.Value));
            await userManager.AddClaimsAsync(user, claims);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var inviteLink = $"{jwtSettings.Audience}/account/set-password?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await emailSender.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "You've been invited to BowlNEBA",
            HtmlBody = new InviteUserEmail(inviteLink).ToHtmlBody()
        }, cancellationToken);

        return user.Id;
    }
}
```

**New** `CreateUserRequestValidator.cs`

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserRequestValidator : Validator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(r => r.Input.Email)
            .NotEmpty()
            .WithErrorCode("CreateUserRequest.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("CreateUserRequest.EmailInvalid")
            .WithMessage("A valid email address is required.");

        RuleFor(r => r.Input.Roles)
            .NotEmpty()
            .WithErrorCode("CreateUserRequest.RolesRequired")
            .WithMessage("At least one role is required.");

        RuleForEach(r => r.Input.Roles)
            .Must(role => role != Roles.Admin)
            .WithErrorCode("CreateUserRequest.AdminRoleNotAllowed")
            .WithMessage("The Admin role cannot be granted through this endpoint.")
            .Must(role => Roles.All.Contains(role))
            .WithErrorCode("CreateUserRequest.RoleUnknown")
            .WithMessage("One or more roles are not recognized.");
    }
}
```

**New** `CreateUserEndpoint.cs`

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserEndpoint(Messaging.ICommandHandler<CreateUserCommand, Ulid> commandHandler)
    : Endpoint<CreateUserRequest, CreateUserResponse>
{
    private readonly Messaging.ICommandHandler<CreateUserCommand, Ulid> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("users");
        Group<SecurityEndpointGroup>();

        Options(options => options
            .WithVersionSet("Security")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(Permissions.CreateUser.PolicyName);

        Description(description => description
            .WithName("CreateUser")
            .WithTags("Security")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var command = new CreateUserCommand
        {
            Email = req.Input.Email,
            Roles = req.Input.Roles,
            UsbcId = req.Input.UsbcId,
            PhoneNumber = req.Input.PhoneNumber,
            Claims = req.Input.Claims.Select(c => (c.Type, c.Value)).ToList()
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            // Stryker disable once Statement
            await Send.CreatedAtAsync("GetCurrentUser", routeValues: null, responseBody: new CreateUserResponse { UserId = result.Value.ToString() }, cancellation: ct);

            // Stryker disable once Statement
            return;
        }

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
    }
}
```

**New** `CreateUserSummary.cs`

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Security.CreateUser;

namespace Neba.Api.Security.CreateUser;

internal sealed class CreateUserSummary : Summary<CreateUserEndpoint>
{
    public CreateUserSummary()
    {
        Summary = "Creates a new staff user account.";
        Description = "Creates a staff account (webmaster, tournament director, journalist, etc.) without setting a password. The invitee receives an email with a link to set their own password.";

        Response(201, "The account was created and an invite email was sent.",
            contentType: MediaTypeNames.Application.Json,
            example: new CreateUserResponse { UserId = "01JXXXXXXXXXXXXXXXXXXXXXXXXX" });

        Response(400, "The request was malformed.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller does not hold the CreateUser permission.");
        Response(409, "An account with this email already exists.");
        Response(422, "Validation failed (invalid email, missing/unknown roles, Admin role requested).");
    }
}
```

**New** `Security/Emails/InviteUserEmail.cs`

```csharp
using System.Net;

using Neba.Api.Email;

namespace Neba.Api.Security.Emails;

internal sealed class InviteUserEmail(string inviteLink)
{
    public string ToHtmlBody()
    {
        var link = WebUtility.HtmlEncode(inviteLink);
        return EmailLayout.Wrap($"""
            <h1 style="margin:0 0 20px;font-size:22px;color:#1a3a6e;font-weight:700;">You've been invited to BowlNEBA</h1>
            <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#444;">
              An account has been created for you on BowlNEBA. Click the button below to set your password and activate your account.
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
            <p style="font-size:12px;color:#999;line-height:1.6;margin:0;">
              If the button above does not work, copy and paste this link into your browser:<br />
              <a href="{link}" style="color:#1a3a6e;">{link}</a>
            </p>
            """);
    }
}
```

### Security Infrastructure — already merged, as-built

`Security/Domain/Roles.cs`:

```csharp
internal static readonly IReadOnlyCollection<string> All =
[
    Admin,
    Webmaster,
    Manager,
    TournamentDirector,
    Journalist,
    Member
];
```

`Security/Infrastructure/SecurityRoleSeeder.cs` — `Permissions.CreateUser` added to `Roles.Webmaster`'s
claim list only (`Roles.Admin` already gets it via `Permissions.List`).

### Domain / Database

- No domain aggregate or schema changes. `ApplicationUser` already has `Email`, `PhoneNumber`, `UsbcId`;
  roles/claims are standard ASP.NET Core Identity tables already in place.

### Tests

**New factories** (`Neba.TestFactory/Security/`), matching `RegisterRequestFactory`/`RegisterResponseFactory`'s
`Create()`/`Bogus()` shape:

```csharp
// ClaimInputFactory.cs
public static class ClaimInputFactory
{
    public const string ValidType = "test-claim";
    public const string ValidValue = "test-value";

    public static ClaimInput Create(string? type = null, string? value = null)
        => new() { Type = type ?? ValidType, Value = value ?? ValidValue };

    internal static IReadOnlyCollection<ClaimInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new ClaimInput
        {
            Type = faker.Random.Word(),
            Value = faker.Random.Word(),
        })];
    }

    public static IReadOnlyCollection<ClaimInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}

// CreateUserRequestFactory.cs
public static class CreateUserRequestFactory
{
    public const string ValidEmail = "newstaff@bowlneba.com";

    public static CreateUserRequest Create(
        string? email = null,
        IReadOnlyCollection<string>? roles = null,
        string? usbcId = null,
        string? phoneNumber = null,
        IReadOnlyCollection<ClaimInput>? claims = null)
        => new()
        {
            Input = new CreateUserInput
            {
                Email = email ?? ValidEmail,
                Roles = roles ?? [Roles.Webmaster],
                UsbcId = usbcId,
                PhoneNumber = phoneNumber,
                Claims = claims ?? []
            }
        };

    internal static IReadOnlyCollection<CreateUserRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var eligibleRoles = Roles.All.Where(r => r != Roles.Admin).ToArray();
        return [.. Enumerable.Range(0, count).Select(_ => new CreateUserRequest
        {
            Input = new CreateUserInput
            {
                Email = faker.Internet.Email(),
                Roles = [faker.PickRandom(eligibleRoles)],
                UsbcId = faker.Random.Bool() ? faker.Random.AlphaNumeric(8) : null,
                PhoneNumber = faker.Random.Bool() ? faker.Phone.PhoneNumber() : null
            }
        })];
    }

    public static IReadOnlyCollection<CreateUserRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}

// CreateUserResponseFactory.cs — identical shape to RegisterResponseFactory, UserId only.
```

**`CreateUserCommandHandlerIntegrationTests`** — same shape as `ResetPasswordCommandHandlerIntegrationTests`
(real `UserManager<ApplicationUser>` from `SecurityDbContextFixture`, `Mock<IEmailSender>(MockBehavior.Strict)`,
a `JwtSettings { Audience = "https://bowlneba.com" }` instance passed directly — `UserManager<>`'s Identity
internals can't be mocked, hence integration rather than unit, matching `RegisterCommandHandlerIntegrationTests`):

```csharp
[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class CreateUserCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private static readonly JwtSettings TestJwtSettings = new() { Audience = "https://bowlneba.com" };

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateUserCommandHandler CreateHandler(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        => new(userManager, emailSender, TestJwtSettings);

    [Fact(DisplayName = "HandleAsync assigns every requested role and returns a non-empty Ulid")]
    public async Task HandleAsync_ShouldAssignRolesAndReturnUserId_WhenCreationSucceeds() { /* Arrange/Act/Assert, seed roles via SecurityRoleSeeder first */ }

    [Fact(DisplayName = "HandleAsync assigns supplied claims")]
    public async Task HandleAsync_ShouldAssignClaims_WhenClaimsProvided() { }

    [Fact(DisplayName = "HandleAsync sends an invite email with a link built from JwtSettings.Audience and the reset token")]
    public async Task HandleAsync_ShouldSendInviteEmail_WithCorrectlyBuiltLink() { }

    [Fact(DisplayName = "HandleAsync returns DuplicateEmail conflict when the email already exists")]
    public async Task HandleAsync_ShouldReturnDuplicateEmailError_WhenEmailAlreadyRegistered() { }

    [Fact(DisplayName = "HandleAsync leaves EmailConfirmed false until the invite is redeemed")]
    public async Task HandleAsync_ShouldLeaveEmailUnconfirmed_UntilInviteRedeemed() { }
}
```

**`CreateUserRequestValidatorTests`** — required `Email`/`Roles`, invalid email format, empty `Roles`
collection, `Roles.Admin` anywhere in the list → `CreateUserRequest.AdminRoleNotAllowed`, an unrecognized
role string → `CreateUserRequest.RoleUnknown`. Same `Validator<T>.TestValidateAsync` pattern as
`RegisterRequestValidatorTests`.

**`CreateUserEndpointTests`** (Configure-level, per CLAUDE.md's API Layer Mutation Testing conventions) —
route (`"users"`), policy (`Permissions.CreateUser.PolicyName`), success/conflict/validation branches via
`Factory.Create<CreateUserEndpoint>()`, success branch asserted via the `LinkGenerator`-throws pattern
(`Send.CreatedAtAsync` → `InvalidOperationException` containing `"LinkGenerator"`), same as `RegisterEndpointTests`.

**Regression test** — extend `SecurityRoleSeederTests` (already touched on this branch) to assert
`Permissions.CreateUser` is present in the claims persisted for both `Roles.Admin` and `Roles.Webmaster`
after `SeedAsync`.

### Explicitly out of scope (unchanged from the original plan)

- Any "list/get users" endpoint — `CreateUserEndpoint`'s `Send.CreatedAtAsync` pointing at
  `GetCurrentUser` is a known, precedented stand-in, not a real detail link.
- Retiring `RegisterCommandHandler`/`RegisterEndpoint` — separate sub-branch (`retire-register`) in the
  original delivery plan, not part of this one.
- Admin-reset-upgrade (#84) — separate sub-branch, not part of this one (though `ResetPasswordLinkEmail`
  already exists unused, suggesting some prior work started on it outside this plan's scope).

## Phase 2: UI

Builds the flow confirmed under "Confirmed UI flow" above: an `AccountMenu` entry point, the
`/account/create-user` form, and the new `/account/set-password` page.

### Pages

**Edit** `Layout/AccountMenu.razor` — add inside the existing `<Authorized>`, above the divider:

```razor
@using Neba.Api.Contracts.Security

<AuthorizeView Policy="@Permissions.CreateUser.PolicyName" Context="createUserContext">
    <Authorized>
        <a class="account-dropdown-link" href="/account/create-user" role="menuitem" data-enhance-nav="false">Create User</a>
    </Authorized>
</AuthorizeView>
<div class="account-divider"></div>
```

**Edit** `Account/Login/Login.razor` — add a query-driven confirmation banner (additive only, no change
to existing login logic):

```razor
@if (PasswordSet == true)
{
    <div class="mb-4">
        <NebaAlert Severity="NotifySeverity.Success" Message="Your password has been set — you can now log in." Dismissible="false" />
    </div>
}
```

```csharp
[SupplyParameterFromQuery(Name = "passwordSet")]
private bool? PasswordSet { get; set; }
```

**New** `Account/CreateUser/CreateUser.razor`:

```razor
@page "/account/create-user"
@using System.ComponentModel.DataAnnotations
@using ErrorOr
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Security.CreateUser
@using Neba.Website.Server.Services
@using Refit
@implements IAsyncDisposable
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISecurityApi SecurityApi

<PageTitle>Create User - BowlNEBA</PageTitle>

<AuthorizeView Policy="@Permissions.CreateUser.PolicyName" Context="authContext">
    <Authorized>
        <div class="neba-space-y-6">

            <div class="page-title-bar">
                <div class="page-title-inner">
                    <h1>Create User</h1>
                    <p>Invite a staff member — webmaster, tournament director, journalist, or manager. They'll set their own password from an emailed link.</p>
                </div>
            </div>

            @if (_successMessage is not null)
            {
                <NebaAlert Severity="NotifySeverity.Success" Title="Invite Sent" Message="@_successMessage" Dismissible="true"
                           OnDismiss="@(() => _successMessage = null)" />
            }

            @if (_errorMessage is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to Create User" Message="@_errorMessage" Dismissible="true"
                           OnDismiss="@(() => _errorMessage = null)" />
            }

            <DirtyFormGuard IsDirty="@_isDirty" />

            <div class="neba-card">
                <EditForm EditContext="_editContext" FormName="CreateUserForm" OnValidSubmit="HandleCreateAsync">
                    <DataAnnotationsValidator />
                    <div class="neba-space-y-6">

                        <section class="neba-space-y-4">
                            <h2 class="create-sponsor-section-title">Account</h2>

                            <div>
                                <FormLabel TargetId="email" For="@(() => _model.Email)">Email</FormLabel>
                                <InputText id="email" @bind-Value="_model.Email" class="neba-input" placeholder="newstaff@bowlneba.com" />
                                <ValidationMessage For="@(() => _model.Email)" class="block text-sm text-red-600 mt-1" />
                            </div>

                            <div>
                                <span class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">
                                    Roles <span class="form-label-required-tag">(required)</span>
                                </span>
                                <div class="create-user-role-grid">
                                    @foreach (var role in StaffRoleOptions.All)
                                    {
                                        <label class="create-user-role-option">
                                            <input type="checkbox" checked="@_model.Roles.Contains(role)" @onchange="@(e => HandleRoleToggled(role, e))" />
                                            @role
                                        </label>
                                    }
                                </div>
                                @if (_rolesError is not null)
                                {
                                    <p class="block text-sm text-red-600 mt-1">@_rolesError</p>
                                }
                            </div>
                        </section>

                        <section class="neba-space-y-4">
                            <h2 class="create-sponsor-section-title">Additional Info</h2>
                            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <div>
                                    <FormLabel TargetId="usbc-id" For="@(() => _model.UsbcId)">USBC ID</FormLabel>
                                    <InputText id="usbc-id" @bind-Value="_model.UsbcId" class="neba-input" placeholder="1234567" />
                                </div>
                                <div>
                                    <FormLabel TargetId="phone" For="@(() => _model.PhoneNumber)">Phone Number</FormLabel>
                                    <InputText id="phone" @bind-Value="_model.PhoneNumber" class="neba-input" placeholder="(555) 123-4567" />
                                </div>
                            </div>
                        </section>

                        <div class="flex items-center gap-3">
                            <button type="submit" class="neba-btn neba-btn-primary" disabled="@_isSubmitting">
                                @(_isSubmitting ? "Sending Invite…" : "Send Invite")
                            </button>
                        </div>

                    </div>
                </EditForm>
            </div>

        </div>
    </Authorized>
    <NotAuthorized>
        <div class="news-empty">
            <p class="news-empty-text">You don't have permission to create users.</p>
            <a href="/" class="neba-btn neba-btn-secondary">Back Home</a>
        </div>
    </NotAuthorized>
</AuthorizeView>

@code {
    private CreateUserFormModel _model = new();
    private EditContext _editContext;
    private bool _isDirty;
    private bool _isSubmitting;
    private string? _successMessage;
    private string? _errorMessage;
    private string? _rolesError;

    public CreateUser()
    {
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    private void MarkDirty() => _isDirty = true;

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    private void HandleRoleToggled(string role, ChangeEventArgs e)
    {
        var isChecked = e.Value is bool value && value;

        if (isChecked)
        {
            if (!_model.Roles.Contains(role))
                _model.Roles.Add(role);
        }
        else
        {
            _model.Roles.Remove(role);
        }

        MarkDirty();
    }

    private async Task HandleCreateAsync()
    {
        _rolesError = _model.Roles.Count == 0 ? "Select at least one role." : null;

        if (_rolesError is not null)
        {
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;
        _successMessage = null;

        var request = new CreateUserRequest
        {
            Input = new CreateUserInput
            {
                Email = _model.Email,
                Roles = _model.Roles,
                UsbcId = string.IsNullOrWhiteSpace(_model.UsbcId) ? null : _model.UsbcId,
                PhoneNumber = string.IsNullOrWhiteSpace(_model.PhoneNumber) ? null : _model.PhoneNumber
            }
        };

        var result = await ApiExecutor.ExecuteAsync(
            "Security",
            "CreateUser",
            ct => SecurityApi.CreateUserAsync(request, ct));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        _successMessage = "An account was created for \"" + _model.Email + "\" and an invite email is on its way.";

        _editContext.OnFieldChanged -= HandleFieldChanged;
        _model = new CreateUserFormModel();
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += HandleFieldChanged;
        _isDirty = false;
    }

    public ValueTask DisposeAsync()
    {
        _editContext.OnFieldChanged -= HandleFieldChanged;
        return ValueTask.CompletedTask;
    }

    private sealed class CreateUserFormModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; } = [];

        public string? UsbcId { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
```

**New** `Account/SetPassword/SetPassword.razor`:

```razor
@page "/account/set-password"
@using Neba.Api.Contracts.Security.SetPasswordFromToken
@using Neba.Website.Server.Services
@using Refit
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISecurityApi SecurityApi
@inject NavigationManager NavigationManager

<PageTitle>Set Password - BowlNEBA</PageTitle>

<div class="flex items-center justify-center min-h-[60vh] px-4">
    <div class="w-full max-w-sm neba-space-y-4">

        @if (_errorMessage is not null)
        {
            <NebaAlert Severity="NotifySeverity.Error" Message="@_errorMessage" Dismissible="false" />
        }

        <div class="neba-card">
            <div class="text-center mb-6">
                <img src="/images/neba-logo.png" alt="NEBA" class="h-12 mx-auto mb-3" style="display:block;margin:0 auto;" />
                <h1 class="text-xl font-bold text-[var(--neba-gray-900)] font-display">Set Your Password</h1>
                <p class="text-sm text-[var(--neba-gray-500)] mt-2">
                    You've been invited to BowlNEBA — choose a password to activate your account.
                </p>
            </div>

            <form @onsubmit="HandleSubmitAsync">
                <div class="neba-space-y-4">
                    <PasswordFields Label="New Password"
                                     Password="@_password"
                                     PasswordChanged="@(value => _password = value)"
                                     ConfirmPassword="@_confirmPassword"
                                     ConfirmPasswordChanged="@(value => _confirmPassword = value)" />

                    <button type="submit" class="neba-btn neba-btn-primary w-full justify-center" disabled="@(_isSubmitting || !CanSubmit)">
                        @(_isSubmitting ? "Setting Password…" : "Set Password")
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

@code {
    [SupplyParameterFromQuery(Name = "userId")]
    private string? UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    private string? Token { get; set; }

    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _isSubmitting;
    private string? _errorMessage;

    private bool CanSubmit => _password.Length > 0 && _password == _confirmPassword;

    private async Task HandleSubmitAsync()
    {
        if (!CanSubmit || UserId is null || Token is null)
        {
            _errorMessage = "This invite link is invalid or has expired. Ask an admin to send a new one.";
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;

        var result = await ApiExecutor.ExecuteAsync(
            "Security",
            "SetPasswordFromToken",
            ct => SecurityApi.SetPasswordFromTokenAsync(new SetPasswordFromTokenRequest
            {
                UserId = UserId,
                Token = Token,
                NewPassword = _password
            }, ct));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = "This invite link is invalid or has expired. Ask an admin to send a new one.";
            return;
        }

        NavigationManager.NavigateTo("/account/login?passwordSet=1", forceLoad: true);
    }
}
```

### Components

**New** `Account/CreateUser/StaffRoleOptions.cs`:

```csharp
namespace Neba.Website.Server.Account.CreateUser;

/// <summary>
/// The staff role options shown in the Create User form's role checkboxes. Mirrors
/// <see cref="Neba.Website.Server.Sponsors.SponsorCategoryOptions"/>'s pattern — a small, static,
/// client-side list. <c>Neba.Api.Security.Domain.Roles</c> is internal to <c>Neba.Api</c> and
/// unreachable here, so this list is intentionally duplicated rather than shared.
/// </summary>
internal static class StaffRoleOptions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Webmaster",
        "Manager",
        "Tournament Director",
        "Journalist",
        "Member"
    ];
}
```

**New** `Account/PasswordPolicy.cs`:

```csharp
namespace Neba.Website.Server.Account;

/// <summary>
/// Mirrors the Identity password policy configured in <c>SecurityConfiguration.AddSecurity()</c>
/// (<c>Neba.Api</c>, internal, unreachable here). This is a client-side hint only — the API endpoint
/// remains the actual enforcement point. Keep in sync if the server-side policy changes.
/// </summary>
internal static class PasswordPolicy
{
    public const int RequiredLength = 8;
    public const bool RequireDigit = true;
    public const bool RequireUppercase = true;
    public const bool RequireLowercase = true;
}
```

**New** `Components/PasswordFields.razor`:

```razor
@using Neba.Website.Server.Account

<div class="neba-space-y-4">
    <div>
        <label for="@_passwordId" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">@Label</label>
        <input id="@_passwordId" type="password" class="neba-input" autocomplete="new-password"
               value="@Password" @oninput="HandlePasswordInput" />

        <div class="password-strength-meter mt-2">
            <div class="password-strength-track flex gap-1">
                @for (var i = 0; i < 4; i++)
                {
                    var segmentIndex = i;
                    <div class="password-strength-seg @StrengthSegmentClass(segmentIndex)"></div>
                }
            </div>
            <p class="password-strength-label text-xs font-semibold mt-1 @StrengthLabelClass()">
                @(Password.Length == 0 ? "Password strength: —" : "Password strength: " + StrengthLabel())
            </p>
        </div>

        <ul class="password-requirements grid grid-cols-2 gap-x-3 gap-y-1 mt-2 text-xs">
            <li class="@RequirementClass(_meetsLength)"><span class="password-requirement-mark">✓</span> At least @PasswordPolicy.RequiredLength characters</li>
            <li class="@RequirementClass(_meetsUppercase)"><span class="password-requirement-mark">✓</span> One uppercase letter</li>
            <li class="@RequirementClass(_meetsLowercase)"><span class="password-requirement-mark">✓</span> One lowercase letter</li>
            <li class="@RequirementClass(_meetsDigit)"><span class="password-requirement-mark">✓</span> One number</li>
        </ul>
    </div>

    <div>
        <label for="@_confirmId" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Confirm @Label</label>
        <input id="@_confirmId" type="password" class="neba-input" autocomplete="new-password"
               value="@ConfirmPassword" @oninput="HandleConfirmInput" />
        @if (_showMismatch)
        {
            <p class="block text-sm text-red-600 mt-1">Passwords do not match.</p>
        }
    </div>
</div>

@code {
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private string _passwordId => "password-" + _instanceId;
    private string _confirmId => "confirm-password-" + _instanceId;

    private bool _meetsLength;
    private bool _meetsUppercase;
    private bool _meetsLowercase;
    private bool _meetsDigit;
    private int _strengthScore;
    private bool _showMismatch;

    [Parameter, EditorRequired]
    public string Label { get; set; } = default!;

    [Parameter]
    public string Password { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> PasswordChanged { get; set; }

    [Parameter]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ConfirmPasswordChanged { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    protected override void OnParametersSet() => Recompute();

    private async Task HandlePasswordInput(ChangeEventArgs e)
    {
        Password = e.Value?.ToString() ?? string.Empty;
        Recompute();
        await PasswordChanged.InvokeAsync(Password);
        await OnChanged.InvokeAsync();
    }

    private async Task HandleConfirmInput(ChangeEventArgs e)
    {
        ConfirmPassword = e.Value?.ToString() ?? string.Empty;
        Recompute();
        await ConfirmPasswordChanged.InvokeAsync(ConfirmPassword);
        await OnChanged.InvokeAsync();
    }

    private void Recompute()
    {
        _meetsLength = Password.Length >= PasswordPolicy.RequiredLength;
        _meetsUppercase = Password.Any(char.IsUpper);
        _meetsLowercase = Password.Any(char.IsLower);
        _meetsDigit = Password.Any(char.IsDigit);
        _strengthScore = ComputeStrengthScore(Password);
        _showMismatch = ConfirmPassword.Length > 0 && Password != ConfirmPassword;
    }

    // A separate, more continuous signal layered on the binary requirements above — rewards length
    // past the minimum and character variety, rather than just restating the checklist.
    private static int ComputeStrengthScore(string password)
    {
        if (password.Length == 0)
        {
            return 0;
        }

        var score = 0;
        if (password.Length >= PasswordPolicy.RequiredLength) score++;
        if (password.Length >= 12) score++;
        if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return Math.Min(score, 4);
    }

    private string StrengthLabel() => _strengthScore switch
    {
        <= 1 => "Weak",
        2 => "Fair",
        3 => "Good",
        _ => "Strong"
    };

    private string StrengthLabelClass() => _strengthScore <= 1 ? "text-red-600" : _strengthScore == 2 ? "text-amber-600" : "text-green-600";

    private string StrengthSegmentClass(int index)
    {
        if (Password.Length == 0 || index >= _strengthScore)
        {
            return string.Empty;
        }

        return _strengthScore <= 1 ? "password-strength-seg-weak" : _strengthScore == 2 ? "password-strength-seg-fair" : "password-strength-seg-good";
    }

    private static string RequirementClass(bool met) => met ? "password-requirement-met" : "password-requirement-pending";
}
```

**Edit** `wwwroot/neba_theme.css` — add rendering for the new role grid and `PasswordFields` states
(no existing classes cover checkbox grids or a strength meter):

```css
.create-user-role-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.6rem 0.75rem;
  background: var(--neba-gray-050);
  border: 1px solid var(--neba-gray-200);
  border-radius: var(--neba-radius);
  padding: 0.9rem 1rem;
}

.create-user-role-option {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  font-size: 0.88rem;
}

.password-strength-seg {
  flex: 1;
  height: 5px;
  border-radius: 3px;
  background: var(--neba-gray-200);
}

.password-strength-seg-weak { background: var(--neba-accent-red); }
.password-strength-seg-fair { background: var(--neba-warning); }
.password-strength-seg-good { background: var(--neba-success); }

.password-requirement-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background: var(--neba-gray-200);
  color: transparent;
  font-size: 0.6rem;
  flex-shrink: 0;
}

.password-requirement-met { color: var(--neba-gray-800); }
.password-requirement-met .password-requirement-mark {
  background: var(--neba-success);
  color: #fff;
}

.password-requirement-pending { color: var(--neba-gray-500); }
```

### API Client

- No new Refit methods needed — `ISecurityApi.CreateUserAsync` (Phase 1) and
  `ISecurityApi.SetPasswordFromTokenAsync` (already merged, `#109`) cover both pages.
- Both pages call through `ApiExecutor.ExecuteAsync(...)` (the `ErrorOr`-wrapping pattern used by
  `CreateSponsor.razor`), not the raw `SecurityApi` calls `Login.razor` makes directly — `Login.razor`
  predates `ApiExecutor` and is treated as legacy here, not a pattern to copy forward.

### State / Dirty Tracking

- `CreateUser.razor` **uses `DirtyFormGuard`** — it's a real data-entry form. `EditContext` created
  explicitly in the constructor, `OnFieldChanged` marks dirty (covers Email/UsbcId/PhoneNumber via
  `InputText`); the Roles checkbox group is a plain `<input type="checkbox">` loop (not an `InputBase`
  descendant), so its `@onchange` handler calls `MarkDirty()` explicitly, same reasoning as
  `SponsorPhoneNumbersEditor`'s `OnChanged` callback in `CreateSponsor.razor`. `_isDirty = false` is reset
  both after a successful submit (form reset, not navigation, so the guard must not immediately refire)
  and there's no navigate-away-after-save case to worry about here (per the plan's "stays on page"
  decision).
- `SetPassword.razor` **omits `DirtyFormGuard`** — credential-only form, matching the existing
  `Login.razor` exclusion in CLAUDE.md's "Dirty Form Guard" convention.

### `<PageTitle>` / Render Mode

- `CreateUser.razor`: `<PageTitle>Create User - BowlNEBA</PageTitle>`, `@rendermode InteractiveServer`
  (no async data load — `StaffRoleOptions.All` is a static in-memory list, so prerender is fine).
- `SetPassword.razor`: `<PageTitle>Set Password - BowlNEBA</PageTitle>`, `@rendermode InteractiveServer`.
  No `SignInAsync`/`SignOutAsync` call happens on this page (unlike `Login.razor`/`Logout.razor`), so the
  auth-page exception doesn't apply here — it's a normal interactive page.

### List Page "Add New" / FAB

- Not applicable. There's no list page for users (explicitly out of scope, Phase 1), so there's nothing
  for a `FabCreateButton` to sit on — the `AccountMenu` link is the only entry point, per the plan's
  decision.

### Tests

**bUnit** (`Neba.Website.Tests`), following `CreateSponsorTests`'s `BunitContext` + `Mock<ISecurityApi>`
(`Refit.Testing`) + `ApiExecutor` (with mocked `IStopwatchProvider`) setup shape:

```csharp
// Neba.Website.Tests/Account/CreateUser/CreateUserTests.cs
using AngleSharp.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using CreateUserPage = Neba.Website.Server.Account.CreateUser.CreateUser;

namespace Neba.Website.Tests.Account.CreateUser;

[UnitTest]
[Component("Website.Account.CreateUser")]
public sealed class CreateUserTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;

    public CreateUserTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();

        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetPolicies(Permissions.CreateUser.PolicyName);

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show a permission message when the user lacks CreateUser")]
    public void Render_ShouldShowPermissionMessage_WhenUserLacksCreateUserPermission()
    {
        // Arrange
        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("other-user");
        authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<CreateUserPage>();

        // Assert
        cut.Find(".news-empty-text").TextContent.ShouldContain("don't have permission to create users");
    }

    [Fact(DisplayName = "Should show a required-roles message when submitting with no roles checked")]
    public void Submit_ShouldShowRolesRequiredMessage_WhenNoRolesChecked()
    {
        // Arrange
        var cut = _ctx.Render<CreateUserPage>();
        cut.Find("#email").Change("newstaff@bowlneba.com");

        // Act
        cut.Find("button[type=submit]").Click();

        // Assert
        cut.Markup.ShouldContain("Select at least one role.");
        _mockApi.Verify(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "Should show the success alert and reset the form when creation succeeds")]
    public void Submit_ShouldShowSuccessAndResetForm_WhenCreationSucceeds()
    {
        // Arrange
        _mockApi
            .Setup(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created).ToApiResponse(CreateUserResponseFactory.Create()));

        var cut = _ctx.Render<CreateUserPage>();
        cut.Find("#email").Change("newstaff@bowlneba.com");
        cut.Find("input[type=checkbox][value=Webmaster]").Change(true);

        // Act
        cut.Find("button[type=submit]").Click();

        // Assert
        cut.Markup.ShouldContain("Invite Sent");
        cut.Find<IHtmlInputElement>("#email").Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the error alert and keep the form populated when creation fails")]
    public void Submit_ShouldShowErrorAndKeepForm_WhenCreationFails() { /* 409 duplicate-email response → error NebaAlert, _model untouched */ }

    [Fact(DisplayName = "Should mark the form dirty when Email, a role checkbox, or USBC ID/Phone changes")]
    public void FieldChange_ShouldMarkFormDirty_ForEachTrackedField() { /* assert DirtyFormGuard's IsDirty parameter after each field's change */ }
}
```

```csharp
// Neba.Website.Tests/Components/PasswordFieldsTests.cs
using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.PasswordFields")]
public sealed class PasswordFieldsTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Theory(DisplayName = "Should toggle each requirement independently as the password meets it")]
    [InlineData("short", "At least 8 characters", false)]
    [InlineData("longenough", "At least 8 characters", true)]
    [InlineData("nouppercase1", "One uppercase letter", false)]
    [InlineData("HasUppercase1", "One uppercase letter", true)]
    public void PasswordInput_ShouldToggleRequirement_BasedOnContent(string password, string requirementText, bool expectMet)
    {
        // Arrange
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "New Password"));

        // Act
        cut.Find("input[type=password]").Input(password);

        // Assert
        var item = cut.FindAll("li").First(li => li.TextContent.Contains(requirementText));
        item.ClassList.Contains("password-requirement-met").ShouldBe(expectMet);
    }

    [Fact(DisplayName = "Should render the Confirm label using the Label parameter")]
    public void Render_ShouldUseLabelParameter_ForBothFieldLabels()
    {
        // Arrange & Act
        var cut = _ctx.Render<PasswordFields>(p => p.Add(x => x.Label, "Password"));

        // Assert
        cut.Markup.ShouldContain(">Password<");
        cut.Markup.ShouldContain(">Confirm Password<");
    }

    [Fact(DisplayName = "Should not show the mismatch message until Confirm Password has content")]
    public void ConfirmInput_ShouldStayHidden_UntilConfirmHasContent() { /* type into Password only → no mismatch message rendered */ }

    [Fact(DisplayName = "Should show the mismatch message once Confirm Password differs from Password")]
    public void ConfirmInput_ShouldShowMismatch_WhenValuesDiffer() { /* type Password then a different Confirm → message renders */ }

    [Theory(DisplayName = "Should raise the strength meter tier as score-increasing characteristics are added")]
    [InlineData("abcdefgh", "Weak")]
    [InlineData("Abcdefgh1", "Fair")]
    [InlineData("Abcdefghijkl1", "Good")]
    [InlineData("Abcdefghijkl1!", "Strong")]
    public void PasswordInput_ShouldReachExpectedStrengthTier(string password, string expectedTier) { /* assert ".password-strength-label" text contains expectedTier */ }
}
```

```csharp
// Neba.Website.Tests/Account/SetPassword/SetPasswordTests.cs — same BunitContext/Mock<ISecurityApi> shape as CreateUserTests
[Fact(DisplayName = "Should not call the API when passwords do not match")]
public void Submit_ShouldNotCallApi_WhenPasswordsDoNotMatch() { /* fill mismatched values, submit, Verify CreateUserAsync-equivalent Never */ }

[Fact(DisplayName = "Should navigate to the login page with passwordSet=1 when set-password succeeds")]
public void Submit_ShouldNavigateToLoginWithPasswordSetFlag_WhenSucceeds() { /* assert NavigationManager.Uri after successful ApiResponse */ }

[Fact(DisplayName = "Should show an inline error and stay on the page when the token is invalid or expired")]
public void Submit_ShouldShowInlineError_WhenTokenInvalidOrExpired() { /* mocked 422/404 → error alert renders, NavigationManager.Uri unchanged */ }
```

```csharp
// Neba.Website.Tests/Account/AccountMenuTests.cs (extend if a file already exists, else new)
[Fact(DisplayName = "Should show the Create User link when the user holds the CreateUser policy")]
public void Render_ShouldShowCreateUserLink_WhenUserHoldsPolicy() { /* SetPolicies(Permissions.CreateUser.PolicyName) → link present */ }

[Fact(DisplayName = "Should not show the Create User link when the user lacks the CreateUser policy")]
public void Render_ShouldNotShowCreateUserLink_WhenUserLacksPolicy() { /* SetPolicies() → link absent */ }
```

```csharp
// Neba.Website.Tests/Account/Login/LoginTests.cs — extend existing file
[Fact(DisplayName = "Should show the password-set confirmation when passwordSet=1 is present")]
public void Render_ShouldShowPasswordSetConfirmation_WhenQueryParameterPresent() { /* render with ?passwordSet=1, assert NebaAlert success text */ }

[Fact(DisplayName = "Should not show the confirmation when passwordSet is absent")]
public void Render_ShouldNotShowPasswordSetConfirmation_WhenQueryParameterAbsent() { /* render with no query params, assert alert absent */ }
```

**Playwright** (`tests/e2e/`), using the existing `/__test/login?permissions=...` helper seen in
`CreateTournament.spec.ts`:

```typescript
// tests/e2e/CreateUser.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Account — create user (unauthenticated)', () => {
  test('does not show the Create User link', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('menuitem', { name: 'Create User' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the create page', async ({ page }) => {
    await page.goto('/account/create-user');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to create users");
  });
});

test.describe('Account — create user (authenticated)', () => {
  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.CreateUser');
  });

  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/users');
  });

  test('shows validation errors when submitting an empty form', async ({ page }) => {
    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-card')).toContainText('Email is required.');
    await expect(page.locator('.neba-card')).toContainText('Select at least one role.');
  });

  test('creates the user, shows the success message, and resets the form', async ({ page }) => {
    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.fill('#email', 'newstaff@bowlneba.com');
    await page.check('input[type="checkbox"][value="Webmaster"]');
    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert-success')).toContainText('Invite Sent');
    await expect(page.locator('#email')).toHaveValue('');
    await expect(page).toHaveURL(/\/account\/create-user$/);
  });
});
```

```typescript
// tests/e2e/SetPassword.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Account — set password', () => {
  test('redirects to login with the confirmation message on success', async ({ page }) => {
    await page.goto('/account/set-password?userId=test-user&token=valid-token');
    await page.fill('#new-password', 'GoodPassword1');
    await page.fill('#confirm-password', 'GoodPassword1');
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/\/account\/login\?passwordSet=1$/);
    await expect(page.locator('.neba-alert-success')).toContainText('you can now log in');
  });

  test('blocks submission client-side when passwords do not match', async ({ page }) => {
    await page.goto('/account/set-password?userId=test-user&token=valid-token');
    await page.fill('#new-password', 'GoodPassword1');
    await page.fill('#confirm-password', 'Different1');

    await expect(page.locator('button[type="submit"]')).toBeDisabled();
  });

  test('shows an inline error without navigating when the token is invalid or expired', async ({ page }) => {
    await page.goto('/account/set-password?userId=test-user&token=expired-token');
    await page.fill('#new-password', 'GoodPassword1');
    await page.fill('#confirm-password', 'GoodPassword1');
    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert-error')).toContainText('invalid or has expired');
    await expect(page).toHaveURL(/\/account\/set-password/);
  });
});
```

### Mockups

Both are data-capture forms (no real layout tradeoff to weigh), so each gets a single mockup. Both reuse
the existing app's tokens (`neba_theme.css`/`app.css` colors, Inter/Manrope, `neba-card`/`neba-btn`/
`neba-input` shapes) rather than introducing a new visual direction — this is an addition to an
established, branded app, not a greenfield page.

- `docs/plans/mockups/create-user/create-user.html` — `/account/create-user`: gradient page-title bar,
  a card with Email + a two-column role checkbox grid + USBC ID/Phone, and an inline success state that
  the mockup's script toggles on submit (simulating the "stays on page, form resets" behavior).
- `docs/plans/mockups/create-user/set-password.html` — `/account/set-password`: a centered auth-style
  card matching `Login.razor`'s layout. The dashed box marks the `PasswordFields` component's own
  boundary (New Password + live requirements checklist + strength meter, Confirm Password + live mismatch
  message) — the submit button sits outside that box on the host page, matching the "button not in the
  component" decision. Typing into either field live-updates the checklist/meter/mismatch state; buttons
  below the card preview the invalid-token error state and reset back to the default.

### Explicitly out of scope (Phase 2)

- Any UI for the `Claims` field on `CreateUserInput` — API-only for now, per Phase 1's decision.
- A "list users" admin page — no such endpoint exists (Phase 1 scope), so there's nothing to build a page
  around yet.
