# Create Sponsor

Add a "Create Sponsor" feature, structurally mirroring `CreateArticle` (News). GET/List sponsors are already wired up (`GetSponsorDetail`, `ListActiveSponsors`); this plan adds the write side.

## Decisions locked in during scoping

- **UI entry point**: new admin-gated sponsor list page (`/sponsors/manage` — see Phase 2) with a `FabCreateButton` → `/sponsors/new`, mirroring `NewsList.razor`. The existing public `/sponsors` page (tiered marketing display) is untouched.
- **Admin list data source**: no new API route. The existing `GET /sponsors` (`ListActiveSponsorsEndpoint`) is amended to check the caller's permissions and branch its filter, exactly like `ListArticlesEndpoint`/`ListArticlesQuery` does with `CallerHasArticleManagementPermission` — anonymous/unpermitted callers still get active-only sponsors (today's public-page behavior, unchanged), callers holding a Sponsors-management permission get every sponsor, active and inactive. This replaces the separate `ListSponsorsForAdmin` endpoint from the earlier draft of this plan.
- **Form/factory scope**: `Sponsor.Create(...)` takes the mandatory fields as required parameters and every other `Sponsor` property as a nullable parameter defaulting to `null`, matching the existing `SponsorFactory.Create(...)` test-factory signature (`tests/Neba.TestFactory/Sponsors/SponsorFactory.cs`) — that signature is the reference for the domain factory's shape. The create UI form captures the full field set (not deferred to a future Edit Sponsor feature).
- **Slug uniqueness**: enforced with the same check-then-insert + `Error.Conflict` (409) pattern `CreateArticleCommandHandler` uses for `Article.Slug`, since `Sponsor.Slug` already has a DB alternate key (`SponsorConfiguration.cs`) that would otherwise surface as an unhandled `DbUpdateException`.
- **Business address input (UI)**: manual entry now (`UsState` dropdown + free-text street/unit/city/postal code), no address-autocomplete integration. The app has zero existing Google Maps/Places dependency anywhere (`DirectionsModal.razor` only builds a deep-link URL, no SDK/API key) — adding Places Autocomplete would be the first such dependency (new API key, billing, CSP change, JS interop). Worth it long-term, though — members will eventually be able to update their own address (a second, higher-volume address-entry form), which is exactly the case that justifies the shared integration cost. Tracked as a GitHub issue rather than scoped into this feature: [`docs/plans/address-autocomplete-issue.md`](./address-autocomplete-issue.md).

## Open assumptions to confirm at this gate

1. **Address input is US-only** for now (`Address.Create(street, unit, city, UsState, zip, coordinates)` overload), consistent with `AddressFactory.BogusUs` being the only address bogus-generator sponsors currently use. No Canadian address input in the create form.
2. **Business email/phone/address/contact validation happens in the command handler**, not the FluentValidation request validator — `Address.Create`, `EmailAddress.Create`, and `PhoneNumber.CreateNorthAmerican` all return `ErrorOr<T>` and encode business rules (regex formats, area-code rules), which per CLAUDE.md's "Validators handle structural validation only" belong in the handler/domain, not the validator. This is a new pattern for this codebase (no existing command handler calls these three `Create` methods yet) — flagging in case there's a preferred existing approach I've missed.
3. **`SponsorContact` (contact person) is all-or-nothing**: if the request supplies a contact name, phone, or email, all three must be present or the request is rejected as a validation error; otherwise `SponsorContact` stays `null`. No partial contact info.
4. **No `Priority` range validation** beyond what the type system gives (`int`, defaults to `0` if omitted) — no domain rule for priority was specified for `Sponsor`, so none is added. Flag if this needs a `> 0` constraint.
5. **Reserved slug "new"** — like `Article`, the sponsor create route is `/sponsors/new`, so `Sponsor.Create` rejects a normalized slug of `"new"` the same way `Article.Create` does, to keep the route unambiguous.

---

## Phase 1: API

### Domain (`Neba.Api.Features.Sponsors.Domain`)

**New — `src/Neba.Api/Domain/SlugNormalizer.cs`** (shared helper, extracted from `Article.NormalizeSlug` so `Sponsor` doesn't duplicate the character-filtering logic):

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Neba.Api.Domain;

/// <summary>
/// Normalizes a title or a staff-supplied slug override into a URL-safe slug: lowercase,
/// alphanumeric runs joined by single hyphens, no leading/trailing hyphen.
/// </summary>
internal static class SlugNormalizer
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Slugs are URL-facing and must be lowercase, not normalized for security comparisons.")]
    public static string Normalize(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = false;

        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }
}
```

**Edit — `src/Neba.Api/Features/News/Domain/Article.cs`** — remove the private `NormalizeSlug` method and its `using System.Text;`/`using System.Diagnostics.CodeAnalysis;` if now unused, and call the shared helper instead:

```csharp
var normalizedSlug = SlugNormalizer.Normalize(string.IsNullOrEmpty(slug)
    ? title
    : slug);
```

(add `using Neba.Api.Domain;` — already present in `Article.cs`)

**Edit — `src/Neba.Api/Features/Sponsors/Domain/Sponsor.cs`** — add the reserved-slug constant and static factory:

```csharp
using System.Diagnostics.CodeAnalysis;

using ErrorOr;

using Neba.Api.Contacts.Domain;
using Neba.Api.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Sponsors.Domain;

public sealed class Sponsor
    : AggregateRoot
{
    // ...existing properties unchanged...

    private const string ReservedSlugNew = "new";

    /// <summary>
    /// Creates a new sponsor. If <paramref name="slug"/> is null or empty, the slug is generated from
    /// <paramref name="name"/>. Returns a validation error if <paramref name="name"/> is empty, the
    /// normalized slug has no alphanumeric characters, or the normalized slug is the reserved value
    /// "new" (reserved for the <c>/sponsors/new</c> create route). <paramref name="id"/> is
    /// production-optional — it exists so test factories can assign a deterministic ID for stable
    /// Verify snapshots; production callers always omit it.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Aggregate factory method — each parameter is a required or optional field of the always-valid Sponsor invariant (see CLAUDE.md 'Always-Valid Entities'); splitting into a parameter object would just move the same fields into a second type with no behavior of its own.")]
    public static ErrorOr<Sponsor> Create(
        string name,
        bool isCurrentSponsor,
        int priority,
        SponsorTier tier,
        SponsorCategory category,
        string? slug = null,
        StoredFile? logo = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        Address? businessAddress = null,
        EmailAddress? businessEmail = null,
        IReadOnlyCollection<PhoneNumber>? phoneNumbers = null,
        ContactInfo? sponsorContact = null,
        SponsorId? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SponsorErrors.NameRequired;
        }

        var normalizedSlug = SlugNormalizer.Normalize(string.IsNullOrEmpty(slug)
            ? name
            : slug);

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return SponsorErrors.SlugInvalid;
        }

        if (normalizedSlug == ReservedSlugNew)
        {
            return SponsorErrors.SlugReserved;
        }

        return new Sponsor
        {
            Id = id ?? SponsorId.New(),
            Name = name,
            Slug = normalizedSlug,
            IsCurrentSponsor = isCurrentSponsor,
            Priority = priority,
            Tier = tier,
            Category = category,
            Logo = logo,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
            Description = description,
            LiveReadText = liveReadText,
            PromotionalNotes = promotionalNotes,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl,
            BusinessAddress = businessAddress,
            BusinessEmail = businessEmail,
            PhoneNumbers = phoneNumbers ?? [],
            SponsorContact = sponsorContact
        };
    }
}
```

**Edit — `src/Neba.Api/Features/Sponsors/SponsorErrors.cs`**:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Sponsors;

internal static class SponsorErrors
{
    public static Error SponsorNotFound(string slug)
        => Error.NotFound(
            code: "Sponsor.NotFound",
            description: "Sponsor not found.",
            metadata: new() { { "slug", slug } });

    public static Error NameRequired
        => Error.Validation("Sponsor.Name.Required", "Name must not be empty.");

    public static Error SlugInvalid
        => Error.Validation("Sponsor.Slug.Invalid", "Slug must contain at least one alphanumeric character.");

    public static Error SlugReserved
        => Error.Validation("Sponsor.Slug.Reserved", "Slug 'new' is reserved for the sponsor-creation route.");

    public static Error SlugAlreadyExists(string slug)
        => Error.Conflict(
            code: "Sponsor.Slug.AlreadyExists",
            description: "A sponsor with this slug already exists.",
            metadata: new Dictionary<string, object> { { "Slug", slug } });
}
```

### Application (`Neba.Api.Features.Sponsors.CreateSponsor/`, new folder)

**New — `CreateSponsorPhoneNumberInput.cs`** (raw phone entry carried on the command, mirrors `NewArticleAttachment`'s role as a command-level nested type):

```csharp
using Neba.Api.Contacts.Domain;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed record CreateSponsorPhoneNumberInput
{
    public required PhoneNumberType Type { get; init; }

    public required string Number { get; init; }

    public string? Extension { get; init; }
}
```

**New — `CreateSponsorCommand.cs`**:

```csharp
using Neba.Api.Contacts.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed record CreateSponsorCommand
    : ICommand<CreatedSponsor>
{
    public required string Name { get; init; }

    public string? Slug { get; init; }

    public required bool IsCurrentSponsor { get; init; }

    public required int Priority { get; init; }

    public required SponsorTier Tier { get; init; }

    public required SponsorCategory Category { get; init; }

    public StoredFile? Logo { get; init; }

    public Uri? WebsiteUrl { get; init; }

    public string? TagPhrase { get; init; }

    public string? Description { get; init; }

    public string? LiveReadText { get; init; }

    public string? PromotionalNotes { get; init; }

    public Uri? FacebookUrl { get; init; }

    public Uri? InstagramUrl { get; init; }

    public string? BusinessStreet { get; init; }

    public string? BusinessUnit { get; init; }

    public string? BusinessCity { get; init; }

    public UsState? BusinessState { get; init; }

    public string? BusinessPostalCode { get; init; }

    public string? BusinessEmailAddress { get; init; }

    public IReadOnlyCollection<CreateSponsorPhoneNumberInput> PhoneNumbers { get; init; } = [];

    public string? ContactName { get; init; }

    public PhoneNumberType? ContactPhoneType { get; init; }

    public string? ContactPhoneNumber { get; init; }

    public string? ContactPhoneExtension { get; init; }

    public string? ContactEmail { get; init; }
}
```

**New — `CreatedSponsor.cs`**:

```csharp
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

/// <summary>
/// Result of successfully creating a sponsor, including its identifier and normalized slug.
/// </summary>
public sealed record CreatedSponsor
{
    /// <summary>
    /// The unique identifier of the newly created sponsor.
    /// </summary>
    public required SponsorId Id { get; init; }

    /// <summary>
    /// The normalized slug assigned to the newly created sponsor.
    /// </summary>
    public required string Slug { get; init; }
}
```

**New — `CreateSponsorCommandHandler.cs`**:

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contacts.Domain;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache)
    : ICommandHandler<CreateSponsorCommand, CreatedSponsor>
{
    public async Task<ErrorOr<CreatedSponsor>> HandleAsync(CreateSponsorCommand command, CancellationToken cancellationToken)
    {
        var addressResult = BuildBusinessAddress(command);

        if (addressResult.IsError)
        {
            return addressResult.Errors;
        }

        var emailResult = BuildBusinessEmail(command.BusinessEmailAddress);

        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        var phoneNumbersResult = BuildPhoneNumbers(command.PhoneNumbers);

        if (phoneNumbersResult.IsError)
        {
            return phoneNumbersResult.Errors;
        }

        var contactResult = BuildSponsorContact(command);

        if (contactResult.IsError)
        {
            return contactResult.Errors;
        }

        var sponsorResult = Sponsor.Create(
            command.Name,
            command.IsCurrentSponsor,
            command.Priority,
            command.Tier,
            command.Category,
            command.Slug,
            command.Logo,
            command.WebsiteUrl,
            command.TagPhrase,
            command.Description,
            command.LiveReadText,
            command.PromotionalNotes,
            command.FacebookUrl,
            command.InstagramUrl,
            addressResult.Value,
            emailResult.Value,
            phoneNumbersResult.Value,
            contactResult.Value);

        if (sponsorResult.IsError)
        {
            return sponsorResult.Errors;
        }

        var sponsor = sponsorResult.Value;

        var slugCheck = await EnsureSlugIsAvailableAsync(sponsor.Slug, cancellationToken);

        if (slugCheck.IsError)
        {
            return slugCheck.Errors;
        }

        await appDbContext.Sponsors.AddAsync(sponsor, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:sponsors", token: cancellationToken);

        return new CreatedSponsor
        {
            Id = sponsor.Id,
            Slug = sponsor.Slug
        };
    }

    private static ErrorOr<Address?> BuildBusinessAddress(CreateSponsorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.BusinessStreet))
        {
            return (Address?)null;
        }

        ArgumentNullException.ThrowIfNull(command.BusinessState);

        var result = Address.Create(
            command.BusinessStreet,
            command.BusinessUnit,
            command.BusinessCity ?? string.Empty,
            command.BusinessState,
            command.BusinessPostalCode ?? string.Empty);

        return result.IsError
            ? ErrorOr<Address?>.From(result.Errors)
            : result.Value;
    }

    private static ErrorOr<EmailAddress?> BuildBusinessEmail(string? businessEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(businessEmailAddress))
        {
            return (EmailAddress?)null;
        }

        var result = EmailAddress.Create(businessEmailAddress);

        return result.IsError
            ? ErrorOr<EmailAddress?>.From(result.Errors)
            : result.Value;
    }

    private static ErrorOr<IReadOnlyCollection<PhoneNumber>> BuildPhoneNumbers(
        IReadOnlyCollection<CreateSponsorPhoneNumberInput> phoneNumbers)
    {
        var built = new List<PhoneNumber>(phoneNumbers.Count);

        foreach (var phoneNumber in phoneNumbers)
        {
            var result = PhoneNumber.CreateNorthAmerican(phoneNumber.Type, phoneNumber.Number, phoneNumber.Extension);

            if (result.IsError)
            {
                return result.Errors;
            }

            built.Add(result.Value);
        }

        return built;
    }

    // All-or-nothing per scoping decision: if any of Name/Phone/Email is supplied, all three must be.
    private static ErrorOr<ContactInfo?> BuildSponsorContact(CreateSponsorCommand command)
    {
        var anySupplied = !string.IsNullOrWhiteSpace(command.ContactName)
            || !string.IsNullOrWhiteSpace(command.ContactPhoneNumber)
            || !string.IsNullOrWhiteSpace(command.ContactEmail);

        if (!anySupplied)
        {
            return (ContactInfo?)null;
        }

        ArgumentNullException.ThrowIfNull(command.ContactPhoneType);

        var phoneResult = PhoneNumber.CreateNorthAmerican(
            command.ContactPhoneType.Value,
            command.ContactPhoneNumber ?? string.Empty,
            command.ContactPhoneExtension);

        if (phoneResult.IsError)
        {
            return phoneResult.Errors;
        }

        var emailResult = EmailAddress.Create(command.ContactEmail ?? string.Empty);

        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        return new ContactInfo
        {
            Name = command.ContactName ?? string.Empty,
            Phone = phoneResult.Value,
            Email = emailResult.Value
        };
    }

    // Check-then-insert: see CreateArticleCommandHandler.EnsureSlugIsAvailableAsync for the same
    // caveat about a theoretical concurrent-insert race — not worth a retry path at current volume.
    private async Task<ErrorOr<Success>> EnsureSlugIsAvailableAsync(string slug, CancellationToken cancellationToken)
    {
        var slugExists = await appDbContext.Sponsors.AnyAsync(s => s.Slug == slug, cancellationToken);

        return slugExists
            ? SponsorErrors.SlugAlreadyExists(slug)
            : Result.Success;
    }
}
```

> **Note for the code-draft review**: `ErrorOr<T?>` doesn't have a built-in implicit conversion from `List<Error>` the same way `ErrorOr<T>` does for non-nullable `T` in this codebase's ErrorOr version — confirm `ErrorOr<Address?>.From(result.Errors)` compiles as written against the installed ErrorOr package version when you implement this; if it doesn't, the fallback is a private nullable wrapper record (`{ Address? Value }`) returned as `ErrorOr<Wrapper>`, or simply inlining the three build steps directly in `HandleAsync` instead of extracting them into `Build*` helper methods (each returning `ErrorOr<Success>` and stashing the built value in a local `Address? address = null;` closed-over variable, matching `CreateArticleCommandHandler`'s flatter style).

### Application (amendment) — `Features/Sponsors/ListActiveSponsors/`

**Edit — `ListActiveSponsorsQuery.cs`**:

```csharp
using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Sponsors.ListActiveSponsors;

internal sealed record ListActiveSponsorsQuery
    : ICachedQuery<IReadOnlyCollection<SponsorSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Sponsors.ListActiveSponsors(CallerHasSponsorManagementPermission);

    public TimeSpan Expiry
        => TimeSpan.FromDays(30);

    public required bool CallerHasSponsorManagementPermission { get; init; }
}
```

**Edit — `src/Neba.Api/Caching/CacheDescriptors.cs`** (replace the `Sponsors.ListActiveSponsors` property with a method):

```csharp
public static CacheDescriptor ListActiveSponsors(bool callerHasSponsorManagementPermission)
    => new()
    {
        Key = $"neba:sponsors:list:scope:{(callerHasSponsorManagementPermission ? "management" : "public")}",
        Tags = ["neba", "neba:sponsors"]
    };
```

**Edit — `ListActiveSponsorsQueryHandler.cs`** (wrap the existing filter):

```csharp
public async Task<IReadOnlyCollection<SponsorSummaryDto>> HandleAsync(ListActiveSponsorsQuery query, CancellationToken cancellationToken)
{
    var sponsors = query.CallerHasSponsorManagementPermission
        ? _sponsors
        : _sponsors.Where(sponsor => sponsor.IsCurrentSponsor);

    var rows = await sponsors
        .Select(sponsor => new
        {
            // ...unchanged projection...
        })
        .ToListAsync(cancellationToken);

    // ...unchanged mapping to SponsorSummaryDto...
}
```

**Edit — `ListActiveSponsorsEndpoint.cs`**:

```csharp
using Neba.Api.Contracts.Security;

using PermissionsScope = Neba.Api.Contracts.Security.Permissions;

// ...

public override async Task HandleAsync(CancellationToken ct)
{
    var query = new ListActiveSponsorsQuery
    {
        CallerHasSponsorManagementPermission = User.HasAnyPermission(PermissionsScope.SponsorManagementPermissions)
    };

    var result = await _queryHandler.HandleAsync(query, ct);

    // ...unchanged response mapping...
}
```

**Naming flag (unresolved)**: keeping `ListActiveSponsorsEndpoint`/`ListActiveSponsorsQuery` as-is for this plan (minimal diff) even though "active" is no longer fully accurate. Rename to `ListSponsors*` is a larger, separate diff — call it out before implementing if you'd rather do it now.

### Security (amendment) — `src/Neba.Api.Contracts/Security/Permission.cs`

```csharp
#region Sponsors

/// <summary>
/// Permission to create a sponsor.
/// </summary>
public static readonly Permissions CreateSponsor = new("Sponsors.CreateSponsor", "Create Sponsor");

/// <summary>
/// A collection of permissions related to sponsor management.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> SponsorManagementPermissions =
[
    CreateSponsor,
];

#endregion
```

No `AddPolicy(...)` registration needed — the dynamic `Permission:{value}` policy provider handles it automatically, same as `CreateArticle`. No `docs/policies/README.md` entry needed (generic `Permission:{value}` row already documents the mechanism).

### Infrastructure

No new EF configuration or migration needed — `SponsorConfiguration.cs` already maps every field `Sponsor.Create` populates.

### API (`Neba.Api.Features.Sponsors.CreateSponsor/`)

**New — `CreateSponsorEndpoint.cs`**:

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorEndpoint(Messaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor> commandHandler)
    : Endpoint<CreateSponsorRequest, SponsorResponse>
{
    private readonly Messaging.ICommandHandler<CreateSponsorCommand, CreatedSponsor> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<SponsorsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateSponsor.PolicyName);

        Description(description => description
            .WithName("CreateSponsor")
            .WithTags("Admin")
            .Produces<SponsorResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateSponsorRequest req, CancellationToken ct)
    {
        var input = req.Sponsor;

        var command = new CreateSponsorCommand
        {
            Name = input.Name,
            Slug = input.Slug,
            IsCurrentSponsor = input.IsCurrentSponsor,
            Priority = input.Priority,
            Tier = SponsorTier.FromName(input.Tier),
            Category = SponsorCategory.FromName(input.Category),
            Logo = input.Logo is null
                ? null
                : new StoredFile
                {
                    Container = input.Logo.Container,
                    Path = input.Logo.Path,
                    ContentType = input.Logo.ContentType,
                    SizeInBytes = input.Logo.SizeInBytes
                },
            WebsiteUrl = input.WebsiteUrl,
            TagPhrase = input.TagPhrase,
            Description = input.Description,
            LiveReadText = input.LiveReadText,
            PromotionalNotes = input.PromotionalNotes,
            FacebookUrl = input.FacebookUrl,
            InstagramUrl = input.InstagramUrl,
            BusinessStreet = input.BusinessStreet,
            BusinessUnit = input.BusinessUnit,
            BusinessCity = input.BusinessCity,
            BusinessState = string.IsNullOrWhiteSpace(input.BusinessState)
                ? null
                : UsState.FromValue(input.BusinessState),
            BusinessPostalCode = input.BusinessPostalCode,
            BusinessEmailAddress = input.BusinessEmailAddress,
            PhoneNumbers = [.. input.PhoneNumbers.Select(p => new CreateSponsorPhoneNumberInput
            {
                Type = PhoneNumberType.FromValue(p.PhoneNumberType),
                Number = p.PhoneNumber,
                Extension = p.Extension
            })],
            ContactName = input.Contact?.Name,
            ContactPhoneType = input.Contact is null
                ? null
                : PhoneNumberType.FromValue(input.Contact.PhoneNumberType),
            ContactPhoneNumber = input.Contact?.PhoneNumber,
            ContactPhoneExtension = input.Contact?.Extension,
            ContactEmail = input.Contact?.Email
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

        var response = new SponsorResponse
        {
            SponsorId = result.Value.Id.Value.ToString(),
            Slug = result.Value.Slug
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetSponsorDetail",
            routeValues: new { slug = result.Value.Slug },
            responseBody: response,
            cancellation: ct);
    }
}
```

**New — `CreateSponsorSummary.cs`**:

```csharp
using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorSummary : Summary<CreateSponsorEndpoint>
{
    public CreateSponsorSummary()
    {
        Summary = "Creates a sponsor.";
        Description = "Creates a sponsor with its full field set (tier, category, contact, business address, etc). Slug is derived from the name unless a staff-supplied override is given; either way it is normalized and must be unique. Requires the Sponsors.CreateSponsor permission.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(201, "Sponsor created.",
            contentType: MediaTypeNames.Application.Json,
            example: new SponsorResponse
            {
                SponsorId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                Slug = "storm-products-inc"
            });
#pragma warning restore S1075 // URIs should not be hardcoded

        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Sponsors.CreateSponsor permission.");
        Response(409, "Slug already taken.");
        Response(422, "Name, slug, tier, category, or a contact/address/email/phone field failed a domain validation rule.");
    }
}
```

**New — `CreateSponsorRequestValidator.cs`**:

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.Api.Features.Sponsors.CreateSponsor;

internal sealed class CreateSponsorRequestValidator
    : Validator<CreateSponsorRequest>
{
    public CreateSponsorRequestValidator()
    {
        RuleFor(r => r.Sponsor.Name)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.NameRequired")
            .WithMessage("Name is required.")
            .MaximumLength(63)
            .WithErrorCode("CreateSponsorRequest.NameTooLong")
            .WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Sponsor.Slug)
            .MaximumLength(63)
            .WithErrorCode("CreateSponsorRequest.SlugTooLong")
            .WithMessage("Slug must be 63 characters or fewer.")
            .When(r => !string.IsNullOrWhiteSpace(r.Sponsor.Slug));

        RuleFor(r => r.Sponsor.Tier)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.TierRequired")
            .WithMessage("Tier is required.")
            .Must(tier => SponsorTier.List.Any(t => t.Name == tier))
            .WithErrorCode("CreateSponsorRequest.TierInvalid")
            .WithMessage("Tier must be one of: Title Sponsor, Premier, Standard.");

        RuleFor(r => r.Sponsor.Category)
            .NotEmpty()
            .WithErrorCode("CreateSponsorRequest.CategoryRequired")
            .WithMessage("Category is required.")
            .Must(category => SponsorCategory.List.Any(c => c.Name == category))
            .WithErrorCode("CreateSponsorRequest.CategoryInvalid")
            .WithMessage("Category must be a known sponsor category.");

        RuleFor(r => r.Sponsor.WebsiteUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.WebsiteUrlInvalid")
            .WithMessage("WebsiteUrl must be an absolute URI.")
            .When(r => r.Sponsor.WebsiteUrl is not null);

        RuleFor(r => r.Sponsor.FacebookUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.FacebookUrlInvalid")
            .WithMessage("FacebookUrl must be an absolute URI.")
            .When(r => r.Sponsor.FacebookUrl is not null);

        RuleFor(r => r.Sponsor.InstagramUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("CreateSponsorRequest.InstagramUrlInvalid")
            .WithMessage("InstagramUrl must be an absolute URI.")
            .When(r => r.Sponsor.InstagramUrl is not null);

        // Structural-only: all-or-nothing shape of the contact block. Whether the phone/email
        // values themselves are *valid* NANP/RFC formats is a business rule left to
        // PhoneNumber.CreateNorthAmerican / EmailAddress.Create in the handler.
        RuleFor(r => r.Sponsor.Contact)
            .Must(contact => !string.IsNullOrWhiteSpace(contact!.Name)
                && !string.IsNullOrWhiteSpace(contact.PhoneNumber)
                && !string.IsNullOrWhiteSpace(contact.Email))
            .WithErrorCode("CreateSponsorRequest.ContactIncomplete")
            .WithMessage("If any contact field is supplied, Name, PhoneNumber, and Email are all required.")
            .When(r => r.Sponsor.Contact is not null);
    }
}
```

### Contracts (`Neba.Api.Contracts.Sponsors.CreateSponsor/`, new folder)

**New — `SponsorPhoneNumberInput.cs`**:

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// A phone number entry supplied when creating a sponsor.
/// </summary>
public sealed record SponsorPhoneNumberInput
{
    /// <summary>
    /// The phone number type value (e.g. "H", "M", "W", "F" — see <c>PhoneNumberType</c>).
    /// </summary>
    public required string PhoneNumberType { get; init; }

    /// <summary>
    /// The phone number, which may include formatting characters.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// An optional extension.
    /// </summary>
    public string? Extension { get; init; }
}
```

**New — `SponsorContactInput.cs`**:

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Contact person details for a sponsor. All three fields are required together — if any one is
/// supplied, all three must be.
/// </summary>
public sealed record SponsorContactInput
{
    /// <summary>
    /// The contact person's name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The contact person's phone number type value (see <c>PhoneNumberType</c>).
    /// </summary>
    public required string PhoneNumberType { get; init; }

    /// <summary>
    /// The contact person's phone number, which may include formatting characters.
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// An optional extension for the contact person's phone number.
    /// </summary>
    public string? Extension { get; init; }

    /// <summary>
    /// The contact person's email address.
    /// </summary>
    public required string Email { get; init; }
}
```

**New — `SponsorLogoInput.cs`** (mirrors `HeaderImageInput`):

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Represents the input required for a sponsor's logo image, already uploaded to storage.
/// </summary>
public sealed record SponsorLogoInput
{
    public required string Container { get; init; }

    public required string Path { get; init; }

    public required string ContentType { get; init; }

    public required long SizeInBytes { get; init; }
}
```

**New — `SponsorInput.cs`**:

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// The fields required to create a sponsor.
/// </summary>
public sealed record SponsorInput
{
    public required string Name { get; init; }

    /// <summary>
    /// An optional staff-supplied slug override. When null or blank, the slug is derived from <see cref="Name"/>.
    /// </summary>
    public string? Slug { get; init; }

    public required bool IsCurrentSponsor { get; init; }

    public required int Priority { get; init; }

    /// <summary>
    /// The sponsor tier name (see <c>SponsorTier</c>): "Title Sponsor", "Premier", or "Standard".
    /// </summary>
    public required string Tier { get; init; }

    /// <summary>
    /// The sponsor category name (see <c>SponsorCategory</c>).
    /// </summary>
    public required string Category { get; init; }

    public SponsorLogoInput? Logo { get; init; }

    public Uri? WebsiteUrl { get; init; }

    public string? TagPhrase { get; init; }

    public string? Description { get; init; }

    public string? LiveReadText { get; init; }

    public string? PromotionalNotes { get; init; }

    public Uri? FacebookUrl { get; init; }

    public Uri? InstagramUrl { get; init; }

    public string? BusinessStreet { get; init; }

    public string? BusinessUnit { get; init; }

    public string? BusinessCity { get; init; }

    /// <summary>
    /// The US state postal abbreviation (e.g. "MA" — see <c>UsState</c>).
    /// </summary>
    public string? BusinessState { get; init; }

    public string? BusinessPostalCode { get; init; }

    public string? BusinessEmailAddress { get; init; }

    public IReadOnlyCollection<SponsorPhoneNumberInput> PhoneNumbers { get; init; } = [];

    public SponsorContactInput? Contact { get; init; }
}
```

**New — `CreateSponsorRequest.cs`**:

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Creates a sponsor.
/// </summary>
public sealed record CreateSponsorRequest
{
    /// <summary>
    /// The sponsor fields to create.
    /// </summary>
    public required SponsorInput Sponsor { get; init; }
}
```

**New — `SponsorResponse.cs`**:

```csharp
namespace Neba.Api.Contracts.Sponsors.CreateSponsor;

/// <summary>
/// Response returned after successfully creating a sponsor.
/// </summary>
public sealed record SponsorResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the newly created sponsor.
    /// </summary>
    public required string SponsorId { get; init; }

    /// <summary>
    /// The normalized, unique slug assigned to the sponsor.
    /// </summary>
    public required string Slug { get; init; }
}
```

**Edit — `src/Neba.Api.Contracts/Sponsors/ISponsorsApi.cs`** — add:

```csharp
using Neba.Api.Contracts.Sponsors.CreateSponsor;

// ...inside ISponsorsApi...

/// <summary>
/// Creates a sponsor.
/// </summary>
[Post("/sponsors")]
Task<IApiResponse<SponsorResponse>> CreateSponsorAsync(CreateSponsorRequest request, CancellationToken cancellationToken = default);
```

### Test Factories (`Neba.TestFactory.Sponsors/`, extending existing folder)

- `SponsorFactory.cs` — no changes required; `Create()`'s current defaults (`ValidName`, `ValidSlug`, etc.) already satisfy `Sponsor.Create`'s new invariants.

**New — `CreatedSponsorFactory.cs`**:

```csharp
using Neba.Api.Features.Sponsors.CreateSponsor;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.TestFactory.Sponsors;

public static class CreatedSponsorFactory
{
    public static CreatedSponsor Create(SponsorId? id = null, string? slug = null)
        => new()
        {
            Id = id ?? SponsorId.New(),
            Slug = slug ?? SponsorFactory.ValidSlug
        };
}
```

**New — `SponsorInputFactory.cs`** / **`CreateSponsorRequestFactory.cs`** (endpoint-test request bodies; mirrors the shape of `SponsorFactory.Create`):

```csharp
using Neba.Api.Contracts.Sponsors.CreateSponsor;

namespace Neba.TestFactory.Sponsors;

public static class SponsorInputFactory
{
    public static SponsorInput Create(
        string? name = null,
        string? slug = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        string? tier = null,
        string? category = null,
        SponsorLogoInput? logo = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessUnit = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<SponsorPhoneNumberInput>? phoneNumbers = null,
        SponsorContactInput? contact = null)
            => new()
            {
                Name = name ?? SponsorFactory.ValidName,
                Slug = slug,
                IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
                Priority = priority ?? SponsorFactory.ValidPriority,
                Tier = tier ?? SponsorFactory.ValidTier.Name,
                Category = category ?? SponsorFactory.ValidCategory.Name,
                Logo = logo,
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                LiveReadText = liveReadText,
                PromotionalNotes = promotionalNotes,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessStreet = businessStreet,
                BusinessUnit = businessUnit,
                BusinessCity = businessCity,
                BusinessState = businessState,
                BusinessPostalCode = businessPostalCode,
                BusinessEmailAddress = businessEmailAddress,
                PhoneNumbers = phoneNumbers ?? [],
                Contact = contact
            };
}

public static class CreateSponsorRequestFactory
{
    public static CreateSponsorRequest Create(SponsorInput? sponsor = null)
        => new()
        {
            Sponsor = sponsor ?? SponsorInputFactory.Create()
        };
}
```

- `SponsorResponseFactory.cs` in `Neba.Api.Contracts.Sponsors.CreateSponsor`'s namespace — a small `Create(string? sponsorId, string? slug)` factory mirroring `CreatedSponsorFactory`, for endpoint-response assertions.

### Tests (`Neba.Api.Tests`)

- `CreateSponsorEndpointTests.cs` — Verify-snapshot happy path, empty/edge cases, `Configure` route+auth test, 409/422 error-path tests — same structure as the `new-endpoint` skill's endpoint-test template and `DeleteArticleEndpointAuthorizationTests` conventions. Cover the `Send.CreatedAtAsync` `LinkGenerator` throw pattern documented in CLAUDE.md's "API Layer Mutation Testing" learning (item 6) for the success path.
- `CreateSponsorCommandHandlerTests.cs` (unit) — slug-conflict path, each value-object validation failure path (bad email, bad phone, bad address), contact all-or-nothing rejection path, success path with/without optional fields populated. `MockBehavior.Strict` for `IFusionCache`.
- `SponsorTests.cs` (domain, new or extend existing) — `Create` validation: name required, slug normalization/reserved/invalid, and the happy path building a fully-populated `Sponsor`.
- `SlugNormalizerTests.cs` (new, domain) — the extracted normalizer's edge cases (previously covered indirectly via `ArticleTests`); keep `ArticleTests`' existing slug-normalization assertions passing unchanged since behavior is identical.
- `CreateSponsorRequestValidatorTests.cs` — structural validation rules only, including the contact all-or-nothing structural check.
- `ListActiveSponsorsQueryHandlerTests.cs` (amend existing) — add a case for `CallerHasSponsorManagementPermission = true` returning inactive sponsors too, alongside the existing active-only case.
- `ListActiveSponsorsEndpointTests.cs` (amend existing) — add a case asserting `CallerHasSponsorManagementPermission` is populated from `User.HasAnyPermission(...)` correctly for both an authenticated management-permission caller and an anonymous/unpermitted caller.
- `CacheDescriptorsTests.cs` (amend existing, if present) — update any test asserting on `CacheDescriptors.Sponsors.ListActiveSponsors` as a property to call it as a method with both `true`/`false` args, per the `/cache-descriptor` skill's generated-test convention.

### Deferred to later (explicitly out of scope for this feature)

- Edit/Delete Sponsor (not requested).
- Sponsor logo upload flow details (endpoint reuse vs. new upload endpoint) — resolved in Phase 2 since it's UI-driven, same as Article's header image.
- Canadian business addresses (assumption 1).

---

## Phase 2: UI

### Pages (`src/Neba.Website.Server/Sponsors/`)

- **New — `SponsorsManage.razor`** (`@page "/sponsors/manage"`) — admin-gated list of sponsors, calling the existing (now permission-aware, per Phase 1's amendment) `ISponsorsApi.ListActiveSponsorsAsync`. Since the page is only reachable behind `Permissions.CreateSponsor.PolicyName`, the same authenticated call that renders the page also carries the claim the API checks — the caller gets every sponsor back, active and inactive, in one request. No separate admin endpoint or view model needed; reuses `SponsorSummaryViewModel`/`SponsorMappingExtensions` as-is.
  - **Active/inactive split**: rendered as two sections, not one flat list — an **Active Sponsors** section on top (grouped by tier the same way the public `Sponsors.razor` page does: Title / Premier / Standard) and a separate **Inactive Sponsors** section below it. This keeps a former Title Sponsor that's now inactive from visually competing with the current Title Sponsor for the single "top slot" styling — the inactive section renders as a plain list (no title/premier tier treatment), since tier styling on an inactive record would be misleading. Computed client-side in `@code`: `ActiveSponsors => _sponsors.Where(s => s.IsCurrentSponsor)`, `InactiveSponsors => _sponsors.Where(s => !s.IsCurrentSponsor)`.
  - Structurally mirrors `NewsList.razor` otherwise: title bar, `<AuthorizeView Policy="@Permissions.CreateSponsor.PolicyName">`-gated `FabCreateButton Href="/sponsors/new" Label="Create Sponsor"`, each row showing Name, Tier, Category, Priority.
  - `<PageTitle>Manage Sponsors - BowlNEBA</PageTitle>`, `@rendermode @(new InteractiveServerRenderMode(prerender: false))` (loads data in `OnInitializedAsync`, same reasoning as `NewsList.razor`).
- **New — `CreateSponsor.razor`** (`@page "/sponsors/new"`) — mirrors `CreateArticle.razor`'s structure end-to-end: `EditContext`-based `EditForm` + `DirtyFormGuard`, sections for:
  - Core fields: Name, Slug (optional override + auto-generated placeholder preview, same `NormalizeSlug` JS-free client mirror as `CreateArticle.razor`), IsCurrentSponsor (checkbox), Priority (number input), Tier (`InputSelect` over `SponsorTier.List`), Category (`InputSelect` over `SponsorCategory.List`).
  - Logo: single-file `FileUpload` (image only), same upload-then-attach pattern as Article's header image (`UploadHeaderImageAsync`-equivalent hitting a new-or-reused sponsor logo upload endpoint — see Open Question below).
  - Optional promo fields: WebsiteUrl, TagPhrase, Description, LiveReadText, PromotionalNotes, FacebookUrl, InstagramUrl — plain `InputText`/`InputTextArea`.
  - Business address block: Street/Unit/City `InputText`, `UsState` `InputSelect`, PostalCode `InputText` — manual entry only, per the scoping decision above.
  - Business email: `InputText` (validated client-side as an email format; server does the authoritative `EmailAddress.Create` validation).
  - Phone numbers: repeatable rows (Type `InputSelect` over `PhoneNumberType`, Number, Extension) with add/remove — see `PhoneNumberListEditor` component below.
  - Contact info: Name/Phone/Email fields, enforced all-or-nothing client-side (disable submit / show a validation message if exactly 1–2 of the 3 are filled) to match the handler's all-or-nothing rule from Phase 1's assumption 3.
  - `<PageTitle>Create Sponsor - BowlNEBA</PageTitle>`. No async initial data load (Tier/Category are static `SmartEnum` lists, no API dependency for dropdown population) — default `@rendermode InteractiveServer` (prerender true) is sufficient, no flash risk.
- **`Sponsors.razor`** (existing public page) — untouched.

### Components (`src/Neba.Website.Server/Sponsors/`, new)

- **New — `PhoneNumberListEditor.razor`** — small reusable repeatable-row editor for a `List<PhoneNumberInput>`-shaped binding (Type/Number/Extension per row, add/remove buttons). Built as a standalone component now (not inlined in `CreateSponsor.razor`) since the same editor will be needed by a future Edit Sponsor page — avoids inlining logic that's already known to be reused.

### API Client (`src/Neba.Website.Server/Sponsors/`)

- No new `ISponsorsApi` methods and no new view model — `SponsorsManage.razor` reuses `CreateSponsorAsync` (Phase 1) and the existing `ListActiveSponsorsAsync` + `SponsorSummaryViewModel`/`ToViewModel()` unchanged.

### Dirty tracking / guard

`DirtyFormGuard` applies to `CreateSponsor.razor` (data-entry form) — same wiring as `CreateArticle.razor`: `EditContext` created in the constructor, `OnFieldChanged += MarkDirty`, explicit `MarkDirty()` calls for anything not routed through an `InputBase` descendant (the Logo `FileUpload` callbacks, `PhoneNumberListEditor` add/remove callbacks, and the Tier/Category selects if they end up as plain `<select>`/`@onchange` rather than `InputSelect` — prefer `InputSelect` bound through `EditContext` where possible so `OnFieldChanged` covers it for free, matching CLAUDE.md's guidance). Reset `_isDirty = false` right before navigating away after a successful create. `SponsorsManage.razor` is a list/display page, not a data-entry form — no guard needed.

### Tests (Phase 2)

- **bUnit** (`tests/Neba.Website.Tests/Sponsors/`): `CreateSponsorPageTests.cs` (form validation, dirty-tracking marks, contact-info all-or-nothing client check, submit success → navigates to `/sponsors/{slug}`, submit failure → shows `_errorMessage`), `SponsorsManagePageTests.cs` (active sponsors render in the top section grouped by tier, inactive sponsors render in a separate bottom section without tier styling, FAB visibility gated on permission, empty state). Mock `ISponsorsApi` per CLAUDE.md's `StubApiResponse<T>` convention (not `Mock<IApiResponse<T>>`).
- **Playwright** (`tests/e2e/`): extend `Sponsors.spec.ts` or add `SponsorsManage.spec.ts` — this qualifies per the new-page-with-API-backed-rendering and navigation-flow rows of the Playwright-vs-bUnit decision table (list page → create page → back, real HTTP + real browser). The existing `MOCK_SPONSOR_OLD_SPONSOR` fixture (`isCurrentSponsor: false`) is already usable for the inactive-section case — extend the mock `GET /sponsors` handler to return the full set (including it) when the request carries the management permission, matching the amended endpoint's real behavior, plus add a `POST /sponsors` handler. Cover: navigate to manage page, confirm active/inactive sections render separately, click FAB, fill form, submit, land on detail page; and a validation-failure path (duplicate slug → 409 → inline error shown, no navigation).

### Open question for this gate

**Sponsor logo upload endpoint** — `CreateArticle.razor` uploads the header image via `NewsApi.UploadArticleHeaderImageAsync` (a News-scoped upload endpoint) before the article itself is created, tracking it as a `PendingUpload` that's claimed on `CreateArticleCommandHandler`'s success path. Does Sponsor logo upload need its own equivalent (`ISponsorsApi.UploadSponsorLogoAsync` + matching `PendingUploads`-claim logic in `CreateSponsorCommandHandler`, which Phase 1's functional draft didn't include), or is there a shared/generic upload endpoint already usable across features that this should call instead? Flagging since Phase 1 didn't scope this — confirm before the code draft so the endpoint list is right in both phases.

### Deferred to later (explicitly out of scope for this feature)

- Edit/Delete Sponsor pages (not requested — `PhoneNumberListEditor` is still built as a standalone component in anticipation, per above).
- Google Places (or equivalent) address autocomplete — tracked as [`docs/plans/address-autocomplete-issue.md`](./address-autocomplete-issue.md), to revisit once member self-service address updates (or another second address-entry form) exist to justify the shared integration cost.
- A dedicated `Sponsors.View`/`Sponsors.Manage` permission distinct from `Sponsors.CreateSponsor`, if the single-permission `SponsorManagementPermissions` collection turns out to be too coarse once Edit/Delete Sponsor exist (same shape as `ArticleManagementPermissions` growing to include `EditArticle`/`DeleteArticle`).
