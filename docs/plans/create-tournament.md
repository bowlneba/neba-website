# Create Tournament (Without Sponsors)

Adds the ability to create a NEBA tournament — including its oil pattern (pick an existing one or create a new one on the fly, or skip it and set lane-condition categories manually) — with no tournament-sponsor concept yet. Season is derived automatically from the tournament's dates, not picked by the user.

## Decisions locked in during scoping

- **Sponsors are out of scope for this plan entirely.** A separate `/feature-plan` will cover picking a tournament sponsor during creation, once this plan ships. That follow-up should reuse the same "public list endpoint + dropdown, no on-the-fly creation" pattern this plan uses for bowling centers (`ListBowlingCentersEndpoint` → `AllowAnonymous` → dropdown) — sponsors are selected from an existing list, not created inline, mirroring how bowling centers are selected.
- **Oil pattern is optional.** If the user picks or creates an `OilPattern`, `PatternLengthCategory`/`PatternRatioCategory` are derived automatically from it. If no pattern is chosen, the user enters those two category values manually. The two paths are mutually exclusive.
- **Ratio derivation** uses the higher of `OilPattern.LeftRatio`/`RightRatio` (the harder side) to classify `PatternRatioCategory`.
- **Oil pattern creation is a separate API call**, not inline fields on `CreateTournamentCommand`. The UI calls `CreateOilPattern` first (producing a real, reusable `OilPattern` row), then passes its ID into `CreateTournament` — this mirrors how a sponsor logo is uploaded first and then referenced by the create-sponsor call, and keeps `OilPattern` a standalone reusable catalog entry rather than tournament-owned data.
- **Season is not user-selected.** `SeasonId` is derived server-side by finding the `Season` whose date range contains the submitted `StartDate`/`EndDate`. No season dropdown in the UI, no `SeasonId` field on the command.
- **All other existing `Tournament` fields are included in this first create** — bowling center (optional, picked via the existing `ListBowlingCenters` picker, same pattern as bowling centers generally), entry fee, external registration URL, logo, stats-eligible flag. Only `Sponsors` is excluded.
- **New permission**: `Permissions.CreateTournament` (`Tournaments.CreateTournament`), gating the new endpoint the same way `Permissions.CreateSponsor` gates `CreateSponsorEndpoint`. No OR-of-many `CanManageTournaments` policy yet — that can be added when an Edit Tournament feature exists, matching how `CanManageSponsors` only appeared once both Create and Edit existed.

## Phase 1: API

### Domain (`src/Neba.Api/Features/Tournaments/Domain/`)

**`Tournament.cs`** (edit) — add factory:

```csharp
public static ErrorOr<Tournament> Create(
    string name,
    TournamentType tournamentType,
    DateOnly startDate,
    DateOnly endDate,
    SeasonId seasonId,
    bool statsEligible,
    decimal entryFee,
    CertificationNumber? bowlingCenterId = null,
    Uri? externalRegistrationUrl = null,
    StoredFile? logo = null,
    PatternLengthCategory? patternLengthCategory = null,
    PatternRatioCategory? patternRatioCategory = null)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return TournamentErrors.NameRequired;
    }

    if (startDate > endDate)
    {
        return TournamentErrors.EndDateBeforeStartDate(startDate, endDate);
    }

    return new Tournament
    {
        Id = TournamentId.New(),
        Name = name,
        TournamentType = tournamentType,
        StartDate = startDate,
        EndDate = endDate,
        SeasonId = seasonId,
        StatsEligible = statsEligible,
        EntryFee = entryFee,
        BowlingCenterId = bowlingCenterId,
        ExternalRegistrationUrl = externalRegistrationUrl,
        Logo = logo,
        PatternLengthCategory = patternLengthCategory,
        PatternRatioCategory = patternRatioCategory
    };
}
```

It deliberately takes the two category enums as plain optional values — it doesn't know or care whether they came from a picked `OilPattern` or manual entry; the handler resolves that before calling `Create`, per the "aggregate invariants requiring cross-aggregate data" pattern (`Season`/`OilPattern`/`BowlingCenter` lookups all live outside this aggregate). No change to `TournamentOilPattern`/`AddOilPattern` — that collection tracks which pattern was used per *round*, a results-entry concern, not creation.

**`OilPattern.cs`** (edit) — factory plus two computed properties (this is where the "auto-derive from a pattern" formula actually lives):

```csharp
public static ErrorOr<OilPattern> Create(
    string name,
    int length,
    decimal volume,
    decimal leftRatio,
    decimal rightRatio,
    Guid? kegelId = null)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return OilPatternErrors.NameRequired;
    }

    if (length <= 0)
    {
        return OilPatternErrors.LengthMustBePositive;
    }

    if (volume <= 0)
    {
        return OilPatternErrors.VolumeMustBePositive;
    }

    return new OilPattern
    {
        Id = OilPatternId.New(),
        Name = name,
        Length = length,
        Volume = volume,
        LeftRatio = leftRatio,
        RightRatio = rightRatio,
        KegelId = kegelId
    };
}

/// <summary>
/// The length category this pattern's <see cref="Length"/> falls into.
/// </summary>
public PatternLengthCategory LengthCategory
    => PatternLengthCategory.FromLength(Length);

/// <summary>
/// The ratio category derived from the harder (higher) of <see cref="LeftRatio"/>/<see cref="RightRatio"/>.
/// </summary>
public PatternRatioCategory RatioCategory
    => PatternRatioCategory.FromRatio(Math.Max(LeftRatio, RightRatio));
```

`OilPattern` isn't owned by another aggregate — it's a shared reference/catalog entity, same shape as `Sponsor` — so `Create` is `public`, not `internal`.

**`PatternLengthCategory.cs`** (edit) — add:

```csharp
public static PatternLengthCategory FromLength(int length)
    => List.First(category =>
        (category.MinimumLength is null || length >= category.MinimumLength)
        && (category.MaximumLength is null || length <= category.MaximumLength));
```

**`PatternRatioCategory.cs`** (edit) — add:

```csharp
public static PatternRatioCategory FromRatio(decimal ratio)
    => List.First(category =>
        (category.MinimumRatio is null || ratio >= category.MinimumRatio)
        && (category.MaximumRatio is null || ratio <= category.MaximumRatio));
```

**`TournamentErrors.cs`** (edit) — add (needs `using Neba.Api.Features.BowlingCenters.Domain;` for `CertificationNumber`; `System.Globalization` is already imported):

```csharp
public static Error NameRequired
    => Error.Validation("Tournament.Name.Required", "Name must not be empty.");

public static Error EndDateBeforeStartDate(DateOnly startDate, DateOnly endDate)
    => Error.Validation(
        code: "Tournament.EndDateBeforeStartDate",
        description: "End date must not be before start date.",
        metadata: new Dictionary<string, object>
        {
            { "StartDate", startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            { "EndDate", endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
        });

public static Error NoSeasonForDates(DateOnly startDate, DateOnly endDate)
    => Error.Validation(
        code: "Tournament.NoSeasonForDates",
        description: "No season is configured that contains these tournament dates.",
        metadata: new Dictionary<string, object>
        {
            { "StartDate", startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            { "EndDate", endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
        });

public static Error OilPatternNotFound(OilPatternId id)
    => Error.Validation(
        code: "Tournament.OilPatternNotFound",
        description: "The specified oil pattern was not found.",
        metadata: new Dictionary<string, object> { { "OilPatternId", id.Value } });

public static Error BowlingCenterNotFound(CertificationNumber id)
    => Error.Validation(
        code: "Tournament.BowlingCenterNotFound",
        description: "The specified bowling center was not found.",
        metadata: new Dictionary<string, object> { { "CertificationNumber", id.Value } });
```

`BowlingCenterNotFound` is an addition beyond the functional draft: since `BowlingCenterId` is a real FK, the handler validates it exists (same treatment as `OilPatternId`) rather than letting a bad value surface as a raw `DbUpdateException` at `SaveChanges`.

**New `OilPatternErrors.cs`**:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class OilPatternErrors
{
    public static Error NameRequired
        => Error.Validation("OilPattern.Name.Required", "Name must not be empty.");

    public static Error LengthMustBePositive
        => Error.Validation("OilPattern.Length.MustBePositive", "Length must be greater than zero.");

    public static Error VolumeMustBePositive
        => Error.Validation("OilPattern.Volume.MustBePositive", "Volume must be greater than zero.");

    public static Error KegelIdAlreadyExists(Guid kegelId)
        => Error.Conflict(
            code: "OilPattern.KegelId.AlreadyExists",
            description: "A pattern with this Kegel ID already exists.",
            metadata: new Dictionary<string, object> { { "KegelId", kegelId } });
}
```

### Database

**No migration needed.** `tournaments` already has `pattern_length_category`/`pattern_ratio_category` columns (nullable), and `oil_patterns` already has every column `OilPattern.Create` populates, all `IsRequired()` except `kegel_id`.

### Caching (`src/Neba.Api/Caching/CacheDescriptors.cs`)

New top-level sibling class (alongside `BowlingCenters`/`Seasons`/`Tournaments` — `OilPattern` is its own reference/catalog concept even though its code lives under the `Tournaments` feature folder):

```csharp
public static class OilPatterns
{
    /// <summary>
    /// Returns a cache descriptor for the list of oil patterns, with a key and tags that allow
    /// for efficient caching and invalidation of oil pattern data.
    /// </summary>
    public static CacheDescriptor List
        => new()
        {
            Key = "neba:oil-patterns:list",
            Tags = ["neba", "neba:oil-patterns"]
        };
}
```

Unlike `OilPatterns`, `TournamentType` **stays nested under the existing `Tournaments` class** — it's genuinely owned by (and only ever consumed by) the Tournaments feature, not shared reference/catalog data. Add a new member alongside `ListForSeason`/`TournamentDetail`:

```csharp
/// <summary>
/// Returns a cache descriptor for the list of active tournament types.
/// </summary>
public static CacheDescriptor Types
    => new()
    {
        Key = "neba:tournaments:types:list",
        Tags = ["neba", "neba:tournaments", "neba:tournaments:types"]
    };
```

### API — new use-case folders under `src/Neba.Api/Features/Tournaments/`

**`CreateTournament/CreateTournamentCommand.cs`**:

```csharp
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed record CreateTournamentCommand
    : ICommand<CreatedTournament>
{
    public required string Name { get; init; }

    public required TournamentType TournamentType { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required bool StatsEligible { get; init; }

    public required decimal EntryFee { get; init; }

    public CertificationNumber? BowlingCenterId { get; init; }

    public Uri? ExternalRegistrationUrl { get; init; }

    public StoredFile? Logo { get; init; }

    public OilPatternId? OilPatternId { get; init; }

    public PatternLengthCategory? PatternLengthCategory { get; init; }

    public PatternRatioCategory? PatternRatioCategory { get; init; }
}
```

**`CreateTournament/CreatedTournament.cs`**:

```csharp
namespace Neba.Api.Features.Tournaments.CreateTournament;

/// <summary>
/// Result of successfully creating a tournament.
/// </summary>
public sealed record CreatedTournament
{
    /// <summary>
    /// The unique identifier of the newly created tournament.
    /// </summary>
    public required TournamentId Id { get; init; }
}
```

**`CreateTournament/CreateTournamentCommandHandler.cs`**:

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateTournamentCommand, CreatedTournament>
{
    public async Task<ErrorOr<CreatedTournament>> HandleAsync(CreateTournamentCommand command, CancellationToken cancellationToken)
    {
        var season = await appDbContext.Seasons
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.StartDate <= command.StartDate && s.EndDate >= command.EndDate, cancellationToken);

        if (season is null)
        {
            return TournamentErrors.NoSeasonForDates(command.StartDate, command.EndDate);
        }

        if (command.BowlingCenterId is { } bowlingCenterId
            && !await appDbContext.BowlingCenters.AnyAsync(bc => bc.CertificationNumber == bowlingCenterId, cancellationToken))
        {
            return TournamentErrors.BowlingCenterNotFound(bowlingCenterId);
        }

        var patternLengthCategory = command.PatternLengthCategory;
        var patternRatioCategory = command.PatternRatioCategory;

        if (command.OilPatternId is { } oilPatternId)
        {
            var oilPattern = await appDbContext.OilPatterns
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == oilPatternId, cancellationToken);

            if (oilPattern is null)
            {
                return TournamentErrors.OilPatternNotFound(oilPatternId);
            }

            patternLengthCategory = oilPattern.LengthCategory;
            patternRatioCategory = oilPattern.RatioCategory;
        }

        var tournamentResult = Tournament.Create(
            name: command.Name,
            tournamentType: command.TournamentType,
            startDate: command.StartDate,
            endDate: command.EndDate,
            seasonId: season.Id,
            statsEligible: command.StatsEligible,
            entryFee: command.EntryFee,
            bowlingCenterId: command.BowlingCenterId,
            externalRegistrationUrl: command.ExternalRegistrationUrl,
            logo: command.Logo,
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory);

        if (tournamentResult.IsError)
        {
            return tournamentResult.Errors;
        }

        var tournament = tournamentResult.Value;

        await appDbContext.Tournaments.AddAsync(tournament, cancellationToken);

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, tournament.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", token: cancellationToken);

        return new CreatedTournament { Id = tournament.Id };
    }
}
```

**`CreateTournament/CreateTournamentEndpoint.cs`**:

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentEndpoint(Messaging.ICommandHandler<CreateTournamentCommand, CreatedTournament> commandHandler)
    : Endpoint<CreateTournamentRequest, CreatedTournamentResponse>
{
    private readonly Messaging.ICommandHandler<CreateTournamentCommand, CreatedTournament> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateTournament.PolicyName);

        Description(description => description
            .WithName("CreateTournament")
            .WithTags("Admin")
            .Produces<CreatedTournamentResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateTournamentRequest req, CancellationToken ct)
    {
        var input = req.Tournament;

        var command = new CreateTournamentCommand
        {
            Name = input.Name,
            TournamentType = TournamentType.FromName(input.TournamentType),
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StatsEligible = input.StatsEligible,
            EntryFee = input.EntryFee,
            BowlingCenterId = string.IsNullOrWhiteSpace(input.BowlingCenterCertificationNumber)
                ? null
                : new CertificationNumber { Value = input.BowlingCenterCertificationNumber },
            ExternalRegistrationUrl = input.ExternalRegistrationUrl,
            Logo = input.Logo is null
                ? null
                : new StoredFile
                {
                    Container = input.Logo.Container,
                    Path = input.Logo.Path,
                    ContentType = input.Logo.ContentType,
                    SizeInBytes = input.Logo.SizeInBytes
                },
            OilPatternId = string.IsNullOrWhiteSpace(input.OilPatternId)
                ? null
                : new OilPatternId(input.OilPatternId),
            PatternLengthCategory = string.IsNullOrWhiteSpace(input.PatternLengthCategory)
                ? null
                : PatternLengthCategory.FromName(input.PatternLengthCategory),
            PatternRatioCategory = string.IsNullOrWhiteSpace(input.PatternRatioCategory)
                ? null
                : PatternRatioCategory.FromName(input.PatternRatioCategory)
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        var response = new CreatedTournamentResponse
        {
            TournamentId = result.Value.Id.Value.ToString()
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetTournament",
            routeValues: new { id = result.Value.Id.Value.ToString() },
            responseBody: response,
            cancellation: ct);
    }
}
```

No 409 path exists for `CreateTournament` (no uniqueness constraint like a slug), so unlike `CreateSponsorEndpoint` it doesn't need `SponsorMutationResultSender`'s conflict/validation split — every error here is 422.

**`CreateTournament/CreateTournamentRequestValidator.cs`**:

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentRequestValidator
    : Validator<CreateTournamentRequest>
{
    public CreateTournamentRequestValidator()
    {
        RuleFor(r => r.Tournament.Name)
            .NotEmpty().WithErrorCode("CreateTournamentRequest.NameRequired").WithMessage("Name is required.")
            .MaximumLength(127).WithErrorCode("CreateTournamentRequest.NameTooLong").WithMessage("Name must be 127 characters or fewer.");

        RuleFor(r => r.Tournament.TournamentType)
            .NotEmpty().WithErrorCode("CreateTournamentRequest.TournamentTypeRequired").WithMessage("Tournament type is required.")
            .Must(t => TournamentType.List.Any(known => known.Name == t))
            .WithErrorCode("CreateTournamentRequest.TournamentTypeInvalid")
            .WithMessage("Tournament type must be a known, active format.");

        RuleFor(r => r.Tournament.EndDate)
            .GreaterThanOrEqualTo(r => r.Tournament.StartDate)
            .WithErrorCode("CreateTournamentRequest.EndDateBeforeStartDate")
            .WithMessage("End date must not be before start date.");

        RuleFor(r => r.Tournament.EntryFee)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode("CreateTournamentRequest.EntryFeeInvalid")
            .WithMessage("Entry fee must not be negative.");

        RuleFor(r => r.Tournament.ExternalRegistrationUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateTournamentRequest.ExternalRegistrationUrlInvalid")
            .WithMessage("External registration URL must be an absolute URL.")
            .When(r => r.Tournament.ExternalRegistrationUrl is not null);

        RuleFor(r => r.Tournament.PatternLengthCategory)
            .Must(c => PatternLengthCategory.List.Any(known => known.Name == c))
            .WithErrorCode("CreateTournamentRequest.PatternLengthCategoryInvalid")
            .WithMessage("Pattern length category must be one of: Short, Medium, Long.")
            .When(r => !string.IsNullOrWhiteSpace(r.Tournament.PatternLengthCategory));

        RuleFor(r => r.Tournament.PatternRatioCategory)
            .Must(c => PatternRatioCategory.List.Any(known => known.Name == c))
            .WithErrorCode("CreateTournamentRequest.PatternRatioCategoryInvalid")
            .WithMessage("Pattern ratio category must be one of: Sport, Challenge, Recreation.")
            .When(r => !string.IsNullOrWhiteSpace(r.Tournament.PatternRatioCategory));

        RuleFor(r => r.Tournament)
            .Must(t => string.IsNullOrWhiteSpace(t.OilPatternId)
                || (string.IsNullOrWhiteSpace(t.PatternLengthCategory) && string.IsNullOrWhiteSpace(t.PatternRatioCategory)))
            .WithErrorCode("CreateTournamentRequest.OilPatternAndManualCategoriesConflict")
            .WithMessage("Provide either an oil pattern ID or manual pattern categories, not both.");
    }
}
```

**`CreateTournament/CreateTournamentSummary.cs`** — same shape as `CreateSponsorSummary`: `Summary`/`Description` strings naming the permission, `Response(201, ...)` example, `Response(400/401/403/422, ...)` explanations (no 409).

**Routing correction**: oil patterns are reusable reference/catalog data — the same category of thing as `BowlingCenter`/`Season`, both of which get their own top-level route rather than living under whichever feature consumes them first. So `CreateOilPattern`/`ListOilPatterns` get their own top-level route group, not `Group<TournamentsEndpointGroup>()`. The C# files stay physically under `Features/Tournaments/` for this phase (no `Features/OilPatterns/` domain-folder split) — only the HTTP surface and Refit contract move.

**New `OilPatternsEndpointGroup.cs`** (top-level, mirrors `BowlingCentersEndpointGroup`):

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Neba.Api.Features.Tournaments;

internal sealed class OilPatternsEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public OilPatternsEndpointGroup()
    {
        VersionSets.CreateApi("OilPatterns", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("oil-patterns", endpoint => endpoint
            .Description(description => description
                .WithTags("OilPatterns")
                .ProducesProblemDetails(500)));
    }
}
```

**`CreateOilPattern/`** — mirrors `CreateSponsor/` exactly in shape.

`CreateOilPatternCommand.cs`:

```csharp
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed record CreateOilPatternCommand
    : ICommand<CreatedOilPattern>
{
    public required string Name { get; init; }

    public required int Length { get; init; }

    public required decimal Volume { get; init; }

    public required decimal LeftRatio { get; init; }

    public required decimal RightRatio { get; init; }

    public Guid? KegelId { get; init; }
}
```

`CreatedOilPattern.cs`:

```csharp
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

/// <summary>
/// Result of successfully creating an oil pattern, including its derived lane-condition categories.
/// </summary>
public sealed record CreatedOilPattern
{
    public required OilPatternId Id { get; init; }

    public required string Name { get; init; }

    public required int Length { get; init; }

    public required PatternLengthCategory LengthCategory { get; init; }

    public required PatternRatioCategory RatioCategory { get; init; }
}
```

`CreateOilPatternCommandHandler.cs`:

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>
{
    public async Task<ErrorOr<CreatedOilPattern>> HandleAsync(CreateOilPatternCommand command, CancellationToken cancellationToken)
    {
        var kegelIdTaken = command.KegelId is { } kegelId
            && await appDbContext.OilPatterns.AnyAsync(p => p.KegelId == kegelId, cancellationToken);

        if (kegelIdTaken)
        {
            return OilPatternErrors.KegelIdAlreadyExists(command.KegelId!.Value);
        }

        var oilPatternResult = OilPattern.Create(
            name: command.Name,
            length: command.Length,
            volume: command.Volume,
            leftRatio: command.LeftRatio,
            rightRatio: command.RightRatio,
            kegelId: command.KegelId);

        if (oilPatternResult.IsError)
        {
            return oilPatternResult.Errors;
        }

        var oilPattern = oilPatternResult.Value;

        await appDbContext.OilPatterns.AddAsync(oilPattern, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:oil-patterns", token: cancellationToken);

        return new CreatedOilPattern
        {
            Id = oilPattern.Id,
            Name = oilPattern.Name,
            Length = oilPattern.Length,
            LengthCategory = oilPattern.LengthCategory,
            RatioCategory = oilPattern.RatioCategory
        };
    }
}
```

`CreateOilPatternEndpoint.cs` — `POST /oil-patterns`, same `CreateTournament` permission (creating a pattern is part of the tournament-creation flow, not a separate permission, even though the route itself is top-level):

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternEndpoint(Messaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern> commandHandler)
    : Endpoint<CreateOilPatternRequest, CreatedOilPatternResponse>
{
    private readonly Messaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<OilPatternsEndpointGroup>();

        Options(options => options
            .WithVersionSet("OilPatterns")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateTournament.PolicyName);

        Description(description => description
            .WithName("CreateOilPattern")
            .WithTags("Admin")
            .Produces<CreatedOilPatternResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateOilPatternRequest req, CancellationToken ct)
    {
        var command = new CreateOilPatternCommand
        {
            Name = req.Name,
            Length = req.Length,
            Volume = req.Volume,
            LeftRatio = req.LeftRatio,
            RightRatio = req.RightRatio,
            KegelId = req.KegelId
        };

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
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            // Stryker disable once Statement
            return;
        }

        var response = new CreatedOilPatternResponse
        {
            OilPatternId = result.Value.Id.Value.ToString(),
            Name = result.Value.Name,
            Length = result.Value.Length,
            LengthCategory = result.Value.LengthCategory.Name,
            RatioCategory = result.Value.RatioCategory.Name
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct); // 200, not 201 — no GetOilPattern-by-id endpoint exists to point a Location header at (same treatment as UploadSponsorLogoEndpoint)
    }
}
```

`CreateOilPatternRequestValidator.cs`:

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

namespace Neba.Api.Features.Tournaments.CreateOilPattern;

internal sealed class CreateOilPatternRequestValidator
    : Validator<CreateOilPatternRequest>
{
    public CreateOilPatternRequestValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithErrorCode("CreateOilPatternRequest.NameRequired").WithMessage("Name is required.")
            .MaximumLength(63).WithErrorCode("CreateOilPatternRequest.NameTooLong").WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Length)
            .GreaterThan(0).WithErrorCode("CreateOilPatternRequest.LengthMustBePositive").WithMessage("Length must be greater than zero.");

        RuleFor(r => r.Volume)
            .GreaterThan(0).WithErrorCode("CreateOilPatternRequest.VolumeMustBePositive").WithMessage("Volume must be greater than zero.");

        RuleFor(r => r.LeftRatio)
            .GreaterThanOrEqualTo(0).WithErrorCode("CreateOilPatternRequest.LeftRatioInvalid").WithMessage("Left ratio must not be negative.");

        RuleFor(r => r.RightRatio)
            .GreaterThanOrEqualTo(0).WithErrorCode("CreateOilPatternRequest.RightRatioInvalid").WithMessage("Right ratio must not be negative.");
    }
}
```

`CreateOilPatternSummary.cs` — same shape as `CreateSponsorSummary`.

**`ListOilPatterns/`** — mirrors `ListBowlingCenters/`/`ListSeasons/` exactly (public, cached, no request).

`OilPatternSummaryDto.cs`:

```csharp
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

public sealed record OilPatternSummaryDto
{
    public required OilPatternId Id { get; init; }

    public required string Name { get; init; }

    public required int Length { get; init; }

    public required decimal Volume { get; init; }

    public required decimal LeftRatio { get; init; }

    public required decimal RightRatio { get; init; }

    public Guid? KegelId { get; init; }

    public required string LengthCategory { get; init; }

    public required string RatioCategory { get; init; }
}
```

`ListOilPatternsQuery.cs`:

```csharp
using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed record ListOilPatternsQuery
    : ICachedQuery<IReadOnlyCollection<OilPatternSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.OilPatterns.List;

    public TimeSpan Expiry
        => TimeSpan.FromDays(90);
}
```

`ListOilPatternsQueryHandler.cs` — **important EF translation note**: `OilPattern.LengthCategory`/`RatioCategory` are computed C# properties, not mapped columns, so they can't be referenced inside a `.Select()` that EF translates to SQL. The handler must materialize the raw scalar columns first (same two-step pattern `ListActiveSponsorsQueryHandler` already uses for its logo-URL computation), then compute the categories in plain C# afterward:

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed class ListOilPatternsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>>
{
    private readonly IQueryable<OilPattern> _oilPatterns = appDbContext.OilPatterns.AsNoTracking();

    public async Task<IReadOnlyCollection<OilPatternSummaryDto>> HandleAsync(ListOilPatternsQuery query, CancellationToken cancellationToken)
    {
        var rows = await _oilPatterns
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Length,
                p.Volume,
                p.LeftRatio,
                p.RightRatio,
                p.KegelId
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new OilPatternSummaryDto
        {
            Id = row.Id,
            Name = row.Name,
            Length = row.Length,
            Volume = row.Volume,
            LeftRatio = row.LeftRatio,
            RightRatio = row.RightRatio,
            KegelId = row.KegelId,
            LengthCategory = PatternLengthCategory.FromLength(row.Length).Name,
            RatioCategory = PatternRatioCategory.FromRatio(Math.Max(row.LeftRatio, row.RightRatio)).Name
        })];
    }
}
```

`ListOilPatternsEndpoint.cs` — `GET /oil-patterns`, `AllowAnonymous()`:

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListOilPatterns;

internal sealed class ListOilPatternsEndpoint(IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<OilPatternSummaryResponse>>
{
    private readonly IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get(string.Empty);
        Group<OilPatternsEndpointGroup>();

        Options(options => options
            .WithVersionSet("OilPatterns")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListOilPatterns")
            .WithTags("Public")
            .Produces<CollectionResponse<OilPatternSummaryResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListOilPatternsQuery(), ct);

        var response = new CollectionResponse<OilPatternSummaryResponse>
        {
            Items = [.. result.Select(p => new OilPatternSummaryResponse
            {
                OilPatternId = p.Id.Value.ToString(),
                Name = p.Name,
                Length = p.Length,
                Volume = p.Volume,
                LeftRatio = p.LeftRatio,
                RightRatio = p.RightRatio,
                KegelId = p.KegelId,
                LengthCategory = p.LengthCategory,
                RatioCategory = p.RatioCategory
            })]
        };

        await Send.OkAsync(response, ct);
    }
}
```

`ListOilPatternsSummary.cs` — same shape as `ListSeasonsSummary`.

**`ListTournamentTypes/`** — solves the "two spots to update" problem for the tournament-type dropdown: the Blazor form fetches this list instead of hand-maintaining a duplicate of `TournamentType.List`, so adding a new tournament type only ever means editing `TournamentType.cs`. Purely in-memory (no `AppDbContext` — `TournamentType.List` is a compiled `SmartEnum` list, not a database table), but still cached like the other list endpoints per the "we don't add tournament types often" reasoning — consistency of shape over the (negligible) cost saved.

`TournamentTypeSummaryDto.cs`:

```csharp
namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

public sealed record TournamentTypeSummaryDto
{
    public required string Name { get; init; }
}
```

`ListTournamentTypesQuery.cs`:

```csharp
using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed record ListTournamentTypesQuery
    : ICachedQuery<IReadOnlyCollection<TournamentTypeSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.Types;

    public TimeSpan Expiry
        => TimeSpan.FromDays(90);
}
```

`ListTournamentTypesQueryHandler.cs` — no DB query; projects the domain `SmartEnum` list directly, filtered to active formats only (the same list `CreateTournamentRequestValidator` already checks submitted values against):

```csharp
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed class ListTournamentTypesQueryHandler
    : IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>>
{
    public Task<IReadOnlyCollection<TournamentTypeSummaryDto>> HandleAsync(ListTournamentTypesQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<TournamentTypeSummaryDto>>(
            [.. TournamentType.List
                .Where(t => t.ActiveFormat)
                .Select(t => new TournamentTypeSummaryDto { Name = t.Name })]);
}
```

`ListTournamentTypesEndpoint.cs` — `GET /tournaments/types`, `AllowAnonymous()`, same shape as `ListOilPatternsEndpoint` but on `TournamentsEndpointGroup` (this data is tournament-owned, not a shared catalog):

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Tournaments.ListTournamentTypes;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed class ListTournamentTypesEndpoint(IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>> queryHandler)
    : EndpointWithoutRequest<CollectionResponse<TournamentTypeSummaryResponse>>
{
    private readonly IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("types");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("ListTournamentTypes")
            .WithTags("Public")
            .Produces<CollectionResponse<TournamentTypeSummaryResponse>>(StatusCodes.Status200OK));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new ListTournamentTypesQuery(), ct);

        var response = new CollectionResponse<TournamentTypeSummaryResponse>
        {
            Items = [.. result.Select(t => new TournamentTypeSummaryResponse { Name = t.Name })]
        };

        await Send.OkAsync(response, ct);
    }
}
```

`ListTournamentTypesSummary.cs` — same shape as `ListSeasonsSummary`.

**`UploadTournamentLogo/`** — mirrors `Sponsors/UploadSponsorLogo/` file-for-file: `UploadTournamentLogoEndpoint.cs` (`Post("logo")`, `Group<TournamentsEndpointGroup>()`, `Policies(PermissionCatalog.CreateTournament.PolicyName)`, stages via `IUploadStagingService.StageUploadAsync(req.File, "bowlneba-public", "tournaments/logo", null, ct)`), `UploadTournamentLogoRequestValidator.cs` (identical content-type/size rules to `UploadSponsorLogoRequestValidator`), `UploadTournamentLogoSummary.cs`.

**New `TournamentPendingUploadCleaner.cs`** (mirrors `SponsorPendingUploadCleaner` exactly, scoped to tournament logos):

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.Tournaments;

internal static class TournamentPendingUploadCleaner
{
    public static async Task RemoveClaimedAsync(AppDbContext appDbContext, StoredFile? logo, CancellationToken cancellationToken)
    {
        if (logo is null)
        {
            return;
        }

        var claimed = await appDbContext.PendingUploads
            .Where(pending => pending.Container == logo.Container && pending.Path == logo.Path)
            .ToListAsync(cancellationToken);

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}
```

### Authorization

**`Permission.cs`** (edit) — new region:

```csharp
#region Tournaments

/// <summary>
/// Permission to create a tournament.
/// </summary>
public static readonly Permissions CreateTournament = new("Tournaments.CreateTournament", "Create Tournament");

#endregion
```

No `TournamentManagementPermissions` collection / OR-policy yet (matches the Sponsors precedent). The three mutating endpoints (`CreateTournament`, `CreateOilPattern`, `UploadTournamentLogo`) gate on `Permissions.CreateTournament.PolicyName`; the two list endpoints (`ListOilPatterns`, `ListTournamentTypes`) are `AllowAnonymous()`. `docs/policies/README.md` needs no new row — the generic dynamic `Permission:{value}` row already documents this.

### Contracts (`src/Neba.Api.Contracts/Tournaments/`)

**`CreateTournament/TournamentLogoInput.cs`** — identical shape to `SponsorLogoInput` (`Container`, `Path`, `ContentType`, `SizeInBytes`, all `required`).

**`CreateTournament/TournamentInput.cs`**:

```csharp
namespace Neba.Api.Contracts.Tournaments.CreateTournament;

/// <summary>
/// The fields required to create a tournament.
/// </summary>
public sealed record TournamentInput
{
    public required string Name { get; init; }

    /// <summary>
    /// The tournament type name (see <c>TournamentType</c>).
    /// </summary>
    public required string TournamentType { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required bool StatsEligible { get; init; }

    public required decimal EntryFee { get; init; }

    /// <summary>
    /// The certification number of the bowling center hosting this tournament; null if not yet assigned.
    /// </summary>
    public string? BowlingCenterCertificationNumber { get; init; }

    public Uri? ExternalRegistrationUrl { get; init; }

    /// <summary>
    /// The tournament's logo image, already uploaded to storage.
    /// </summary>
    public TournamentLogoInput? Logo { get; init; }

    /// <summary>
    /// An existing or newly-created oil pattern's ID. When set, <see cref="PatternLengthCategory"/>
    /// and <see cref="PatternRatioCategory"/> must be null — they're derived from the pattern instead.
    /// </summary>
    public string? OilPatternId { get; init; }

    /// <summary>
    /// Manual pattern length category name (see <c>PatternLengthCategory</c>). Only valid when
    /// <see cref="OilPatternId"/> is null.
    /// </summary>
    public string? PatternLengthCategory { get; init; }

    /// <summary>
    /// Manual pattern ratio category name (see <c>PatternRatioCategory</c>). Only valid when
    /// <see cref="OilPatternId"/> is null.
    /// </summary>
    public string? PatternRatioCategory { get; init; }
}
```

**`CreateTournament/CreateTournamentRequest.cs`**:

```csharp
namespace Neba.Api.Contracts.Tournaments.CreateTournament;

public sealed record CreateTournamentRequest
{
    public required TournamentInput Tournament { get; init; }
}
```

**`CreateTournament/CreatedTournamentResponse.cs`**:

```csharp
namespace Neba.Api.Contracts.Tournaments.CreateTournament;

public sealed record CreatedTournamentResponse
{
    public required string TournamentId { get; init; }
}
```

**Note**: `CreateOilPattern`/`ListOilPatterns` contract types move to their own top-level `src/Neba.Api.Contracts/OilPatterns/` folder — sibling to `Tournaments/`, mirroring `BowlingCenters/`/`Seasons/` — rather than living under `Contracts/Tournaments/`, per the routing correction above.

**`OilPatterns/CreateOilPattern/CreateOilPatternRequest.cs`**:

```csharp
namespace Neba.Api.Contracts.OilPatterns.CreateOilPattern;

public sealed record CreateOilPatternRequest
{
    public required string Name { get; init; }

    public required int Length { get; init; }

    public required decimal Volume { get; init; }

    public required decimal LeftRatio { get; init; }

    public required decimal RightRatio { get; init; }

    public Guid? KegelId { get; init; }
}
```

**`OilPatterns/CreateOilPattern/CreatedOilPatternResponse.cs`**:

```csharp
namespace Neba.Api.Contracts.OilPatterns.CreateOilPattern;

public sealed record CreatedOilPatternResponse
{
    public required string OilPatternId { get; init; }

    public required string Name { get; init; }

    public required int Length { get; init; }

    public required string LengthCategory { get; init; }

    public required string RatioCategory { get; init; }
}
```

**`OilPatterns/ListOilPatterns/OilPatternSummaryResponse.cs`**:

```csharp
namespace Neba.Api.Contracts.OilPatterns.ListOilPatterns;

public sealed record OilPatternSummaryResponse
{
    public required string OilPatternId { get; init; }

    public required string Name { get; init; }

    public required int Length { get; init; }

    public required decimal Volume { get; init; }

    public required decimal LeftRatio { get; init; }

    public required decimal RightRatio { get; init; }

    public Guid? KegelId { get; init; }

    public required string LengthCategory { get; init; }

    public required string RatioCategory { get; init; }
}
```

**New `OilPatterns/IOilPatternsApi.cs`** (mirrors `IBowlingCentersApi`/`ISeasonsApi` — its own top-level Refit contract, not bolted onto `ITournamentsApi`):

```csharp
using Neba.Api.Contracts.OilPatterns.CreateOilPattern;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;

using Refit;

namespace Neba.Api.Contracts.OilPatterns;

/// <summary>
/// Defines the oil patterns API contract.
/// </summary>
public interface IOilPatternsApi
{
    /// <summary>
    /// Lists all oil patterns available to choose from when creating a tournament.
    /// </summary>
    [Get("/oil-patterns")]
    Task<IApiResponse<CollectionResponse<OilPatternSummaryResponse>>> ListOilPatternsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable oil pattern.
    /// </summary>
    [Post("/oil-patterns")]
    Task<IApiResponse<CreatedOilPatternResponse>> CreateOilPatternAsync(CreateOilPatternRequest request, CancellationToken cancellationToken = default);
}
```

**`ListTournamentTypes/TournamentTypeSummaryResponse.cs`**:

```csharp
namespace Neba.Api.Contracts.Tournaments.ListTournamentTypes;

public sealed record TournamentTypeSummaryResponse
{
    public required string Name { get; init; }
}
```

**`ITournamentsApi.cs`** (edit) — add the tournament-specific members (oil pattern methods live on `IOilPatternsApi` instead):

```csharp
/// <summary>
/// Lists all active tournament types available when creating a tournament.
/// </summary>
[Get("/tournaments/types")]
Task<IApiResponse<CollectionResponse<TournamentTypeSummaryResponse>>> ListTournamentTypesAsync(CancellationToken cancellationToken = default);

/// <summary>
/// Uploads a tournament logo. Requires the Tournaments.CreateTournament permission.
/// </summary>
[Multipart]
[Post("/tournaments/logo")]
Task<IApiResponse<UploadedFileResponse>> UploadTournamentLogoAsync(
    [AliasAs("File")] StreamPart file,
    CancellationToken cancellationToken = default);

/// <summary>
/// Creates a tournament.
/// </summary>
[Post("/tournaments")]
Task<IApiResponse<CreatedTournamentResponse>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken cancellationToken = default);
```

### Test Factories (`tests/Neba.TestFactory/Tournaments/`)

- `TournamentFactory`/`OilPatternFactory` (existing) need no changes — both already construct via object initializer with every field represented, independent of the new `Create()` factories.
- New factories, following the established `Create()`-with-nullable-params-and-const-defaults shape: `CreateTournamentCommandFactory`, `CreateOilPatternCommandFactory`, `OilPatternSummaryDtoFactory`, `TournamentTypeSummaryDtoFactory`, `CreatedTournamentFactory`, `CreatedOilPatternFactory`, plus Contracts-side equivalents `CreateTournamentRequestFactory`, `TournamentInputFactory`, `CreateOilPatternRequestFactory`, `CreatedTournamentResponseFactory`, `CreatedOilPatternResponseFactory`, `OilPatternSummaryResponseFactory`, `TournamentTypeSummaryResponseFactory`.

### Tests

- **Domain**: `Tournament.Create` (valid; missing name; start > end); `OilPattern.Create` (valid; missing name; non-positive length/volume); `OilPattern.LengthCategory`/`RatioCategory` computed-property cases at each threshold boundary; `PatternLengthCategory.FromLength`/`PatternRatioCategory.FromRatio` boundary tests.
- **Handlers**: `CreateTournamentCommandHandlerTests` — season derived correctly from dates; no matching season; oil pattern id resolves categories; oil pattern not found; bowling center not found; manual categories path; bowling center omitted (null FK) is valid. `CreateOilPatternCommandHandlerTests` — happy path; duplicate Kegel ID conflict. `ListOilPatternsQueryHandlerTests` — basic projection, including a case that proves the category strings are computed post-materialization (e.g. via an in-memory EF provider, since a real SQL provider would fail to translate a naive single-pass `.Select()` — the whole reason for the two-step handler shape above). `ListTournamentTypesQueryHandlerTests` — returns only `ActiveFormat` types, in the same shape `TournamentType.List` exposes them (no DB/mocking needed at all — a plain call, no `MockBehavior.Strict` setups since there's nothing to mock).
- **Endpoints**: Configure/HandleAsync unit tests for all five new endpoints, following the FastEndpoints unit-test limitations already documented in CLAUDE.md (`ignore-methods`, `LinkGenerator` throw pattern for `Send.CreatedAtAsync` in `CreateTournamentEndpoint`, etc.).
- **Validators**: `CreateTournamentRequestValidator` (required fields, date ordering, mutual-exclusivity of oil-pattern-id vs. manual categories); `CreateOilPatternRequestValidator` (required fields, positive numbers).
- **Cache descriptor**: run `/cache-descriptor` for `ListOilPatternsQuery` and `ListTournamentTypesQuery` once they exist, to generate their `CacheDescriptors.cs` entries' tests per that skill's convention.

### Deferred / out of scope for this phase

- Tournament sponsors entirely (separate future `/feature-plan`).
- Per-round oil pattern tracking (`Tournament.OilPatterns`/`AddOilPattern`) — unrelated to creation, belongs to a future results-entry feature.
- Editing an already-created tournament (no `EditTournament` in this plan).
- A `CanManageTournaments` OR-policy (add once Edit exists).

## Phase 2: UI

### Pages (`src/Neba.Website.Server/Tournaments/`)

**New `CreateTournament.razor`** — route `/tournaments/new`:

```razor
@page "/tournaments/new"
@using System.ComponentModel.DataAnnotations
@using ErrorOr
@using Neba.Api.Contracts.BowlingCenters
@using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Tournaments
@using Neba.Api.Contracts.Tournaments.CreateTournament
@using Neba.Api.Contracts.Tournaments.ListTournamentTypes
@using Neba.Api.Contracts.Uploads
@using Neba.Website.Server.Notifications
@using Neba.Website.Server.Services
@using Refit
@implements IAsyncDisposable
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ITournamentsApi TournamentsApi
@inject IBowlingCentersApi BowlingCentersApi
@inject NavigationManager NavigationManager
@inject ToastService ToastService

<PageTitle>Create Tournament - BowlNEBA</PageTitle>

<AuthorizeView Policy="@Permissions.CreateTournament.PolicyName" Context="authContext">
    <Authorized>
        <div class="neba-space-y-6">

            <div class="page-title-bar">
                <div class="page-title-inner">
                    <h1>Create Tournament</h1>
                    <p>Add a new tournament to the NEBA schedule</p>
                </div>
            </div>

            @if (!string.IsNullOrWhiteSpace(_errorMessage))
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to Create Tournament" Message="@_errorMessage" Dismissible="true"
                           OnDismiss="@(() => _errorMessage = null)" />
            }

            <DirtyFormGuard IsDirty="@_isDirty" />

            <div class="neba-card">
                <EditForm EditContext="_editContext" FormName="CreateTournamentForm" OnValidSubmit="HandleCreateAsync">
                    <DataAnnotationsValidator />
                    <div class="neba-space-y-6">

                        <section class="neba-space-y-4">
                            <h2 class="create-tournament-section-title">Basic Info</h2>

                            <div>
                                <label for="name" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Name</label>
                                <InputText id="name" @bind-Value="_model.Name" class="neba-input" placeholder="e.g. NEBA Fall Classic" />
                                <ValidationMessage For="@(() => _model.Name)" class="block text-sm text-red-600 mt-1" />
                            </div>

                            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                <div>
                                    <label for="tournament-type" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Tournament Type</label>
                                    <InputSelect id="tournament-type" @bind-Value="_model.TournamentType" class="neba-select">
                                        @foreach (var type in _tournamentTypes)
                                        {
                                            <option value="@type.Name">@type.Name</option>
                                        }
                                    </InputSelect>
                                    <ValidationMessage For="@(() => _model.TournamentType)" class="block text-sm text-red-600 mt-1" />
                                </div>

                                <div>
                                    <label for="start-date" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Start Date</label>
                                    <InputDate id="start-date" @bind-Value="_model.StartDate" class="neba-input" />
                                    <ValidationMessage For="@(() => _model.StartDate)" class="block text-sm text-red-600 mt-1" />
                                </div>

                                <div>
                                    <label for="end-date" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">End Date</label>
                                    <InputDate id="end-date" @bind-Value="_model.EndDate" class="neba-input" />
                                    <ValidationMessage For="@(() => _model.EndDate)" class="block text-sm text-red-600 mt-1" />
                                </div>
                            </div>
                            <p class="text-sm text-[var(--neba-gray-500)]">The season is determined automatically from these dates.</p>

                            <div class="flex items-center gap-2">
                                <InputCheckbox id="stats-eligible" @bind-Value="_model.StatsEligible" />
                                <label for="stats-eligible" class="text-sm font-medium text-[var(--neba-gray-700)]">Counts toward season stats and awards</label>
                            </div>
                        </section>

                        <section class="neba-space-y-4">
                            <h2 class="create-tournament-section-title">Venue &amp; Entry Fee</h2>

                            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                <div>
                                    <label for="bowling-center" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Bowling Center</label>
                                    <InputSelect id="bowling-center" @bind-Value="_model.BowlingCenterCertificationNumber" class="neba-select">
                                        <option value="">Not yet assigned</option>
                                        @foreach (var center in _bowlingCenters)
                                        {
                                            <option value="@center.CertificationNumber">@center.Name — @center.Address.City, @center.Address.Region</option>
                                        }
                                    </InputSelect>
                                </div>

                                <div>
                                    <label for="entry-fee" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Entry Fee</label>
                                    <InputNumber id="entry-fee" @bind-Value="_model.EntryFee" class="neba-input" />
                                    <ValidationMessage For="@(() => _model.EntryFee)" class="block text-sm text-red-600 mt-1" />
                                </div>
                            </div>

                            <div>
                                <label for="registration-url" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">External Registration URL</label>
                                <InputText id="registration-url" @bind-Value="_model.ExternalRegistrationUrl" class="neba-input" placeholder="https://…" />
                                <ValidationMessage For="@(() => _model.ExternalRegistrationUrl)" class="block text-sm text-red-600 mt-1" />
                            </div>
                        </section>

                        <section class="neba-space-y-4">
                            <h2 class="create-tournament-section-title">Oil Pattern</h2>
                            <p class="text-sm text-[var(--neba-gray-500)]">Optional. Pick a pattern to auto-fill lane condition, create a new one, or set the condition manually.</p>
                            <OilPatternPicker SelectionChanged="HandleOilPatternSelectionChanged" />
                        </section>

                        <section class="neba-space-y-4">
                            <h2 class="create-tournament-section-title">Logo</h2>
                            <FileUpload MaxFiles="1" Accept="image/*" MaxFileSizeBytes="@(5 * 1024 * 1024)" Label="Upload a logo"
                                        OnUploadRequestedAsync="UploadLogoAsync"
                                        OnFileUploaded="@(response => { _logo = response; MarkDirty(); })"
                                        OnFileRemoved="@(_ => { _logo = null; MarkDirty(); })"
                                        OnBusyChanged="@(busy => _isLogoUploading = busy)" />
                        </section>

                        @if (_isLogoUploading)
                        {
                            <p class="text-sm text-[var(--neba-gray-500)]">Uploading logo…</p>
                        }

                        <div class="flex items-center gap-3">
                            <button type="submit" class="neba-btn neba-btn-primary" disabled="@(_isSubmitting || _isLogoUploading)">
                                @(_isSubmitting ? "Creating…" : "Create Tournament")
                            </button>
                            <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancel" disabled="@_isSubmitting">
                                Cancel
                            </button>
                        </div>

                    </div>
                </EditForm>
            </div>

        </div>
    </Authorized>
    <NotAuthorized>
        <div class="news-empty">
            <p class="news-empty-text">You don't have permission to create tournaments.</p>
            <a href="/tournaments" class="neba-btn neba-btn-secondary">Back to Tournaments</a>
        </div>
    </NotAuthorized>
</AuthorizeView>

@code {
    private readonly CreateTournamentFormModel _model = new();
    private readonly EditContext _editContext;

    private UploadedFileResponse? _logo;
    private OilPatternSelection _oilPatternSelection = new();
    private bool _isLogoUploading;
    private bool _isSubmitting;
    private bool _isDirty;
    private string? _errorMessage;

    private IReadOnlyCollection<BowlingCenterSummaryResponse> _bowlingCenters = [];
    private IReadOnlyCollection<TournamentTypeSummaryResponse> _tournamentTypes = [];

    public CreateTournament()
    {
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        var bowlingCentersResult = await ApiExecutor.ExecuteAsync(
            "BowlingCenters",
            "ListBowlingCenters",
            BowlingCentersApi.ListBowlingCentersAsync);

        if (!bowlingCentersResult.IsError)
        {
            _bowlingCenters = bowlingCentersResult.Value.Items;
        }

        var tournamentTypesResult = await ApiExecutor.ExecuteAsync(
            "Tournaments",
            "ListTournamentTypes",
            TournamentsApi.ListTournamentTypesAsync);

        if (!tournamentTypesResult.IsError)
        {
            _tournamentTypes = tournamentTypesResult.Value.Items;
        }
    }

    private void MarkDirty() => _isDirty = true;

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    private void HandleOilPatternSelectionChanged(OilPatternSelection selection)
    {
        _oilPatternSelection = selection;
        MarkDirty();
    }

    private async Task<ErrorOr<UploadedFileResponse>> UploadLogoAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => await ApiExecutor.ExecuteAsync(
            "Tournaments",
            "UploadTournamentLogo",
            c => TournamentsApi.UploadTournamentLogoAsync(new StreamPart(stream, fileName, contentType), c),
            ct);

    private async Task HandleCreateAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;

        var request = new CreateTournamentRequest { Tournament = BuildTournamentInput() };

        var result = await ApiExecutor.ExecuteAsync(
            "Tournaments",
            "CreateTournament",
            ct => TournamentsApi.CreateTournamentAsync(request, ct));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        _isDirty = false;
        ToastService.Show("Tournament Created", "\"" + _model.Name + "\" was successfully created.", NotifySeverity.Success);

        // See CreateSponsor.razor's HandleCreateAsync for why the Task.Yield() below is required —
        // DirtyFormGuard only observes _isDirty once this component's queued re-render actually runs.
        StateHasChanged();
        await Task.Yield();

        NavigationManager.NavigateTo($"/tournaments/{result.Value.TournamentId}");
    }

    private void HandleCancel() => NavigationManager.NavigateTo("/tournaments");

    private TournamentInput BuildTournamentInput() => new()
    {
        Name = _model.Name,
        TournamentType = _model.TournamentType,
        StartDate = _model.StartDate!.Value,
        EndDate = _model.EndDate!.Value,
        StatsEligible = _model.StatsEligible,
        EntryFee = _model.EntryFee,
        BowlingCenterCertificationNumber = string.IsNullOrWhiteSpace(_model.BowlingCenterCertificationNumber)
            ? null
            : _model.BowlingCenterCertificationNumber,
        ExternalRegistrationUrl = ParseUri(_model.ExternalRegistrationUrl),
        Logo = BuildLogoInput(),
        OilPatternId = _oilPatternSelection.OilPatternId,
        PatternLengthCategory = _oilPatternSelection.PatternLengthCategory,
        PatternRatioCategory = _oilPatternSelection.PatternRatioCategory
    };

    private static Uri? ParseUri(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;

    private TournamentLogoInput? BuildLogoInput() => _logo is null ? null : new TournamentLogoInput
    {
        Container = _logo.Container,
        Path = _logo.Path,
        ContentType = _logo.ContentType,
        SizeInBytes = _logo.SizeInBytes
    };

    public ValueTask DisposeAsync()
    {
        _editContext.OnFieldChanged -= HandleFieldChanged;
        return ValueTask.CompletedTask;
    }

    private sealed class CreateTournamentFormModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(127, ErrorMessage = "Name must be 127 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tournament type is required.")]
        public string TournamentType { get; set; } = "Singles";

        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateOnly? EndDate { get; set; }

        public bool StatsEligible { get; set; } = true;

        [Range(0, double.MaxValue, ErrorMessage = "Entry fee must be zero or greater.")]
        public decimal EntryFee { get; set; }

        public string? BowlingCenterCertificationNumber { get; set; }

        [Url(ErrorMessage = "External registration URL must be a valid, absolute URL.")]
        public string? ExternalRegistrationUrl { get; set; }
    }
}
```

**New `CreateTournament.razor.css`** (mirrors `CreateSponsor.razor.css`):

```css
.create-tournament-section-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--neba-gray-800, #262626);
    border-bottom: 1px solid var(--neba-gray-200, #E5E5E5);
    padding-bottom: 0.5rem;
}
```

**No `TournamentTypeOptions.cs`** — unlike `SponsorCategoryOptions.cs` (which hand-maintains a duplicate list), the tournament-type dropdown fetches `ITournamentsApi.ListTournamentTypesAsync()` in `CreateTournament.razor.OnInitializedAsync` instead, so a new `TournamentType` only ever requires a domain-layer change (see Phase 1's `ListTournamentTypes`). This deliberately does *not* match the `SponsorCategoryOptions` precedent — that hardcoded list is a pre-existing shortcut in the codebase, not a pattern to extend.

**Existing `Tournaments/Schedule/Tournaments.razor`** (edit) — add `@using Neba.Api.Contracts.Security` to the top if not already present, and insert immediately after the closing `</div>` of `.tournaments-page` (before `@code {`):

```razor
<AuthorizeView Policy="@Permissions.CreateTournament.PolicyName">
    <Authorized>
        <FabCreateButton Href="/tournaments/new" Label="Create Tournament" />
    </Authorized>
</AuthorizeView>
```

### Components (`src/Neba.Website.Server/Tournaments/`)

**New `OilPatternSelection.cs`** — the payload `OilPatternPicker` emits; exactly one of `OilPatternId` or the two category fields is populated, matching the mutual-exclusivity the API validator enforces:

```csharp
namespace Neba.Website.Server.Tournaments;

internal sealed record OilPatternSelection
{
    public string? OilPatternId { get; init; }

    public string? PatternLengthCategory { get; init; }

    public string? PatternRatioCategory { get; init; }
}
```

**New `OilPatternCategoryOptions.cs`**:

```csharp
namespace Neba.Website.Server.Tournaments;

internal static class OilPatternCategoryOptions
{
    public static readonly IReadOnlyList<string> LengthCategories = ["Short", "Medium", "Long"];

    public static readonly IReadOnlyList<string> RatioCategories = ["Sport", "Challenge", "Recreation"];
}
```

**New `OilPatternPicker.razor`** — the one genuinely new piece of UI flow. Its selects are plain HTML `<select>`/`<input>` with `@bind`, not Blazor `InputSelect`/`InputText`, since it isn't part of the parent's `EditContext` (no `DataAnnotationsValidator` coverage needed here — the API already validates):

```razor
@using ErrorOr
@using Neba.Api.Contracts.OilPatterns
@using Neba.Api.Contracts.OilPatterns.CreateOilPattern
@using Neba.Api.Contracts.OilPatterns.ListOilPatterns
@using Neba.Website.Server.Services

@inject ApiExecutor ApiExecutor
@inject IOilPatternsApi OilPatternsApi

<div class="neba-space-y-4">
    <div class="neba-segmented-control" role="tablist" aria-label="Oil pattern mode">
        <button type="button" class="neba-segment-button @(_mode == OilPatternMode.None ? "neba-segment-selected" : null)"
                @onclick="@(() => SetMode(OilPatternMode.None))">No Pattern</button>
        <button type="button" class="neba-segment-button @(_mode == OilPatternMode.Pick ? "neba-segment-selected" : null)"
                @onclick="@(() => SetMode(OilPatternMode.Pick))">Pick Existing</button>
        <button type="button" class="neba-segment-button @(_mode == OilPatternMode.Create ? "neba-segment-selected" : null)"
                @onclick="@(() => SetMode(OilPatternMode.Create))">Create New</button>
    </div>

    @if (_mode == OilPatternMode.None)
    {
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
                <label for="manual-length-category" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Length Category</label>
                <select id="manual-length-category" class="neba-select" @bind="_manualLengthCategory" @bind:after="EmitSelectionChanged">
                    <option value="">Not specified</option>
                    @foreach (var category in OilPatternCategoryOptions.LengthCategories)
                    {
                        <option value="@category">@category</option>
                    }
                </select>
            </div>
            <div>
                <label for="manual-ratio-category" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Ratio Category</label>
                <select id="manual-ratio-category" class="neba-select" @bind="_manualRatioCategory" @bind:after="EmitSelectionChanged">
                    <option value="">Not specified</option>
                    @foreach (var category in OilPatternCategoryOptions.RatioCategories)
                    {
                        <option value="@category">@category</option>
                    }
                </select>
            </div>
        </div>
    }
    else if (_mode == OilPatternMode.Pick)
    {
        <div>
            <label for="pattern-select" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Existing Pattern</label>
            <select id="pattern-select" class="neba-select" @bind="_selectedOilPatternId" @bind:after="EmitSelectionChanged">
                <option value="">Select a pattern…</option>
                @foreach (var pattern in _patterns)
                {
                    <option value="@pattern.OilPatternId">@pattern.Name — @pattern.Length ft</option>
                }
            </select>

            @if (_selectedPattern is not null)
            {
                <div class="flex items-center gap-3 mt-3 p-3 rounded" style="background: var(--neba-blue-100); border: 1px solid var(--neba-blue-200);">
                    <span class="font-semibold text-sm" style="color: var(--neba-blue-brand);">@_selectedPattern.Name</span>
                    <span class="text-sm text-[var(--neba-gray-600)]">@_selectedPattern.Length ft, ratio @_selectedPattern.LeftRatio.ToString("0.0")/@_selectedPattern.RightRatio.ToString("0.0")</span>
                    <span class="neba-badge neba-badge-primary">@_selectedPattern.LengthCategory</span>
                    <span class="neba-badge neba-badge-primary">@_selectedPattern.RatioCategory</span>
                </div>
            }
        </div>
    }
    else
    {
        <div class="neba-space-y-4 p-4 rounded" style="border: 1px dashed var(--neba-gray-300); background: var(--neba-gray-050);">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                    <label for="new-pattern-name" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Name</label>
                    <input id="new-pattern-name" class="neba-input" @bind="_newPattern.Name" placeholder="e.g. Typhoon" />
                </div>
                <div>
                    <label for="new-pattern-kegel" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Kegel ID (optional)</label>
                    <input id="new-pattern-kegel" class="neba-input" @bind="_newPattern.KegelId" placeholder="Kegel pattern library ID" />
                </div>
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div>
                    <label for="new-pattern-length" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Length (ft)</label>
                    <input id="new-pattern-length" type="number" min="1" class="neba-input" @bind="_newPattern.Length" />
                </div>
                <div>
                    <label for="new-pattern-volume" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Volume (mL)</label>
                    <input id="new-pattern-volume" type="number" min="0" step="0.1" class="neba-input" @bind="_newPattern.Volume" />
                </div>
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                    <label for="new-pattern-left" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Left Ratio</label>
                    <input id="new-pattern-left" type="number" min="0" step="0.1" class="neba-input" @bind="_newPattern.LeftRatio" />
                </div>
                <div>
                    <label for="new-pattern-right" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Right Ratio</label>
                    <input id="new-pattern-right" type="number" min="0" step="0.1" class="neba-input" @bind="_newPattern.RightRatio" />
                </div>
            </div>

            @if (!string.IsNullOrWhiteSpace(_createErrorMessage))
            {
                <p class="text-sm text-red-600">@_createErrorMessage</p>
            }

            <button type="button" class="neba-btn neba-btn-primary neba-btn-sm" @onclick="AddNewPatternAsync" disabled="@_isCreatingPattern">
                @(_isCreatingPattern ? "Adding…" : "Add Pattern")
            </button>
            <p class="text-sm text-[var(--neba-gray-500)]">Adding a pattern saves it to NEBA's reusable pattern library immediately, then selects it here.</p>
        </div>
    }
</div>

@code {
    private enum OilPatternMode { None, Pick, Create }

    [Parameter]
    public EventCallback<OilPatternSelection> SelectionChanged { get; set; }

    private OilPatternMode _mode = OilPatternMode.None;
    private IReadOnlyCollection<OilPatternSummaryResponse> _patterns = [];
    private string? _selectedOilPatternId;
    private string _manualLengthCategory = "";
    private string _manualRatioCategory = "";
    private NewOilPatternModel _newPattern = new();
    private bool _isCreatingPattern;
    private string? _createErrorMessage;

    private OilPatternSummaryResponse? _selectedPattern
        => _patterns.FirstOrDefault(p => p.OilPatternId == _selectedOilPatternId);

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync(
            "OilPatterns",
            "ListOilPatterns",
            OilPatternsApi.ListOilPatternsAsync);

        if (!result.IsError)
        {
            _patterns = result.Value.Items;
        }
    }

    private void SetMode(OilPatternMode mode)
    {
        _mode = mode;
        _ = EmitSelectionChanged();
    }

    private Task EmitSelectionChanged()
    {
        var selection = _mode switch
        {
            OilPatternMode.Pick => new OilPatternSelection { OilPatternId = _selectedOilPatternId },
            OilPatternMode.None => new OilPatternSelection
            {
                PatternLengthCategory = string.IsNullOrWhiteSpace(_manualLengthCategory) ? null : _manualLengthCategory,
                PatternRatioCategory = string.IsNullOrWhiteSpace(_manualRatioCategory) ? null : _manualRatioCategory
            },
            _ => new OilPatternSelection()
        };

        return SelectionChanged.InvokeAsync(selection);
    }

    private async Task AddNewPatternAsync()
    {
        _isCreatingPattern = true;
        _createErrorMessage = null;

        var request = new CreateOilPatternRequest
        {
            Name = _newPattern.Name,
            Length = _newPattern.Length,
            Volume = _newPattern.Volume,
            LeftRatio = _newPattern.LeftRatio,
            RightRatio = _newPattern.RightRatio,
            KegelId = Guid.TryParse(_newPattern.KegelId, out var kegelId) ? kegelId : null
        };

        var result = await ApiExecutor.ExecuteAsync(
            "OilPatterns",
            "CreateOilPattern",
            ct => OilPatternsApi.CreateOilPatternAsync(request, ct));

        _isCreatingPattern = false;

        if (result.IsError)
        {
            _createErrorMessage = result.FirstError.Description;
            return;
        }

        _patterns = [.. _patterns, new OilPatternSummaryResponse
        {
            OilPatternId = result.Value.OilPatternId,
            Name = result.Value.Name,
            Length = result.Value.Length,
            Volume = _newPattern.Volume,
            LeftRatio = _newPattern.LeftRatio,
            RightRatio = _newPattern.RightRatio,
            KegelId = request.KegelId,
            LengthCategory = result.Value.LengthCategory,
            RatioCategory = result.Value.RatioCategory
        }];

        _selectedOilPatternId = result.Value.OilPatternId;
        _newPattern = new NewOilPatternModel();
        _mode = OilPatternMode.Pick;

        await EmitSelectionChanged();
    }

    private sealed class NewOilPatternModel
    {
        public string Name { get; set; } = string.Empty;
        public int Length { get; set; } = 40;
        public decimal Volume { get; set; } = 24;
        public decimal LeftRatio { get; set; } = 5;
        public decimal RightRatio { get; set; } = 5;
        public string? KegelId { get; set; }
    }
}
```

### Mockups

- [`docs/plans/mockups/create-tournament/create-tournament.html`](mockups/create-tournament/create-tournament.html) — single mockup (data-capture page, per the mockup-scoping rule: no layout tradeoff worth comparing for a form). Uses the real theme tokens/classes from `neba_theme.css`/`app.css` (page-title-bar gradient, `neba-card`, `neba-segmented-control`, `neba-file-upload-*`, badges) so it reads as this app, not a generic form. Simulates with inline JS: the oil-pattern 3-way mode switch, live category-badge preview when picking an existing pattern (math mirrors the real `FromLength`/`FromRatio` thresholds), the "Create New" pattern flow appending to and auto-selecting in the picker, a real client-side logo thumbnail preview via `FileReader`, and a mock success banner on submit.

### API Client

No new contract work here beyond Phase 1 — `ITournamentsApi.CreateTournamentAsync`/`UploadTournamentLogoAsync` and `IOilPatternsApi.CreateOilPatternAsync`/`ListOilPatternsAsync` already exist from Phase 1 (the latter two on their own top-level `IOilPatternsApi`, per the `/oil-patterns` routing correction — see Phase 1). `IBowlingCentersApi.ListBowlingCentersAsync()` already exists too (used for the venue `<select>`, first Blazor consumer of that endpoint).

`src/Neba.Website.Server/Services/ApiServicesConfiguration.cs` (edit) — add `services.RegisterApiEndpoint<IOilPatternsApi>();` alongside the existing `RegisterApiEndpoint<ITournamentsApi>()`/`RegisterApiEndpoint<IBowlingCentersApi>()` calls.

### State / Dirty-Tracking

- `DirtyFormGuard IsDirty="@_isDirty"` wraps the page.
- `_editContext.OnFieldChanged` (wired in the constructor, unwired in `DisposeAsync`) covers every `InputSelect`/`InputText`/`InputNumber`/`InputDate`/`InputCheckbox` bound to `_model` — `Name`, `TournamentType`, dates, `StatsEligible`, `EntryFee`, `BowlingCenterCertificationNumber`, `ExternalRegistrationUrl`.
- `OilPatternPicker` isn't wired through the parent `EditContext` at all (its `<select>`/`<input>` elements are plain HTML, not `InputBase` descendants) — `HandleOilPatternSelectionChanged` calls `MarkDirty()` directly whenever `SelectionChanged` fires.
- `FileUpload`'s `OnFileUploaded`/`OnFileRemoved` callbacks call `MarkDirty()` directly, same as `CreateSponsor.razor`.

### `<PageTitle>` / Render Mode

- `<PageTitle>Create Tournament - BowlNEBA</PageTitle>`.
- `@rendermode InteractiveServer` — matches `CreateSponsor.razor` (has async reference-data loading in `OnInitializedAsync` but still uses plain `InteractiveServer`, not the no-prerender variant, since prerender there doesn't produce a visible flash-of-empty-content problem in practice for that page).

### Tests

- **bUnit** (`tests/Neba.Website.Tests/Tournaments/`): `CreateTournamentComponentTests` — required-field validation surfaces `ValidationMessage`s; submit is disabled while `_isSubmitting`/`_isLogoUploading`; `HandleCreateAsync` maps form model → `TournamentInput` correctly (mirrors `CreateSponsorComponentTests` if it exists, or `CreateArticle`'s bUnit pattern otherwise). `OilPatternPickerComponentTests` — mode switching hides/shows the right sub-sections; picking an existing pattern surfaces its derived categories read-only; "Create new" calls `CreateOilPatternAsync` and transitions to "Pick existing" with the new pattern selected; manual-categories mode and existing-pattern mode are mutually exclusive in the emitted `OilPatternSelection`.
- **Playwright** (`tests/e2e/`): one real end-to-end flow — navigate to `/tournaments/new` as an authorized user, fill the form picking an existing oil pattern, submit, assert redirect to the new tournament's detail page and that its lane-condition categories match the picked pattern. A second, shorter Playwright case (or bUnit, if it doesn't need a real HTTP round-trip) covering the "create new pattern inline" sub-flow specifically, since that's the one interaction genuinely novel to this feature (matching the `new-endpoint`/`pull-request-prep` decision table: bUnit for internal component logic, Playwright for real browser + HTTP flows).

## One Last Thing

we need the ability to now show the oil pattern details as of a certain date/time.  this is a nullable field.  if it is null and/or the user
has tournament management permissions, we need to show the full oil pattern details.  if the date isn't null, and we haven't hit the datetime yet, we only
show the length and ratio cateories if we have them.  if after the datetime, we show the full info.  this is on all views of the tournament as it isn't public
knowledge before that date.  we need to add this column to the tournament record as a nullable datetime.  we might need to have something around cache clearing so
that when the oil pattern reveal date has passed, it clears tournament list/detail cache so that it will show the datetime.  i'd like to explore options for this
the field on the ui should be in the oil pattern section, and should be there for all 3 views because regardless if we know the pattern at time of creation, we will most likely
know when we want it revealed