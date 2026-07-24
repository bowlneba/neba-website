# Tournament Sponsors

Lets staff attach and remove sponsors on an existing tournament — picking an active sponsor, setting a sponsorship amount, and optionally marking it the title sponsor — completing the create-tournament epic (`docs/help/create-tournament.md`).

## Decisions locked in during scoping

- **Placement**: not a step in the Create Tournament form. Sponsors are managed after the tournament exists, via a new admin-only **Manage Sponsors** panel on the Tournament Detail page (`/tournaments/{id}`). The public-facing "Sponsors" section already on that page is unaffected — it just starts reflecting what's managed here.
- **Scope**: both **add** and **remove**. `Tournament.AddSponsor(...)` already exists in the domain; a new `Tournament.RemoveSponsor(SponsorId)` method is needed.
- **Authorization**: new dedicated permission `Tournaments.ManageSponsors`, separate from `Tournaments.CreateTournament`.
- **Mockup confirmed**: `docs/plans/mockups/tournament-sponsors/manage-sponsors.html` — sponsor rows show real logo images (fallback to name text when no logo, matching the existing public sponsor cards on the same page), a Title Sponsor badge, sponsorship amount, and a Remove action; adding uses a modal with a sponsor picker (active sponsors not already attached), amount input, and a title-sponsor checkbox; removing is confirmed via a second modal.
- **Interaction model**: each Add or Remove is its own immediate API call, fired when the user confirms that one action (clicking "Add Sponsor" in the modal, or "Remove Sponsor" in the confirm dialog) — not batched behind a page-level Save button. This is a standalone admin action panel on the read-only Tournament Detail page, not a data-entry form the page navigates away from, so there's no natural "save the whole page" moment and no `DirtyFormGuard` needed for this panel. Each call updates the panel's local list on success (optimistic-after-confirm, not optimistic-before-response) rather than reloading the whole page.

## Phase 1: API

**Response codes refined from the functional draft**: both endpoints return `204 No Content` on success (like `EditSponsorEndpoint`/`DeleteArticleEndpoint`), not `201`/`200` with a body. There's no dedicated "get one tournament sponsor" resource to `CreatedAtAsync` link to, and the client already holds everything it needs to update its own list locally — the picked sponsor's `Name`/`Slug`/`LogoUrl` come from the already-loaded `ListActiveSponsors` dropdown data, and the submitted `TitleSponsor`/`SponsorshipAmount` are known client-side before the call. So no new response contract types are needed for either endpoint.

**Role seeding**: no `SecurityRoleSeeder.cs` change needed. `Roles.Admin` is granted `Permissions.List` (every permission, automatically), so the new permission reaches Admin with zero extra wiring. `Roles.Webmaster` doesn't currently hold `Tournaments.CreateTournament` either — sponsor management follows the same as-is scoping.

### Domain

```csharp
// src/Neba.Api/Features/Tournaments/Domain/Tournament.cs — add after AddSponsor(...)

/// <summary>
/// Removes a sponsor; returns an error if the sponsor isn't currently attached.
/// </summary>
public ErrorOr<Deleted> RemoveSponsor(SponsorId sponsorId)
{
    var sponsor = _sponsors.SingleOrDefault(tournamentSponsor => tournamentSponsor.SponsorId == sponsorId);

    if (sponsor is null)
    {
        return TournamentErrors.SponsorNotAttached(sponsorId);
    }

    _sponsors.Remove(sponsor);

    return Result.Deleted;
}
```

```csharp
// src/Neba.Api/Features/Tournaments/Domain/TournamentErrors.cs — add alongside SponsorAlreadyAdded/TitleSponsorAlreadyAdded

public static Error SponsorNotFound(SponsorId sponsorId)
    => Error.Validation(
        code: "Tournament.SponsorNotFound",
        description: "The specified sponsor was not found.",
        metadata: new Dictionary<string, object>
        {
            { "SponsorId", sponsorId.ToString() }
        });

public static Error SponsorNotAttached(SponsorId sponsorId)
    => Error.Conflict(
        code: "Tournament.SponsorNotAttached",
        description: "The specified sponsor is not attached to this tournament.",
        metadata: new Dictionary<string, object>
        {
            { "SponsorId", sponsorId.ToString() }
        });
```

`TournamentSponsor.cs` and `TournamentSponsorErrors.cs` need no changes — `Create` and the negative-amount check already cover the add path.

### Application — `AddTournamentSponsor/`

```csharp
// AddTournamentSponsorCommand.cs
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed record AddTournamentSponsorCommand
    : ICommand
{
    public required TournamentId TournamentId { get; init; }

    public required SponsorId SponsorId { get; init; }

    public required bool TitleSponsor { get; init; }

    public required decimal SponsorshipAmount { get; init; }
}
```

```csharp
// AddTournamentSponsorCommandHandler.cs
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<AddTournamentSponsorCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(AddTournamentSponsorCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        if (!await appDbContext.Sponsors.AnyAsync(s => s.Id == command.SponsorId, cancellationToken))
        {
            return TournamentErrors.SponsorNotFound(command.SponsorId);
        }

        var result = tournament.AddSponsor(command.SponsorId, command.TitleSponsor, command.SponsorshipAmount);

        if (result.IsError)
        {
            return result.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Success;
    }
}
```

```csharp
// AddTournamentSponsorEndpoint.cs
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorEndpoint(Messaging.ICommandHandler<AddTournamentSponsorCommand, Success> commandHandler)
    : Endpoint<AddTournamentSponsorRequest>
{
    private readonly Messaging.ICommandHandler<AddTournamentSponsorCommand, Success> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post("{id}/sponsors");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentSponsors.PolicyName);

        Description(description => description
            .WithName("AddTournamentSponsor")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(AddTournamentSponsorRequest req, CancellationToken ct)
    {
        var command = new AddTournamentSponsorCommand
        {
            TournamentId = new TournamentId(req.Id),
            SponsorId = new SponsorId(req.Sponsor.SponsorId),
            TitleSponsor = req.Sponsor.TitleSponsor,
            SponsorshipAmount = req.Sponsor.SponsorshipAmount
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

```csharp
// AddTournamentSponsorRequestValidator.cs
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorRequestValidator
    : Validator<AddTournamentSponsorRequest>
{
    public AddTournamentSponsorRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("AddTournamentSponsorRequest.IdRequired").WithMessage("Tournament ID is required.")
            .Length(26).WithErrorCode("AddTournamentSponsorRequest.IdInvalidLength").WithMessage("Tournament ID must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.SponsorId)
            .NotEmpty().WithErrorCode("AddTournamentSponsorRequest.SponsorIdRequired").WithMessage("Sponsor ID is required.")
            .Length(26).WithErrorCode("AddTournamentSponsorRequest.SponsorIdInvalidLength").WithMessage("Sponsor ID must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.SponsorshipAmount)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("AddTournamentSponsorRequest.SponsorshipAmountInvalid")
            .WithMessage("Sponsorship amount must be zero or greater.");
    }
}
```

```csharp
// AddTournamentSponsorSummary.cs
using FastEndpoints;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorSummary : Summary<AddTournamentSponsorEndpoint>
{
    public AddTournamentSponsorSummary()
    {
        Summary = "Adds a sponsor to a tournament.";
        Description = "Attaches an existing sponsor to a tournament with a sponsorship amount, optionally marking it the title sponsor. Requires the Tournaments.ManageSponsors permission.";

        Response(204, "Sponsor added.");
        Response(400, "Sponsor ID or sponsorship amount failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageSponsors permission.");
        Response(404, "Tournament was not found.");
        Response(409, "The sponsor is already attached to this tournament, or a title sponsor is already set.");
        Response(422, "The specified sponsor was not found.");
    }
}
```

### Application — `RemoveTournamentSponsor/`

```csharp
// RemoveTournamentSponsorCommand.cs
using ErrorOr;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed record RemoveTournamentSponsorCommand
    : ICommand<Deleted>
{
    public required TournamentId TournamentId { get; init; }

    public required SponsorId SponsorId { get; init; }
}
```

```csharp
// RemoveTournamentSponsorCommandHandler.cs
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<RemoveTournamentSponsorCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RemoveTournamentSponsorCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var result = tournament.RemoveSponsor(command.SponsorId);

        if (result.IsError)
        {
            return result.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Deleted;
    }
}
```

```csharp
// RemoveTournamentSponsorRequest.cs — server-side binding only, no Refit body needed (see ITournamentsApi below)
using FastEndpoints;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorRequest
{
    [BindFrom("id")]
    public required string TournamentId { get; set; }

    public required string SponsorId { get; set; }
}
```

```csharp
// RemoveTournamentSponsorEndpoint.cs
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorEndpoint(Messaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted> commandHandler)
    : Endpoint<RemoveTournamentSponsorRequest>
{
    private readonly Messaging.ICommandHandler<RemoveTournamentSponsorCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}/sponsors/{sponsorId}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentSponsors.PolicyName);

        Description(description => description
            .WithName("RemoveTournamentSponsor")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(RemoveTournamentSponsorRequest req, CancellationToken ct)
    {
        var command = new RemoveTournamentSponsorCommand
        {
            TournamentId = new TournamentId(req.TournamentId),
            SponsorId = new SponsorId(req.SponsorId)
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

```csharp
// RemoveTournamentSponsorSummary.cs
using FastEndpoints;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed class RemoveTournamentSponsorSummary : Summary<RemoveTournamentSponsorEndpoint>
{
    public RemoveTournamentSponsorSummary()
    {
        Summary = "Removes a sponsor from a tournament.";
        Description = "Detaches a sponsor from a tournament. Does not affect the sponsor's own profile. Requires the Tournaments.ManageSponsors permission.";

        Response(204, "Sponsor removed.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.ManageSponsors permission.");
        Response(404, "Tournament was not found.");
        Response(409, "The specified sponsor is not attached to this tournament.");
    }
}
```

### Shared — `Features/Tournaments/TournamentMutationResultSender.cs`

New file (mirrors `Features/Sponsors/SponsorMutationResultSender.cs`), shared by both new endpoints:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Maps a failed tournament-sponsor command result onto the 409/422 HTTP responses shared by
/// <see cref="AddTournamentSponsor.AddTournamentSponsorEndpoint"/> and
/// <see cref="RemoveTournamentSponsor.RemoveTournamentSponsorEndpoint"/>. The 404-not-found branch is
/// handled separately by each endpoint. Takes the endpoint's own <c>AddError</c>/<c>Send.ErrorsAsync</c>
/// as delegates since FastEndpoints' response sender is only reachable from within an endpoint instance.
/// </summary>
internal static class TournamentMutationResultSender
{
    public static async Task SendConflictOrValidationErrorsAsync(
        Error firstError,
        IReadOnlyCollection<Error> errors,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken ct)
    {
        if (firstError.Type == ErrorType.Conflict)
        {
            addError(firstError.Description);
            await sendErrorsAsync(StatusCodes.Status409Conflict, ct);
            return;
        }

        foreach (var error in errors)
        {
            addError(error.Description);
        }

        await sendErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
    }
}
```

### Routes

- `POST /tournaments/{id}/sponsors` — add.
- `DELETE /tournaments/{id}/sponsors/{sponsorId}` — remove.

**No new `GET /tournaments/{id}/sponsors` route.** `GetTournamentEndpoint` (`GET /tournaments/{id}`) already returns the full sponsor list, and the picker dropdown reuses the existing `GET /sponsors` (`ListActiveSponsors`).

### Infrastructure / Caching

No new EF configuration — `TournamentSponsorConfiguration`/`tournament_sponsors` already support this shape. Cache eviction (both handlers, shown above) removes two tags via `CacheDescriptors`' existing key format: `neba:tournaments:{tournamentId}` (matches `CacheDescriptors.Tournaments.TournamentDetail`'s most-specific tag — evicts the cached `GetTournamentQuery` entry) and `neba:tournaments:{seasonId}` (matches `CacheDescriptors.Tournaments.ListForSeason`'s tag — evicts the season schedule list, whose `ListTournamentsInSeason` projection also carries sponsor data). Same two-tag eviction `CreateTournamentCommandHandler` already does for the season tag.

### Authorization

```csharp
// src/Neba.Api.Contracts/Security/Permission.cs — inside #region Tournaments, after CreateTournament

/// <summary>
/// Permission to add or remove sponsors on a tournament.
/// </summary>
public static readonly Permissions ManageTournamentSponsors = new("Tournaments.ManageSponsors", "Manage Tournament Sponsors");
```

No `SecurityRoleSeeder.cs` change (see note above the Domain section) and no new policy registration — `Permission:{value}` policies resolve generically. `docs/policies/README.md` gets a new row for `Permission:Tournaments.ManageSponsors`, added via the `policy-documentation` skill after implementation.

### Contracts (`Neba.Api.Contracts`)

```csharp
// Tournaments/AddTournamentSponsor/AddTournamentSponsorRequest.cs
namespace Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

/// <summary>
/// Adds a sponsor to a tournament.
/// </summary>
public sealed record AddTournamentSponsorRequest
{
    /// <summary>
    /// The ULID string identifying the tournament to add the sponsor to.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The sponsorship fields to add.
    /// </summary>
    public required AddTournamentSponsorInput Sponsor { get; init; }
}
```

```csharp
// Tournaments/AddTournamentSponsor/AddTournamentSponsorInput.cs
namespace Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

/// <summary>
/// The fields required to attach a sponsor to a tournament.
/// </summary>
public sealed record AddTournamentSponsorInput
{
    /// <summary>
    /// The ULID string identifying the sponsor to attach.
    /// </summary>
    public required string SponsorId { get; init; }

    /// <summary>
    /// Whether this sponsor is the tournament's title sponsor. Only one sponsor per tournament may hold this designation.
    /// </summary>
    public required bool TitleSponsor { get; init; }

    /// <summary>
    /// The sponsorship amount, in dollars.
    /// </summary>
    public required decimal SponsorshipAmount { get; init; }
}
```

```csharp
// Tournaments/GetTournament/TournamentDetailSponsorResponse.cs — additive fields
public required bool TitleSponsor { get; init; }

public required decimal SponsorshipAmount { get; init; }
```

`Features/Tournaments/GetTournament/TournamentDetailSponsorDto.cs` gets the same two additive fields. `GetTournamentQueryHandler`'s sponsor projection changes from selecting off `tournamentSponsor.Sponsor` alone to selecting off `tournamentSponsor` itself so both the nested `Sponsor` fields and `TournamentSponsor`'s own `TitleSponsor`/`SponsorshipAmount` are captured in one pass:

```csharp
// GetTournamentQueryHandler.cs — replace the existing Sponsors projection
Sponsors = tournament.Sponsors
    .Select(tournamentSponsor => new
    {
        tournamentSponsor.Sponsor.Name,
        tournamentSponsor.Sponsor.Slug,
        LogoContainer = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Container : null,
        LogoPath = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Path : null,
        tournamentSponsor.Sponsor.WebsiteUrl,
        tournamentSponsor.Sponsor.TagPhrase,
        tournamentSponsor.TitleSponsor,
        tournamentSponsor.SponsorshipAmount
    }).ToList(),
```

...and the later mapping to `TournamentDetailSponsorDto` (and `GetTournamentEndpoint`'s mapping to `TournamentDetailSponsorResponse`) each add `TitleSponsor = s.TitleSponsor` / `SponsorshipAmount = s.SponsorshipAmount`. This response type is served through `CachedQueryHandlerDecorator`, whose deserialization-fallback path already handles the shape change for any stale cached entries (per CLAUDE.md's "FusionCache Deserialization Recovery" learning) — no manual cache-clear needed for existing entries.

`ISponsorsApi`/`ListActiveSponsors` are reused as-is for the picker (no changes).

```csharp
// Tournaments/ITournamentsApi.cs — new methods
using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;

/// <summary>
/// Adds a sponsor to a tournament.
/// </summary>
[Post("/tournaments/{id}/sponsors")]
Task<IApiResponse> AddTournamentSponsorAsync(string id, AddTournamentSponsorRequest request, CancellationToken cancellationToken = default);

/// <summary>
/// Removes a sponsor from a tournament.
/// </summary>
[Delete("/tournaments/{id}/sponsors/{sponsorId}")]
Task<IApiResponse> RemoveTournamentSponsorAsync(string id, string sponsorId, CancellationToken cancellationToken = default);
```

### Tests

- `TournamentTests`:
  ```csharp
  [Fact(DisplayName = "RemoveSponsor should remove the sponsor when attached")]
  [UnitTest, Component("Tournaments")]
  public void RemoveSponsor_ShouldRemoveSponsor_WhenAttached()
  {
      // Arrange
      var tournament = TournamentFactory.Create();
      var sponsorId = SponsorId.New();
      tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 100m);

      // Act
      var result = tournament.RemoveSponsor(sponsorId);

      // Assert
      result.IsError.ShouldBeFalse();
      tournament.Sponsors.ShouldNotContain(s => s.SponsorId == sponsorId);
  }

  [Fact(DisplayName = "RemoveSponsor should return a conflict error when the sponsor is not attached")]
  [UnitTest, Component("Tournaments")]
  public void RemoveSponsor_ShouldReturnError_WhenNotAttached()
  {
      // Arrange
      var tournament = TournamentFactory.Create();

      // Act
      var result = tournament.RemoveSponsor(SponsorId.New());

      // Assert
      result.IsError.ShouldBeTrue();
      result.FirstError.Type.ShouldBe(ErrorType.Conflict);
  }
  ```
- `AddTournamentSponsorCommandHandlerTests` — success path, tournament-not-found (`ErrorType.NotFound`), sponsor-not-found (`ErrorType.Validation`), duplicate-add conflict, title-sponsor conflict (both domain errors bubbling through unchanged), cache tags removed on success (`MockBehavior.Strict` on `IFusionCache.RemoveByTagAsync` for both tags).
- `RemoveTournamentSponsorCommandHandlerTests` — success path, tournament-not-found, sponsor-not-attached conflict, cache tags removed on success.
- Endpoint `Configure`/`HandleAsync` tests for both, using `Factory.Create<TEndpoint>()` and the FastEndpoints Stryker `ignore-methods` already documented in CLAUDE.md (`Description`, `Options`, `Get`/route registration, `Version`).
- `GetTournamentQueryHandlerTests` — existing sponsor-mapping test(s) updated to assert the two new fields (`TitleSponsor`, `SponsorshipAmount`) round-trip from `TournamentSponsor` through to `TournamentDetailDto`.
- No new test factory needed — `TournamentFactory` and `SponsorFactory` already exist; `TournamentSponsor` stays `internal`-constructed only through `Tournament.AddSponsor`, consistent with the always-valid-entity pattern.

## Phase 2: UI

### Mockups

Already produced and confirmed during scoping (see "Decisions locked in during scoping" above) rather than at the usual Step 7 point in this plan — the user asked to see the panel before locking in placement:

- `docs/plans/mockups/tournament-sponsors/manage-sponsors.html` — the admin-only Manage Sponsors panel on Tournament Detail: sponsor rows (logo, name, Title Sponsor badge, amount, Remove), an Add Sponsor modal (sponsor picker, amount input, title-sponsor checkbox), and a Remove confirmation modal. Confirmed placeholder logos (`KEGEL`/`STORM` boxes) are mockup-only stand-ins for real `<img>` sponsor logos.

### View Models / Mapping

```csharp
// Tournaments/Detail/TournamentDetailSponsorViewModel.cs — additive fields
public required bool TitleSponsor { get; init; }

public required decimal SponsorshipAmount { get; init; }
```

```csharp
// Tournaments/Detail/TournamentDetailMappingExtensions.cs — ToViewModel() gains two lines
extension(TournamentDetailSponsorResponse response)
{
    public TournamentDetailSponsorViewModel ToViewModel() => new()
    {
        Name = response.Name,
        Slug = response.Slug,
        LogoUrl = response.LogoUrl,
        WebsiteUrl = response.WebsiteUrl,
        TagPhrase = response.TagPhrase,
        TitleSponsor = response.TitleSponsor,
        SponsorshipAmount = response.SponsorshipAmount,
    };
}
```

`Sponsors/SponsorSummaryViewModel.cs` and its existing `SponsorMappingExtensions.SponsorSummaryResponse.ToViewModel()` (in `Neba.Website.Server.Sponsors`) are reused as-is for the Add modal's picker — no changes.

### Components (new, under `Tournaments/Detail/`)

```razor
{{-- Tournaments/Detail/ManageTournamentSponsors.razor --}}
@using ErrorOr
@using Neba.Api.Contracts.Sponsors
@using Neba.Api.Contracts.Tournaments
@using Neba.Website.Server.Components
@using Neba.Website.Server.Notifications
@using Neba.Website.Server.Services
@using Neba.Website.Server.Sponsors

@inject ITournamentsApi TournamentsApi
@inject ISponsorsApi SponsorsApi
@inject ApiExecutor ApiExecutor
@inject ToastService ToastService

<div class="neba-card mts-panel">
    <div class="mts-panel__header">
        <h2 class="td-section-title">Manage Sponsors</h2>
        <button type="button" class="neba-btn neba-btn-primary neba-btn-sm" @onclick="OpenAddModalAsync">
            + Add Sponsor
        </button>
    </div>

    @if (_errorMessage is not null)
    {
        <p class="mts-panel__error" role="alert">@_errorMessage</p>
    }

    @if (Sponsors.Count == 0)
    {
        <p class="mts-panel__empty">No sponsors attached to this tournament yet.</p>
    }
    else
    {
        <div class="mts-panel__list">
            @foreach (var sponsor in Sponsors)
            {
                <div class="mts-row">
                    @if (sponsor.LogoUrl is not null)
                    {
                        <img src="@sponsor.LogoUrl" alt="@(sponsor.Name + " logo")" class="mts-row__logo" />
                    }
                    else
                    {
                        <span class="mts-row__logo mts-row__logo--fallback">@sponsor.Name</span>
                    }
                    <div class="mts-row__body">
                        <div class="mts-row__name-line">
                            <span class="mts-row__name">@sponsor.Name</span>
                            @if (sponsor.TitleSponsor)
                            {
                                <span class="neba-badge neba-badge-primary">Title Sponsor</span>
                            }
                        </div>
                        <div class="mts-row__meta">
                            @sponsor.SponsorshipAmount.ToString("C0", CultureInfo.GetCultureInfo("en-US")) added money
                        </div>
                    </div>
                    <button type="button" class="mts-row__remove" @onclick="() => RequestRemove(sponsor)">Remove</button>
                </div>
            }
        </div>
    }
</div>

<AddTournamentSponsorModal IsOpen="_isAddModalOpen"
                            TournamentId="@TournamentId"
                            AvailableSponsors="_availableSponsors"
                            OnAdded="HandleAddedAsync"
                            OnClose="() => _isAddModalOpen = false" />

<ConfirmActionModal IsOpen="_removeTarget is not null"
                     Title="Remove sponsor?"
                     Message="@RemoveMessage"
                     ConfirmLabel="Remove Sponsor"
                     IsBusy="_isRemoving"
                     OnConfirm="ConfirmRemoveAsync"
                     OnCancel="() => _removeTarget = null" />

@code {
    [Parameter, EditorRequired]
    public required string TournamentId { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlyCollection<TournamentDetailSponsorViewModel> Sponsors { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnChanged { get; set; }

    private bool _isAddModalOpen;
    private bool _isRemoving;
    private string? _errorMessage;
    private TournamentDetailSponsorViewModel? _removeTarget;
    private IReadOnlyCollection<SponsorSummaryViewModel> _availableSponsors = [];

    private string RemoveMessage =>
        _removeTarget is null
            ? string.Empty
            : "Remove " + _removeTarget.Name + " as a sponsor of this tournament? This doesn't affect the sponsor's own profile.";

    private async Task OpenAddModalAsync()
    {
        _errorMessage = null;

        var result = await ApiExecutor.ExecuteAsync(
            "Sponsors",
            "ListActiveSponsors",
            ct => SponsorsApi.ListActiveSponsorsAsync(ct));

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        var attachedSlugs = Sponsors.Select(s => s.Slug).ToHashSet();

        _availableSponsors = [.. result.Value.Items
            .Where(s => !attachedSlugs.Contains(s.Slug))
            .Select(s => s.ToViewModel())];

        _isAddModalOpen = true;
    }

    private async Task HandleAddedAsync()
    {
        _isAddModalOpen = false;
        await OnChanged.InvokeAsync();
        ToastService.Show("Sponsor Added", "The sponsor was added to this tournament.", NotifySeverity.Success);
    }

    private void RequestRemove(TournamentDetailSponsorViewModel sponsor)
    {
        _errorMessage = null;
        _removeTarget = sponsor;
    }

    private async Task ConfirmRemoveAsync()
    {
        if (_removeTarget is null)
        {
            return;
        }

        _isRemoving = true;

        var result = await ApiExecutor.ExecuteAsync(
            "TournamentsApi",
            "RemoveTournamentSponsor",
            ct => TournamentsApi.RemoveTournamentSponsorAsync(TournamentId, _removeTarget.Slug, ct));

        _isRemoving = false;
        _removeTarget = null;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        await OnChanged.InvokeAsync();
        ToastService.Show("Sponsor Removed", "The sponsor was removed from this tournament.", NotifySeverity.Success);
    }
}
```

**Note on the Remove call's sponsor identifier**: the mockup and `TournamentDetailSponsorViewModel` only carry the sponsor's `Slug` (used for the public-facing link to `/sponsors/{slug}`), but `RemoveTournamentSponsorAsync`'s route needs the sponsor's ULID `SponsorId`, not its slug. Two options to reconcile at implementation time: (a) add `SponsorId` (string) to `TournamentDetailSponsorViewModel`/`TournamentDetailSponsorResponse`/`TournamentDetailSponsorDto` alongside the two fields already planned, since the domain/DTO layers already have it on `TournamentSponsor`/`Sponsor.Id` — cheap, additive, and avoids a slug→id lookup; or (b) change the remove route to key on slug instead of ID. **(a) is the better fit** — it's a one-line addition at each layer already being touched for `TitleSponsor`/`SponsorshipAmount`, and keeps `SponsorId` as the actual identifier in the URL, consistent with how `AddTournamentSponsorInput.SponsorId` already works. Folding this into the Phase 1 Contracts/mapping code above rather than treating it as a separate change.

```razor
{{-- Tournaments/Detail/AddTournamentSponsorModal.razor --}}
@using Neba.Api.Contracts.Tournaments
@using Neba.Api.Contracts.Tournaments.AddTournamentSponsor
@using Neba.Website.Server.Components
@using Neba.Website.Server.Services
@using Neba.Website.Server.Sponsors

@inject ITournamentsApi TournamentsApi
@inject ApiExecutor ApiExecutor

<NebaModal IsOpen="@IsOpen" OnClose="@OnClose" Title="Add Sponsor" MaxWidth="440px" CompactSize="true">
    <ChildContent>
        <div class="form-field">
            <label for="sponsor-pick">Sponsor <span class="mts-required-hint">(required)</span></label>
            <select id="sponsor-pick" class="neba-select" @bind="_selectedSlug">
                <option value="">Select a sponsor…</option>
                @foreach (var sponsor in AvailableSponsors)
                {
                    <option value="@sponsor.Slug">@sponsor.Name</option>
                }
            </select>
            <p class="mts-hint">Only active sponsors not already attached to this tournament are listed.</p>
        </div>

        <div class="form-field">
            <label for="sponsor-amount">Sponsorship amount <span class="mts-required-hint">(required)</span></label>
            <input id="sponsor-amount" class="neba-input" type="number" min="0" step="0.01" @bind="_sponsorshipAmount" />
        </div>

        <div class="form-field mts-checkbox">
            <input type="checkbox" id="title-sponsor" @bind="_titleSponsor" />
            <label for="title-sponsor">Make this the title sponsor</label>
        </div>
        <p class="mts-hint">Only one tournament sponsor can be the title sponsor at a time.</p>

        @if (_errorMessage is not null)
        {
            <p class="mts-panel__error" role="alert">@_errorMessage</p>
        }
    </ChildContent>
    <FooterContent>
        <div class="mts-modal-actions">
            <button type="button" class="neba-btn neba-btn-secondary" @onclick="OnClose" disabled="@_isSubmitting">Cancel</button>
            <button type="button" class="neba-btn neba-btn-primary" @onclick="SubmitAsync" disabled="@(_isSubmitting || string.IsNullOrEmpty(_selectedSlug))">
                @(_isSubmitting ? "Adding..." : "Add Sponsor")
            </button>
        </div>
    </FooterContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public required string TournamentId { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyCollection<SponsorSummaryViewModel> AvailableSponsors { get; set; } = [];

    [Parameter, EditorRequired]
    public EventCallback OnAdded { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    private string _selectedSlug = string.Empty;
    private decimal _sponsorshipAmount;
    private bool _titleSponsor;
    private bool _isSubmitting;
    private string? _errorMessage;

    private async Task SubmitAsync()
    {
        var sponsor = AvailableSponsors.SingleOrDefault(s => s.Slug == _selectedSlug);

        if (sponsor is null)
        {
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;

        var request = new AddTournamentSponsorRequest
        {
            Id = TournamentId,
            Sponsor = new AddTournamentSponsorInput
            {
                SponsorId = sponsor.Id,
                TitleSponsor = _titleSponsor,
                SponsorshipAmount = _sponsorshipAmount
            }
        };

        var result = await ApiExecutor.ExecuteAsync(
            "TournamentsApi",
            "AddTournamentSponsor",
            ct => TournamentsApi.AddTournamentSponsorAsync(TournamentId, request, ct));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        _selectedSlug = string.Empty;
        _sponsorshipAmount = 0;
        _titleSponsor = false;

        await OnAdded.InvokeAsync();
    }
}
```

**Same identifier note applies here**: `sponsor.Id` above assumes `SponsorSummaryViewModel` carries the sponsor's ULID `SponsorId`, which it currently doesn't (it has `Slug`, not `Id`) — same gap as the Remove call. Options: add `Id`/`SponsorId` to `SponsorSummaryResponse`/`SponsorSummaryDto`/`SponsorSummaryViewModel` (the API-side `ListActiveSponsorsQueryHandler` already has `Sponsor.Id` in scope, so it's a one-line additive projection change, consistent with how `TournamentDetailSponsorResponse` is being extended), or resolve the ID a different way. **Recommend adding `SponsorId` (string) to the `SponsorSummaryResponse`/`Dto`/`ViewModel` chain** — same shape of change as the two additive fields already planned elsewhere in this feature, and avoids a second lookup call. This is a small addition to the existing `Sponsors/ListActiveSponsors` feature (not new to this plan's endpoints) — flagging it here since the Manage Sponsors panel is what surfaces the need.

Remove reuses the **existing** `Components/ConfirmActionModal.razor` unmodified (shown wired up above) — no new component.

### New CSS — `Tournaments/Detail/ManageTournamentSponsors.razor.css`

Scoped styles for `.mts-panel`, `.mts-row`, `.mts-row__logo` (and `--fallback` variant), `.mts-row__remove`, `.mts-panel__empty`, `.mts-panel__error`, `.mts-hint`, `.mts-checkbox`, `.mts-modal-actions`, `.mts-required-hint` — reusing existing tokens (`--neba-blue-600`, `--neba-accent-red`, `--neba-gray-*`, `var(--neba-radius-lg)`, etc.) rather than inventing new values, following the same approach as `TournamentDetail.razor.css`'s existing `.td-rail-sponsor-card` rules. Ported directly from the confirmed mockup's inline `<style>` block.

### Page wiring

```razor
{{-- Tournaments/Detail/TournamentDetail.razor — inside the section where the public Sponsors rail already renders --}}
@using Neba.Api.Contracts.Security

<AuthorizeView Policy="@Permissions.ManageTournamentSponsors.PolicyName">
    <Authorized>
        <ManageTournamentSponsors TournamentId="@Model.Id"
                                   Sponsors="@Model.Sponsors"
                                   OnChanged="ReloadTournamentAsync" />
    </Authorized>
</AuthorizeView>
```

```csharp
{{-- TournamentDetail.razor @code — new method, reuses the existing load logic --}}
private async Task ReloadTournamentAsync()
{
    var result = await ApiExecutor.ExecuteAsync(
        "TournamentsApi",
        "GetTournamentDetail",
        ct => TournamentsApi.GetTournamentAsync(Id, ct));

    if (!result.IsError)
    {
        _model = result.Value.ToViewModel();
    }
}
```

### API Client

`ITournamentsApi.AddTournamentSponsorAsync`/`RemoveTournamentSponsorAsync` (added in Phase 1) are injected directly into the new components — no separate Website-side interface, matching how `TournamentDetail.razor` already injects `ITournamentsApi` for `GetTournamentAsync`. Both calls go through `ApiExecutor`'s existing bodyless-response overload (`ExecuteAsync(string, string, Func<CancellationToken, Task<IApiResponse>>, ct)` → `ErrorOr<Success>`), the same one `EditSponsor.razor`'s `HandleSaveAsync` already uses for its `PUT`.

### State / Dirty-Tracking

No `DirtyFormGuard` needed (confirmed during scoping) — this isn't a data-entry form the page navigates away from; it's a standalone action panel with per-action confirm-then-call semantics, same as `ConfirmActionModal`'s existing delete flows elsewhere in the app.

### `<PageTitle>` / Render Mode

No change — `TournamentDetail.razor` already declares `<PageTitle>` and `@rendermode @(new InteractiveServerRenderMode(prerender: false))`; the new panel is additive content within the existing page, not a new route.

### FAB / List-Page Entry Point

Not applicable — this isn't a creatable list page, it's a section within an existing detail page.

### Tests

- **bUnit** (`Neba.Website.Tests`) — `ManageTournamentSponsors` component tests: renders sponsor rows correctly (badge only on the title sponsor, amount formatting), Add modal opens/filters out already-attached sponsors, Remove opens `ConfirmActionModal` with the right message and calls the API on confirm, error message surfaced on a failed add/remove (e.g. a stale conflict). `AddTournamentSponsorModal` tests: form validation (sponsor required, amount ≥ 0), submit disabled while busy.
- **Playwright** (`tests/e2e/`) — one flow covering the real HTTP round trip: sign in as an authorized user, open a tournament detail page, add a sponsor via the modal, confirm it appears in both the admin panel and the public "Sponsors" section, then remove it and confirm it disappears from both. A second, short test confirms the panel isn't rendered at all for a user without `Tournaments.ManageSponsors`.
- `TournamentDetailViewModelFactory`/`TournamentDetailSponsorViewModelFactory`/`SponsorSummaryViewModelFactory` (all already exist in `Neba.TestFactory`) — extend `TournamentDetailSponsorViewModelFactory.Create()`'s defaults to include `TitleSponsor`/`SponsorshipAmount` per the "Create() must always produce a valid instance" convention; no new factories needed for the modals since their state is local component state, not a data-transfer type.
