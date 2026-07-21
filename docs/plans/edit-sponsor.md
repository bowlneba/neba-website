# Edit Sponsor

Allows staff with sponsor-management permission to edit an existing sponsor's fields (including logo, business address, phone numbers, and contact person), following the same structure as the existing Edit Article feature.

## Decisions locked in during scoping

- **GetSponsorDetail is extended, not duplicated.** `SponsorDetailDto`/`SponsorDetailResponse` gain `LiveReadText`, `PromotionalNotes`, and a `Contact` block. These are populated only when `CallerHasSponsorManagementPermission` is `true` (the same gate that already exists for viewing an inactive sponsor) — an anonymous/public caller never sees them. The edit page loads its initial data from this same endpoint rather than a new admin-only query.
- **`Sponsor` gains an `Update(...)` method.** Today `Sponsor` is a pure init-only aggregate (unlike `Article`, which has private-set properties + `Update()`). Editable properties (everything except `Id` and `Slug`, which stay immutable) move to `private set` and a new `Update(...)` method mirrors `Article.Update`'s shape: re-validates `Name` non-empty, and re-checks the Title-tier-uniqueness invariant (excluding the sponsor being edited from the "is it taken" check — see Phase 1 for how the handler supplies this).
- **Logo is editable** using the same upload/remove/replace pattern as `EditArticle.razor`'s header image (current-logo preview + "Remove current image" + `FileUpload` replacement), reusing the existing `UploadSponsorLogoAsync` endpoint.
- **Slug remains immutable** after creation, same as `Article.Slug` — displayed read-only on the edit form.
- **New permission**: `Sponsors.EditSponsor` (`"Edit Sponsor"`), following the `News.EditArticle` naming convention, added to `SponsorManagementPermissions`.

## Phase 1: API

### Domain

**`src/Neba.Api/Features/Sponsors/Domain/Sponsor.cs`** (edit) — convert editable properties from `{ get; init; }` to `{ get; private set; }` (all except `Id`, `Slug`). `PhoneNumbers`/`TournamentsSponsored` keep their existing backing-field-free `new List<T>()` defaults (already fixed for the EF array hazard) but become privately settable via `Update`. Add:

```csharp
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
    Justification = "Aggregate mutator — mirrors Create's parameter set minus Slug (immutable). See CLAUDE.md 'Always-Valid Entities'.")]
public ErrorOr<Updated> Update(
    string name,
    bool isCurrentSponsor,
    int priority,
    SponsorTier tier,
    SponsorCategory category,
    bool isTitleSponsorshipAvailable,
    StoredFile? logo,
    Uri? websiteUrl,
    string? tagPhrase,
    string? description,
    string? liveReadText,
    string? promotionalNotes,
    Uri? facebookUrl,
    Uri? instagramUrl,
    Address? businessAddress,
    EmailAddress? businessEmail,
    IReadOnlyCollection<PhoneNumber> phoneNumbers,
    ContactInfo? sponsorContact)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return SponsorErrors.NameRequired;
    }

    if (tier == SponsorTier.TitleSponsor && !isTitleSponsorshipAvailable)
    {
        return SponsorErrors.TitleSponsorshipUnavailable;
    }

    Name = name;
    IsCurrentSponsor = isCurrentSponsor;
    Priority = priority;
    Tier = tier;
    Category = category;
    Logo = logo;
    WebsiteUrl = websiteUrl;
    TagPhrase = tagPhrase;
    Description = description;
    LiveReadText = liveReadText;
    PromotionalNotes = promotionalNotes;
    FacebookUrl = facebookUrl;
    InstagramUrl = instagramUrl;
    BusinessAddress = businessAddress;
    BusinessEmail = businessEmail;
    PhoneNumbers = phoneNumbers;
    SponsorContact = sponsorContact;

    return Result.Updated;
}
```

No changes to `SponsorErrors.cs` — `NameRequired`/`TitleSponsorshipUnavailable` are reused as-is.

### Application (Command)

**`src/Neba.Api/Features/Sponsors/SponsorFieldBuilder.cs`** (new — extracted from `CreateSponsorCommandHandler`, shared by both handlers):

```csharp
internal static class SponsorFieldBuilder
{
    public static ErrorOr<Address?> BuildBusinessAddress(
        string? street, string? unit, string? city, UsState? state, string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            return (Address?)null;
        }

        ArgumentNullException.ThrowIfNull(state);

        var result = Address.Create(street, unit, city ?? string.Empty, state, postalCode ?? string.Empty);
        return result.IsError ? result.Errors : result.Value;
    }

    public static ErrorOr<EmailAddress?> BuildBusinessEmail(string? businessEmailAddress)
    {
        if (string.IsNullOrWhiteSpace(businessEmailAddress))
        {
            return (EmailAddress?)null;
        }

        var result = EmailAddress.Create(businessEmailAddress);
        return result.IsError ? result.Errors : result.Value;
    }

    public static ErrorOr<IReadOnlyCollection<PhoneNumber>> BuildPhoneNumbers(
        IReadOnlyCollection<PhoneNumberInput> phoneNumbers)
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
    public static ErrorOr<ContactInfo?> BuildSponsorContact(
        string? contactName, PhoneNumberType? contactPhoneType, string? contactPhoneNumber,
        string? contactPhoneExtension, string? contactEmail)
    {
        var anySupplied = !string.IsNullOrWhiteSpace(contactName)
            || !string.IsNullOrWhiteSpace(contactPhoneNumber)
            || !string.IsNullOrWhiteSpace(contactEmail);

        if (!anySupplied)
        {
            return (ContactInfo?)null;
        }

        ArgumentNullException.ThrowIfNull(contactPhoneType);

        var phoneResult = PhoneNumber.CreateNorthAmerican(contactPhoneType, contactPhoneNumber ?? string.Empty, contactPhoneExtension);
        if (phoneResult.IsError)
        {
            return phoneResult.Errors;
        }

        var emailResult = EmailAddress.Create(contactEmail ?? string.Empty);
        return emailResult.IsError
            ? emailResult.Errors
            : new ContactInfo { Name = contactName ?? string.Empty, Phone = phoneResult.Value, Email = emailResult.Value };
    }
}
```

`CreateSponsorCommandHandler` is updated to call these static methods (passing its command's individual fields) instead of its own private copies — behavior-neutral refactor.

**`src/Neba.Api/Features/Sponsors/EditSponsor/EditSponsorCommand.cs`** (new):

```csharp
internal sealed record EditSponsorCommand
    : ICommand<Updated>
{
    public required SponsorId SponsorId { get; init; }

    public required string Name { get; init; }

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

    public IReadOnlyCollection<PhoneNumberInput> PhoneNumbers { get; init; } = [];

    public string? ContactName { get; init; }

    public PhoneNumberType? ContactPhoneType { get; init; }

    public string? ContactPhoneNumber { get; init; }

    public string? ContactPhoneExtension { get; init; }

    public string? ContactEmail { get; init; }
}
```

**`src/Neba.Api/Features/Sponsors/EditSponsor/EditSponsorCommandHandler.cs`** (new):

```csharp
internal sealed class EditSponsorCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<EditSponsorCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSponsorCommand command, CancellationToken cancellationToken)
    {
        var sponsor = await appDbContext.Sponsors
            .SingleOrDefaultAsync(s => s.Id == command.SponsorId, cancellationToken);

        if (sponsor is null)
        {
            return SponsorErrors.SponsorNotFound(command.SponsorId.Value.ToString());
        }

        var addressResult = SponsorFieldBuilder.BuildBusinessAddress(
            command.BusinessStreet, command.BusinessUnit, command.BusinessCity, command.BusinessState, command.BusinessPostalCode);
        if (addressResult.IsError)
        {
            return addressResult.Errors;
        }

        var emailResult = SponsorFieldBuilder.BuildBusinessEmail(command.BusinessEmailAddress);
        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        var phoneNumbersResult = SponsorFieldBuilder.BuildPhoneNumbers(command.PhoneNumbers);
        if (phoneNumbersResult.IsError)
        {
            return phoneNumbersResult.Errors;
        }

        var contactResult = SponsorFieldBuilder.BuildSponsorContact(
            command.ContactName, command.ContactPhoneType, command.ContactPhoneNumber, command.ContactPhoneExtension, command.ContactEmail);
        if (contactResult.IsError)
        {
            return contactResult.Errors;
        }

        // Cross-aggregate fact (CLAUDE.md "Aggregate Invariants Requiring Cross-Aggregate Data"):
        // is Title tier held by some OTHER current sponsor? Excludes this sponsor so re-saving its
        // own existing Title tier doesn't self-conflict.
        var titleSponsorshipTaken = command.Tier == SponsorTier.TitleSponsor
            && await appDbContext.Sponsors.AnyAsync(
                s => s.Id != command.SponsorId && s.IsCurrentSponsor && s.Tier == SponsorTier.TitleSponsor,
                cancellationToken);

        // Must snapshot before Update() — Logo is mutated in place, so reading it after the call
        // would return the new value, not the one being replaced (see EditArticleCommandHandler).
        var previousLogo = sponsor.Logo;

        var updateResult = sponsor.Update(
            command.Name,
            command.IsCurrentSponsor,
            command.Priority,
            command.Tier,
            command.Category,
            isTitleSponsorshipAvailable: !titleSponsorshipTaken,
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

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await RemoveClaimedPendingUploadAsync(command.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:sponsors", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:sponsors:{sponsor.Slug}", token: cancellationToken);

        if (previousLogo is not null && previousLogo != command.Logo)
        {
            backgroundJobScheduler.Enqueue(new DeleteSponsorFilesJob
            {
                Files =
                [
                    new StoredFileReference { Container = previousLogo.Container, Path = previousLogo.Path }
                ]
            });
        }

        return Result.Updated;
    }

    private async Task RemoveClaimedPendingUploadAsync(StoredFile? logo, CancellationToken cancellationToken)
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

### Infrastructure (background job — orphaned logo cleanup)

**`src/Neba.Api/Features/Sponsors/EditSponsor/StoredFileReference.cs`** (new):

```csharp
public sealed record StoredFileReference
{
    public required string Container { get; init; }

    public required string Path { get; init; }
}
```

**`src/Neba.Api/Features/Sponsors/EditSponsor/DeleteSponsorFilesJob.cs`** (new):

```csharp
public sealed record DeleteSponsorFilesJob
    : IBackgroundJob
{
    public required IReadOnlyCollection<StoredFileReference> Files { get; init; }

    public string JobName
        => $"{nameof(DeleteSponsorFilesJob)}: {Files.Count} file(s)";
}
```

**`src/Neba.Api/Features/Sponsors/EditSponsor/DeleteSponsorFilesJobHandler.cs`** (new) — identical shape to `DeleteArticleFilesJobHandler`, renamed:

```csharp
internal sealed class DeleteSponsorFilesJobHandler(
        IFileStorageService fileStorageService,
        ILogger<DeleteSponsorFilesJobHandler> logger)
    : IBackgroundJobHandler<DeleteSponsorFilesJob>
{
    public async Task ExecuteAsync(DeleteSponsorFilesJob job, CancellationToken cancellationToken)
    {
        foreach (var file in job.Files)
        {
            await DeleteFileAsync(file, cancellationToken);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to log the error and continue processing other files.")]
    private async Task DeleteFileAsync(StoredFileReference file, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorageService.DeleteAsync(file.Container, file.Path, cancellationToken);
            logger.LogDeletedSponsorFile(file.Container, file.Path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogFailedToDeleteSponsorFile(ex, file.Container, file.Path);
        }
    }
}

internal static partial class DeleteSponsorFilesJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Deleted sponsor file from container '{Container}' with path '{Path}'.")]
    public static partial void LogDeletedSponsorFile(this ILogger<DeleteSponsorFilesJobHandler> logger, string container, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to delete sponsor file from container '{Container}' with path '{Path}'")]
    public static partial void LogFailedToDeleteSponsorFile(this ILogger<DeleteSponsorFilesJobHandler> logger, Exception exception, string container, string path);
}
```

`IBackgroundJobHandler<>` Scrutor scan already picks this up automatically (per `BackgroundJobConfiguration.cs` — no registration edit needed, matching `DeleteArticleFilesJobHandler`).

### Caching

No `CacheDescriptors.cs` change — the handler removes tags directly by string (`"neba:sponsors"`, `$"neba:sponsors:{slug}"`), matching both existing tags already emitted by `CacheDescriptors.Sponsors.Detail`/`ListActiveSponsors`. Both the public- and management-scoped cache entries for the sponsor share the `$"neba:sponsors:{slug}"` tag, so one `RemoveByTagAsync` call invalidates both scopes.

### API (Endpoint + Contracts)

**`src/Neba.Api.Contracts/Sponsors/EditSponsor/EditSponsorInput.cs`** (new) — identical to `SponsorInput` minus `Slug`:

```csharp
public sealed record EditSponsorInput
{
    public required string Name { get; init; }

    public required bool IsCurrentSponsor { get; init; }

    public required int Priority { get; init; }

    public required string Tier { get; init; }

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

    public string? BusinessState { get; init; }

    public string? BusinessPostalCode { get; init; }

    public string? BusinessEmailAddress { get; init; }

    public IReadOnlyCollection<SponsorPhoneNumberInput> PhoneNumbers { get; init; } = [];

    public SponsorContactInput? Contact { get; init; }
}
```

**`src/Neba.Api.Contracts/Sponsors/EditSponsor/EditSponsorRequest.cs`** (new):

```csharp
public sealed record EditSponsorRequest
{
    public required string Id { get; init; }

    public required EditSponsorInput Sponsor { get; init; }
}
```

**`src/Neba.Api.Contracts/Sponsors/ISponsorsApi.cs`** (edit) — add:

```csharp
[Put("/sponsors/{id}")]
Task<IApiResponse> EditSponsorAsync(string id, EditSponsorRequest request, CancellationToken cancellationToken = default);
```

**`src/Neba.Api/Features/Sponsors/EditSponsor/EditSponsorEndpoint.cs`** (new) — mirrors `EditArticleEndpoint`:

```csharp
internal sealed class EditSponsorEndpoint(Messaging.ICommandHandler<EditSponsorCommand, Updated> commandHandler)
    : Endpoint<EditSponsorRequest>
{
    private readonly Messaging.ICommandHandler<EditSponsorCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<SponsorsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Sponsors")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSponsor.PolicyName);

        Description(description => description
            .WithName("EditSponsor")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditSponsorRequest req, CancellationToken ct)
    {
        var input = req.Sponsor;

        var command = new EditSponsorCommand
        {
            SponsorId = new SponsorId(req.Id),
            Name = input.Name,
            IsCurrentSponsor = input.IsCurrentSponsor,
            Priority = input.Priority,
            Tier = SponsorTier.FromName(input.Tier),
            Category = SponsorCategory.FromName(input.Category),
            Logo = input.Logo is null ? null : new StoredFile
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
            BusinessState = string.IsNullOrWhiteSpace(input.BusinessState) ? null : UsState.FromValue(input.BusinessState),
            BusinessPostalCode = input.BusinessPostalCode,
            BusinessEmailAddress = input.BusinessEmailAddress,
            PhoneNumbers = [.. input.PhoneNumbers.Select(p => new PhoneNumberInput
            {
                Type = PhoneNumberType.FromValue(p.PhoneNumberType),
                Number = p.PhoneNumber,
                Extension = p.Extension
            })],
            ContactName = input.Contact?.Name,
            ContactPhoneType = input.Contact is null ? null : PhoneNumberType.FromValue(input.Contact.PhoneNumberType),
            ContactPhoneNumber = input.Contact?.PhoneNumber,
            ContactPhoneExtension = input.Contact?.Extension,
            ContactEmail = input.Contact?.Email
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

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

**`src/Neba.Api/Features/Sponsors/EditSponsor/EditSponsorRequestValidator.cs`** (new) — mirrors `CreateSponsorRequestValidator` minus the `Slug` rule, plus an `Id` rule matching `EditArticleRequestValidator`:

```csharp
internal sealed class EditSponsorRequestValidator
    : Validator<EditSponsorRequest>
{
    public EditSponsorRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("EditSponsorRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Sponsor.Name)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.NameRequired")
            .WithMessage("Name is required.")
            .MaximumLength(63)
            .WithErrorCode("EditSponsorRequest.NameTooLong")
            .WithMessage("Name must be 63 characters or fewer.");

        RuleFor(r => r.Sponsor.Tier)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.TierRequired")
            .WithMessage("Tier is required.")
            .Must(tier => SponsorTier.List.Any(t => t.Name == tier))
            .WithErrorCode("EditSponsorRequest.TierInvalid")
            .WithMessage("Tier must be one of: Title Sponsor, Premier, Standard.");

        RuleFor(r => r.Sponsor.Category)
            .NotEmpty()
            .WithErrorCode("EditSponsorRequest.CategoryRequired")
            .WithMessage("Category is required.")
            .Must(category => SponsorCategory.List.Any(c => c.Name == category))
            .WithErrorCode("EditSponsorRequest.CategoryInvalid")
            .WithMessage("Category must be a known sponsor category.");

        RuleFor(r => r.Sponsor.WebsiteUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.WebsiteUrlInvalid")
            .WithMessage("WebsiteUrl must be an absolute URI.")
            .When(r => r.Sponsor.WebsiteUrl is not null);

        RuleFor(r => r.Sponsor.FacebookUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.FacebookUrlInvalid")
            .WithMessage("FacebookUrl must be an absolute URI.")
            .When(r => r.Sponsor.FacebookUrl is not null);

        RuleFor(r => r.Sponsor.InstagramUrl)
            .Must(uri => uri!.IsAbsoluteUri)
            .WithErrorCode("EditSponsorRequest.InstagramUrlInvalid")
            .WithMessage("InstagramUrl must be an absolute URI.")
            .When(r => r.Sponsor.InstagramUrl is not null);

        RuleFor(r => r.Sponsor.Contact)
            .Must(contact => !string.IsNullOrWhiteSpace(contact!.Name)
                && !string.IsNullOrWhiteSpace(contact.PhoneNumber)
                && !string.IsNullOrWhiteSpace(contact.Email))
            .WithErrorCode("EditSponsorRequest.ContactIncomplete")
            .WithMessage("If any contact field is supplied, Name, PhoneNumber, and Email are all required.")
            .When(r => r.Sponsor.Contact is not null);
    }
}
```

### Extending GetSponsorDetail (per confirmed decision)

**`src/Neba.Api/Features/Sponsors/GetSponsorDetail/SponsorContactDto.cs`** (new):

```csharp
public sealed record SponsorContactDto
{
    public required string Name { get; init; }

    public required PhoneNumberDto Phone { get; init; }

    public required string Email { get; init; }
}
```

**`SponsorDetailDto.cs`** (edit) — add:

```csharp
public string? LiveReadText { get; init; }

public string? PromotionalNotes { get; init; }

public SponsorContactDto? Contact { get; init; }
```

**`GetSponsorDetailQueryHandler.cs`** (edit) — extend the anonymous projection with `sponsor.LiveReadText`, `sponsor.PromotionalNotes`, and a `Contact` shape (`sponsor.SponsorContact != null ? new { ... } : null`), then in the final `SponsorDetailDto` construction:

```csharp
LiveReadText = query.CallerHasSponsorManagementPermission ? row.LiveReadText : null,
PromotionalNotes = query.CallerHasSponsorManagementPermission ? row.PromotionalNotes : null,
Contact = query.CallerHasSponsorManagementPermission && row.Contact is not null
    ? new SponsorContactDto
    {
        Name = row.Contact.Name,
        Phone = new PhoneNumberDto { PhoneNumberType = row.Contact.PhoneType, Number = row.Contact.PhoneNumber },
        Email = row.Contact.Email
    }
    : null,
```

**`src/Neba.Api.Contracts/Sponsors/SponsorContactResponse.cs`** (new):

```csharp
public sealed record SponsorContactResponse
{
    public required string Name { get; init; }

    public required PhoneNumberResponse Phone { get; init; }

    public required string Email { get; init; }
}
```

**`SponsorDetailResponse.cs`** (edit) — add `string? LiveReadText`, `string? PromotionalNotes`, `SponsorContactResponse? Contact` (additive, nullable).

**`GetSponsorDetailEndpoint.cs`** (edit) — map the three new fields through in the `SponsorDetailResponse` construction.

No `CacheDescriptors.cs` change — the existing `ManagementScope`/`PublicScope` key split on `Sponsors.Detail` already prevents an admin-scoped entry from ever being served to a public-scoped cache lookup.

### Security

**`src/Neba.Api.Contracts/Security/Permission.cs`** (edit):

```csharp
public static readonly Permissions EditSponsor = new("Sponsors.EditSponsor", "Edit Sponsor");

public static readonly IReadOnlyCollection<Permissions> SponsorManagementPermissions =
[
    CreateSponsor,
    EditSponsor,
];
```

### Tests

- **`tests/Neba.Api.Tests/Features/Sponsors/Domain/SponsorTests.cs`** (edit) — add `Update_Should...` cases mirroring `ArticleTests.Update_Should...`: success path, empty-name validation error, Title-tier conflict (taken by another sponsor), and Title-tier success when already held by the sponsor itself (the "excluding itself" case — the one genuinely new invariant behavior versus `Create`).
- **`tests/Neba.Api.Tests/Features/Sponsors/EditSponsor/EditSponsorCommandHandlerTests.cs`** (new) — success (full field round-trip incl. logo replace/remove, phone numbers, contact), not-found, Title-tier conflict excluding self, orphaned-logo-file job enqueued only when logo actually changes, pending-upload claim removal, both cache tags removed (`MockBehavior.Strict` throughout, per CLAUDE.md).
- **`tests/Neba.Api.Tests/Features/Sponsors/EditSponsor/EditSponsorEndpointTests.cs`** (new) — Configure tests + HandleAsync branch tests, following the `EditArticleEndpoint` pattern (mind the FastEndpoints static-state gotchas documented in CLAUDE.md if this test spins up a real host).
- **`tests/Neba.Api.Tests/Features/Sponsors/GetSponsorDetail/GetSponsorDetailQueryHandlerTests.cs`** (edit) — add cases asserting `LiveReadText`/`PromotionalNotes`/`Contact` are populated when `CallerHasSponsorManagementPermission` is true and null otherwise.
- **`tests/Neba.TestFactory/Sponsors/EditSponsorRequestFactory.cs`** / **`EditSponsorInputFactory.cs`** (new) — per CLAUDE.md's test-factory requirement, `Create()` with nullable params + const defaults, `Bogus(count, seed)`.
- **`tests/Neba.TestFactory/Sponsors/SponsorContactDtoFactory.cs`** / **`SponsorContactResponseFactory.cs`** (new) — same pattern.
- `SponsorFactory.Create()` signature is unchanged (still calls `Sponsor.Create(...)`), so no edit needed there.

### Deferred / out of scope

- No new DB migration — `Update()` doesn't change the schema, only which properties are mutable at the C# level.
- No change to `ListActiveSponsors` — unaffected by this feature.
- Concurrent-Title-tier-clash-at-save-time (`DbUpdateException` from the filtered unique index) is an existing, documented gap noted on `Sponsor.Create`'s XML doc — `Update` inherits the same fast-path-only guarantee; not being hardened further here.

## Phase 2: UI

### Security

**`src/Neba.Api.Contracts/Security/Permission.cs`** (edit) — add under `#region Sponsors`:

```csharp
public const string CanManageSponsorsPolicyName = "CanManageSponsors";
```

**`src/Neba.Api.Contracts/Security/PolicyExtensions.cs`** (edit):

```csharp
public AuthorizationBuilder AddNebaPolicies()
{
    builder.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy
        .RequireAssertion(context => context.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));

    builder.AddPolicy(Permissions.CanManageSponsorsPolicyName, policy => policy
        .RequireAssertion(context => context.User.HasAnyPermission(Permissions.SponsorManagementPermissions)));

    return builder;
}
```

`Sponsors.razor` and `SponsorDetail.razor` swap their existing `Policy="@Permissions.CreateSponsor.PolicyName"` checks (the ones gating the Active/Inactive badge and Inactive Sponsors section — not the FAB, which correctly stays `CreateSponsor`-only) to `Policy="@Permissions.CanManageSponsorsPolicyName"`.

### Pages

**`src/Neba.Website.Server/Sponsors/EditSponsor.razor`** (new) — route `/sponsors/{Slug}/edit`. Structurally a hybrid of `EditArticle.razor` (load-by-slug, `_isLoading`/`_notFound`/`_loadErrorMessage`, `DirtyFormGuard`, logo replace/remove) and `CreateSponsor.razor` (section layout, phone-number rows, contact block):

```razor
@page "/sponsors/{Slug}/edit"
@using System.ComponentModel.DataAnnotations
@using ErrorOr
@using Neba.Api.Contracts.Contact
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Sponsors
@using Neba.Api.Contracts.Sponsors.CreateSponsor
@using Neba.Api.Contracts.Sponsors.EditSponsor
@using Neba.Api.Contracts.Uploads
@using Neba.Website.Server.Notifications
@using Neba.Website.Server.Services
@using Refit
@implements IAsyncDisposable
@rendermode @(new InteractiveServerRenderMode(prerender: false))

@inject ApiExecutor ApiExecutor
@inject ISponsorsApi SponsorsApi
@inject NavigationManager NavigationManager
@inject ToastService ToastService

<PageTitle>@(_sponsor is not null ? "Edit " + _sponsor.Name + " - BowlNEBA" : "Edit Sponsor - BowlNEBA")</PageTitle>

<AuthorizeView Policy="@Permissions.EditSponsor.PolicyName" Context="authContext">
    <Authorized>

        @if (_isLoading)
        {
            <div class="neba-space-y-6" aria-busy="true" aria-label="Loading sponsor">
                <NebaSkeletonLoader Type="SkeletonType.Custom" Width="55%" Height="2rem" />
                <NebaSkeletonLoader Type="SkeletonType.Text" Rows="6" />
            </div>
        }
        else if (_notFound)
        {
            <div class="news-empty">
                <p class="news-empty-text">This sponsor could not be found.</p>
                <a href="/sponsors" class="neba-btn neba-btn-secondary">Back to Sponsors</a>
            </div>
        }
        else if (_sponsor is null)
        {
            <NebaAlert Severity="NotifySeverity.Error" Title="Unable to Load Sponsor" Message="@_loadErrorMessage" Dismissible="false" />
        }
        else
        {
            <div class="neba-space-y-6">

                <div class="page-title-bar">
                    <div class="page-title-inner">
                        <h1>Edit Sponsor</h1>
                        <p>Update "@_sponsor.Name"</p>
                    </div>
                </div>

                @if (!string.IsNullOrWhiteSpace(_errorMessage))
                {
                    <NebaAlert Severity="NotifySeverity.Error" Title="Unable to Save Sponsor" Message="@_errorMessage" Dismissible="true"
                               OnDismiss="@(() => _errorMessage = null)" />
                }

                <DirtyFormGuard IsDirty="@_isDirty" />

                <div class="neba-card">
                    <EditForm EditContext="_editContext" FormName="EditSponsorForm" OnValidSubmit="HandleSaveAsync">
                        <DataAnnotationsValidator />
                        <div class="neba-space-y-6">

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Basic Info</h2>

                                <div>
                                    <label for="name" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Name</label>
                                    <InputText id="name" @bind-Value="_model.Name" class="neba-input" placeholder="Sponsor name" />
                                    <ValidationMessage For="@(() => _model.Name)" class="block text-sm text-red-600 mt-1" />
                                </div>

                                <div>
                                    <label class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Slug</label>
                                    <p class="neba-input" style="background:var(--neba-gray-050,#FAFAFA);">@_sponsor.Slug</p>
                                    <p class="text-sm text-[var(--neba-gray-500)] mt-1">
                                        The slug cannot be changed after a sponsor is created.
                                    </p>
                                </div>

                                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                    <div>
                                        <label for="tier" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Tier</label>
                                        <InputSelect id="tier" @bind-Value="_model.Tier" class="neba-select">
                                            <option value="Title Sponsor">Title Sponsor</option>
                                            <option value="Premier">Premier</option>
                                            <option value="Standard">Standard</option>
                                        </InputSelect>
                                        <ValidationMessage For="@(() => _model.Tier)" class="block text-sm text-red-600 mt-1" />
                                    </div>

                                    <div>
                                        <label for="category" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Category</label>
                                        <InputSelect id="category" @bind-Value="_model.Category" class="neba-select">
                                            @foreach (var category in SponsorCategories)
                                            {
                                                <option value="@category">@category</option>
                                            }
                                        </InputSelect>
                                        <ValidationMessage For="@(() => _model.Category)" class="block text-sm text-red-600 mt-1" />
                                    </div>

                                    <div>
                                        <label for="priority" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Priority</label>
                                        <InputNumber id="priority" @bind-Value="_model.Priority" class="neba-input" />
                                        <ValidationMessage For="@(() => _model.Priority)" class="block text-sm text-red-600 mt-1" />
                                    </div>
                                </div>

                                <div class="flex items-center gap-2">
                                    <InputCheckbox id="is-current-sponsor" @bind-Value="_model.IsCurrentSponsor" />
                                    <label for="is-current-sponsor" class="text-sm font-medium text-[var(--neba-gray-700)]">Current sponsor</label>
                                </div>
                            </section>

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Logo</h2>
                                @if (_logo is not null)
                                {
                                    <div class="edit-sponsor-current-file">
                                        @if (_logo.Url is not null)
                                        {
                                            <img src="@_logo.Url" alt="" class="edit-sponsor-logo-preview" />
                                        }
                                        <button type="button" class="neba-btn neba-btn-secondary" @onclick="RemoveLogo">Remove current logo</button>
                                    </div>
                                }
                                <FileUpload MaxFiles="1" Accept="image/*" MaxFileSizeBytes="@(5 * 1024 * 1024)" Label="Upload a replacement logo"
                                            OnUploadRequestedAsync="UploadLogoAsync"
                                            OnFileUploaded="@(response => { _logo = ToLogoRef(response); MarkDirty(); })"
                                            OnFileRemoved="@(_ => { _logo = null; MarkDirty(); })"
                                            OnBusyChanged="@(busy => _isLogoUploading = busy)" />
                            </section>

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Links &amp; Content</h2>

                                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                    <div>
                                        <label for="website-url" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Website URL</label>
                                        <InputText id="website-url" @bind-Value="_model.WebsiteUrl" class="neba-input" placeholder="https://example.com" />
                                        <ValidationMessage For="@(() => _model.WebsiteUrl)" class="block text-sm text-red-600 mt-1" />
                                    </div>

                                    <div>
                                        <label for="facebook-url" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Facebook URL</label>
                                        <InputText id="facebook-url" @bind-Value="_model.FacebookUrl" class="neba-input" placeholder="https://facebook.com/…" />
                                        <ValidationMessage For="@(() => _model.FacebookUrl)" class="block text-sm text-red-600 mt-1" />
                                    </div>

                                    <div>
                                        <label for="instagram-url" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Instagram URL</label>
                                        <InputText id="instagram-url" @bind-Value="_model.InstagramUrl" class="neba-input" placeholder="https://instagram.com/…" />
                                        <ValidationMessage For="@(() => _model.InstagramUrl)" class="block text-sm text-red-600 mt-1" />
                                    </div>
                                </div>

                                <div>
                                    <label for="tag-phrase" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Tag Phrase</label>
                                    <InputText id="tag-phrase" @bind-Value="_model.TagPhrase" class="neba-input" placeholder="A short tagline" />
                                </div>

                                <div>
                                    <label for="description" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Description</label>
                                    <InputTextArea id="description" @bind-Value="_model.Description" class="neba-input" rows="3" placeholder="Public-facing description" />
                                </div>

                                <div>
                                    <label for="live-read-text" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Live Read Text</label>
                                    <InputTextArea id="live-read-text" @bind-Value="_model.LiveReadText" class="neba-input" rows="2" placeholder="Text to be read live at events" />
                                </div>

                                <div>
                                    <label for="promotional-notes" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Promotional Notes</label>
                                    <InputTextArea id="promotional-notes" @bind-Value="_model.PromotionalNotes" class="neba-input" rows="2" placeholder="Internal notes for staff" />
                                </div>
                            </section>

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Business Address</h2>

                                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                    <div>
                                        <label for="business-street" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Street</label>
                                        <InputText id="business-street" @bind-Value="_model.BusinessStreet" class="neba-input" />
                                    </div>
                                    <div>
                                        <label for="business-unit" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Unit</label>
                                        <InputText id="business-unit" @bind-Value="_model.BusinessUnit" class="neba-input" />
                                    </div>
                                </div>

                                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                    <div>
                                        <label for="business-city" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">City</label>
                                        <InputText id="business-city" @bind-Value="_model.BusinessCity" class="neba-input" />
                                    </div>
                                    <div>
                                        <label for="business-state" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">State</label>
                                        <select class="neba-select" value="@_model.BusinessState" @onchange="@(e => { _model.BusinessState = e.Value?.ToString(); MarkDirty(); })">
                                            <option value="">Select a state</option>
                                            @foreach (var state in UsState.List)
                                            {
                                                <option value="@state.Value">@state.Name</option>
                                            }
                                        </select>
                                    </div>
                                    <div>
                                        <label for="business-postal-code" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Postal Code</label>
                                        <InputText id="business-postal-code" @bind-Value="_model.BusinessPostalCode" class="neba-input" />
                                    </div>
                                </div>

                                <div>
                                    <label for="business-email" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Business Email</label>
                                    <InputText id="business-email" @bind-Value="_model.BusinessEmailAddress" class="neba-input" placeholder="info@example.com" />
                                    <ValidationMessage For="@(() => _model.BusinessEmailAddress)" class="block text-sm text-red-600 mt-1" />
                                </div>
                            </section>

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Phone Numbers</h2>

                                @if (_phoneNumbers.Count > 0)
                                {
                                    <ul class="create-sponsor-phone-list">
                                        @foreach (var phoneNumber in _phoneNumbers)
                                        {
                                            <li class="create-sponsor-phone-row">
                                                <select class="neba-select create-sponsor-phone-type" value="@phoneNumber.PhoneNumberType" @onchange="@(e => { phoneNumber.PhoneNumberType = e.Value?.ToString() ?? "H"; MarkDirty(); })">
                                                    @foreach (var phoneType in PhoneNumberType.List)
                                                    {
                                                        <option value="@phoneType.Value">@phoneType.Name</option>
                                                    }
                                                </select>
                                                <input class="neba-input create-sponsor-phone-number" value="@phoneNumber.PhoneNumber"
                                                       @oninput="@(e => { phoneNumber.PhoneNumber = e.Value?.ToString() ?? string.Empty; MarkDirty(); })"
                                                       placeholder="Phone number" />
                                                <input class="neba-input create-sponsor-phone-extension" value="@phoneNumber.Extension"
                                                       @oninput="@(e => { phoneNumber.Extension = e.Value?.ToString(); MarkDirty(); })"
                                                       placeholder="Ext." />
                                                <button type="button" class="neba-btn neba-btn-danger create-sponsor-phone-remove" @onclick="@(() => RemovePhoneNumber(phoneNumber))">Remove</button>
                                            </li>
                                        }
                                    </ul>
                                }

                                <button type="button" class="neba-btn neba-btn-secondary" @onclick="AddPhoneNumber">Add Phone Number</button>
                            </section>

                            <section class="neba-space-y-4">
                                <h2 class="create-sponsor-section-title">Contact Person</h2>
                                <p class="text-sm text-[var(--neba-gray-500)]">Optional. If any field is filled in, Name, Phone, and Email are all required.</p>

                                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                    <div>
                                        <label for="contact-name" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Name</label>
                                        <InputText id="contact-name" @bind-Value="_model.ContactName" class="neba-input" />
                                    </div>
                                    <div>
                                        <label for="contact-email" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Email</label>
                                        <InputText id="contact-email" @bind-Value="_model.ContactEmail" class="neba-input" placeholder="name@example.com" />
                                        <ValidationMessage For="@(() => _model.ContactEmail)" class="block text-sm text-red-600 mt-1" />
                                    </div>
                                </div>

                                <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                                    <div>
                                        <label for="contact-phone-type" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Phone Type</label>
                                        <InputSelect id="contact-phone-type" @bind-Value="_model.ContactPhoneType" class="neba-select">
                                            @foreach (var phoneType in PhoneNumberType.List)
                                            {
                                                <option value="@phoneType.Value">@phoneType.Name</option>
                                            }
                                        </InputSelect>
                                    </div>
                                    <div>
                                        <label for="contact-phone-number" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Phone Number</label>
                                        <InputText id="contact-phone-number" @bind-Value="_model.ContactPhoneNumber" class="neba-input" />
                                    </div>
                                    <div>
                                        <label for="contact-phone-extension" class="block text-sm font-medium text-[var(--neba-gray-700)] mb-1">Extension</label>
                                        <InputText id="contact-phone-extension" @bind-Value="_model.ContactPhoneExtension" class="neba-input" />
                                    </div>
                                </div>
                            </section>

                            @if (_isLogoUploading)
                            {
                                <p class="text-sm text-[var(--neba-gray-500)]">Uploading logo…</p>
                            }

                            <div class="flex items-center gap-3">
                                <button type="submit" class="neba-btn neba-btn-primary" disabled="@(_isSubmitting || _isLogoUploading)">
                                    @(_isSubmitting ? "Saving…" : "Save Changes")
                                </button>
                                <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancel" disabled="@_isSubmitting">
                                    Cancel
                                </button>
                            </div>

                        </div>
                    </EditForm>
                </div>

            </div>
        }

    </Authorized>
    <NotAuthorized>
        <div class="news-empty">
            <p class="news-empty-text">You don't have permission to edit sponsors.</p>
            <a href="/sponsors" class="neba-btn neba-btn-secondary">Back to Sponsors</a>
        </div>
    </NotAuthorized>
</AuthorizeView>

@code {
    [Parameter]
    public string Slug { get; set; } = string.Empty;

    private static readonly IReadOnlyList<string> SponsorCategories =
    [
        "Other", "Manufacturer", "Pro Shop", "Bowling Center", "Financial Services", "Technology", "Media", "Individual"
    ];

    private readonly EditSponsorFormModel _model = new();
    private readonly EditContext _editContext;
    private readonly List<TrackedPhoneNumber> _phoneNumbers = [];

    private bool _isLoading = true;
    private bool _notFound;
    private bool _isSubmitting;
    private bool _isDirty;
    private string? _loadErrorMessage;
    private string? _errorMessage;
    private SponsorDetailResponse? _sponsor;
    private string _sponsorId = string.Empty;

    private LogoRef? _logo;
    private bool _isLogoUploading;

    public EditSponsor()
    {
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    private void MarkDirty() => _isDirty = true;

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync(
            "Sponsors",
            "GetSponsorDetail",
            c => SponsorsApi.GetSponsorBySlugAsync(Slug, c));

        _isLoading = false;

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                _notFound = true;
                return;
            }

            _loadErrorMessage = result.FirstError.Description;
            return;
        }

        _sponsor = result.Value;
        _sponsorId = _sponsor.Id.ToString();

        _model.Name = _sponsor.Name;
        _model.IsCurrentSponsor = _sponsor.IsCurrentSponsor;
        _model.Priority = _sponsor.Priority;
        _model.Tier = _sponsor.Tier;
        _model.Category = _sponsor.Category;
        _model.WebsiteUrl = _sponsor.WebsiteUrl?.ToString();
        _model.FacebookUrl = _sponsor.FacebookUrl?.ToString();
        _model.InstagramUrl = _sponsor.InstagramUrl?.ToString();
        _model.TagPhrase = _sponsor.TagPhrase;
        _model.Description = _sponsor.Description;
        _model.LiveReadText = _sponsor.LiveReadText;
        _model.PromotionalNotes = _sponsor.PromotionalNotes;
        _model.BusinessStreet = _sponsor.BusinessStreet;
        _model.BusinessUnit = _sponsor.BusinessUnit;
        _model.BusinessCity = _sponsor.BusinessCity;
        _model.BusinessState = _sponsor.BusinessState;
        _model.BusinessPostalCode = _sponsor.BusinessPostalCode;
        _model.BusinessEmailAddress = _sponsor.BusinessEmailAddress;
        _model.ContactName = _sponsor.Contact?.Name;
        _model.ContactPhoneType = _sponsor.Contact?.Phone.PhoneNumberType ?? "H";
        _model.ContactPhoneNumber = _sponsor.Contact?.Phone.PhoneNumber;
        _model.ContactEmail = _sponsor.Contact?.Email;

        if (_sponsor.LogoUrl is not null)
        {
            _logo = new LogoRef { Url = _sponsor.LogoUrl };
        }

        _phoneNumbers.AddRange(_sponsor.PhoneNumbers.Select(p => new TrackedPhoneNumber
        {
            PhoneNumberType = p.PhoneNumberType,
            PhoneNumber = p.PhoneNumber
        }));
    }

    private void AddPhoneNumber()
    {
        _phoneNumbers.Add(new TrackedPhoneNumber());
        MarkDirty();
    }

    private void RemovePhoneNumber(TrackedPhoneNumber phoneNumber)
    {
        _phoneNumbers.Remove(phoneNumber);
        MarkDirty();
    }

    private async Task<ErrorOr<UploadedFileResponse>> UploadLogoAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => await ApiExecutor.ExecuteAsync(
            "Sponsors",
            "UploadSponsorLogo",
            c => SponsorsApi.UploadSponsorLogoAsync(new StreamPart(stream, fileName, contentType), c),
            ct);

    private static LogoRef ToLogoRef(UploadedFileResponse upload) => new()
    {
        Container = upload.Container,
        Path = upload.Path,
        ContentType = upload.ContentType,
        SizeInBytes = upload.SizeInBytes,
        Url = upload.Url
    };

    private void RemoveLogo()
    {
        _logo = null;
        MarkDirty();
    }

    private async Task HandleSaveAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;

        var request = new EditSponsorRequest { Id = _sponsorId, Sponsor = BuildSponsorInput() };

        var result = await ApiExecutor.ExecuteAsync(
            "Sponsors",
            "EditSponsor",
            ct => SponsorsApi.EditSponsorAsync(_sponsorId, request, ct));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        _isDirty = false;
        ToastService.Show("Sponsor Updated", "\"" + _model.Name + "\" was successfully updated.", NotifySeverity.Success);

        // See CreateArticle.razor's HandleCreateAsync for why the render must be allowed to process
        // before navigating: without it, NavigateTo runs the NavigationLock's before-navigate check
        // against the guard's stale (still-dirty) parameter and incorrectly shows the discard prompt.
        StateHasChanged();
        await Task.Yield();

        NavigationManager.NavigateTo($"/sponsors/{Slug}");
    }

    private void HandleCancel() => NavigationManager.NavigateTo($"/sponsors/{Slug}");

    private EditSponsorInput BuildSponsorInput() => new()
    {
        Name = _model.Name,
        IsCurrentSponsor = _model.IsCurrentSponsor,
        Priority = _model.Priority,
        Tier = _model.Tier,
        Category = _model.Category,
        Logo = BuildLogoInput(),
        WebsiteUrl = ParseUri(_model.WebsiteUrl),
        TagPhrase = string.IsNullOrWhiteSpace(_model.TagPhrase) ? null : _model.TagPhrase,
        Description = string.IsNullOrWhiteSpace(_model.Description) ? null : _model.Description,
        LiveReadText = string.IsNullOrWhiteSpace(_model.LiveReadText) ? null : _model.LiveReadText,
        PromotionalNotes = string.IsNullOrWhiteSpace(_model.PromotionalNotes) ? null : _model.PromotionalNotes,
        FacebookUrl = ParseUri(_model.FacebookUrl),
        InstagramUrl = ParseUri(_model.InstagramUrl),
        BusinessStreet = string.IsNullOrWhiteSpace(_model.BusinessStreet) ? null : _model.BusinessStreet,
        BusinessUnit = string.IsNullOrWhiteSpace(_model.BusinessUnit) ? null : _model.BusinessUnit,
        BusinessCity = string.IsNullOrWhiteSpace(_model.BusinessCity) ? null : _model.BusinessCity,
        BusinessState = string.IsNullOrWhiteSpace(_model.BusinessState) ? null : _model.BusinessState,
        BusinessPostalCode = string.IsNullOrWhiteSpace(_model.BusinessPostalCode) ? null : _model.BusinessPostalCode,
        BusinessEmailAddress = string.IsNullOrWhiteSpace(_model.BusinessEmailAddress) ? null : _model.BusinessEmailAddress,
        PhoneNumbers = [.. _phoneNumbers
            .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
            .Select(p => new SponsorPhoneNumberInput
            {
                PhoneNumberType = p.PhoneNumberType,
                PhoneNumber = p.PhoneNumber,
                Extension = string.IsNullOrWhiteSpace(p.Extension) ? null : p.Extension
            })],
        Contact = BuildContactInput()
    };

    private SponsorLogoInput? BuildLogoInput() => _logo is null || _logo.Container is null ? null : new SponsorLogoInput
    {
        Container = _logo.Container,
        Path = _logo.Path!,
        ContentType = _logo.ContentType!,
        SizeInBytes = _logo.SizeInBytes
    };

    private SponsorContactInput? BuildContactInput()
    {
        var hasContact = !string.IsNullOrWhiteSpace(_model.ContactName)
            || !string.IsNullOrWhiteSpace(_model.ContactPhoneNumber)
            || !string.IsNullOrWhiteSpace(_model.ContactEmail);

        if (!hasContact)
        {
            return null;
        }

        return new SponsorContactInput
        {
            Name = _model.ContactName ?? string.Empty,
            PhoneNumberType = _model.ContactPhoneType,
            PhoneNumber = _model.ContactPhoneNumber ?? string.Empty,
            Extension = string.IsNullOrWhiteSpace(_model.ContactPhoneExtension) ? null : _model.ContactPhoneExtension,
            Email = _model.ContactEmail ?? string.Empty
        };
    }

    private static string? ParseUri(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    public ValueTask DisposeAsync()
    {
        _editContext.OnFieldChanged -= HandleFieldChanged;
        return ValueTask.CompletedTask;
    }

    private sealed class LogoRef
    {
        public string? Container { get; init; }

        public string? Path { get; init; }

        public string? ContentType { get; init; }

        public long SizeInBytes { get; init; }

        public Uri? Url { get; init; }
    }

    private sealed class TrackedPhoneNumber
    {
        public string PhoneNumberType { get; set; } = "H";

        public string PhoneNumber { get; set; } = string.Empty;

        public string? Extension { get; set; }
    }

    private sealed class EditSponsorFormModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(63, ErrorMessage = "Name must be 63 characters or fewer.")]
        public string Name { get; set; } = string.Empty;

        public bool IsCurrentSponsor { get; set; } = true;

        [Range(0, int.MaxValue, ErrorMessage = "Priority must be zero or greater.")]
        public int Priority { get; set; }

        [Required(ErrorMessage = "Tier is required.")]
        public string Tier { get; set; } = "Standard";

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = "Other";

        [Url(ErrorMessage = "Website URL must be a valid, absolute URL.")]
        public string? WebsiteUrl { get; set; }

        [Url(ErrorMessage = "Facebook URL must be a valid, absolute URL.")]
        public string? FacebookUrl { get; set; }

        [Url(ErrorMessage = "Instagram URL must be a valid, absolute URL.")]
        public string? InstagramUrl { get; set; }

        public string? TagPhrase { get; set; }

        public string? Description { get; set; }

        public string? LiveReadText { get; set; }

        public string? PromotionalNotes { get; set; }

        public string? BusinessStreet { get; set; }

        public string? BusinessUnit { get; set; }

        public string? BusinessCity { get; set; }

        public string? BusinessState { get; set; }

        public string? BusinessPostalCode { get; set; }

        [EmailAddress(ErrorMessage = "Business email must be a valid email address.")]
        public string? BusinessEmailAddress { get; set; }

        public string? ContactName { get; set; }

        public string ContactPhoneType { get; set; } = "H";

        public string? ContactPhoneNumber { get; set; }

        public string? ContactPhoneExtension { get; set; }

        [EmailAddress(ErrorMessage = "Contact email must be a valid email address.")]
        public string? ContactEmail { get; set; }
    }
}
```

`_sponsor.Id` is a `Ulid` (from `SponsorDetailResponse.Id`) — `.ToString()` produces the 26-char ULID string `EditSponsorRequest.Id`/`EditSponsorEndpoint` expect, matching how `EditArticle.razor` uses `_article.ArticleId` (already a `string` there — Sponsors' response type differs slightly, hence the explicit `.ToString()`).

**`src/Neba.Website.Server/Sponsors/EditSponsor.razor.css`** (new) — only the two new classes not already covered by shared `neba-card`/`create-sponsor-*` styles (those are defined globally / in `CreateSponsor.razor.css` scoped to that component, so `edit-sponsor-*` needs its own copies since Blazor scopes component CSS):

```css
.edit-sponsor-current-file {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1rem;
}

.edit-sponsor-logo-preview {
    max-width: 8rem;
    max-height: 5rem;
    object-fit: contain;
    border: 1px solid var(--neba-gray-300);
    border-radius: var(--neba-radius);
    padding: 0.5rem;
    background: var(--neba-surface);
}
```

Also copy `.create-sponsor-section-title`, `.create-sponsor-phone-list`, `.create-sponsor-phone-row`, `.create-sponsor-phone-type`, `.create-sponsor-phone-number`, `.create-sponsor-phone-extension`, `.create-sponsor-phone-remove` from `CreateSponsor.razor.css` into `EditSponsor.razor.css` (Blazor CSS isolation scopes rules per-component file, so the classnames must be redefined here even though the markup and rule bodies are identical).

### Components (edit)

**`src/Neba.Website.Server/Sponsors/SponsorDetail.razor`** (edit) — add an Edit button next to the badges, and (using the confirmed-Phase-1 fields, plus the pre-existing but previously unused `sponsor-detail__promo`/`sponsor-detail__live-read`/`sponsor-detail__internal-contact` CSS already sitting in `SponsorDetail.razor.css` — see flag below) render the admin-only Live Read Text / Promotional Notes / internal Contact block when the viewer has edit access:

```razor
<div class="sponsor-detail__badges">
    <span class="sponsor-detail__badge sponsor-detail__badge--tier">@_model?.TierName</span>
    @if (_model?.CategoryName is not null)
    {
        <span class="sponsor-detail__badge sponsor-detail__badge--category">@_model.CategoryName</span>
    }
    <AuthorizeView Policy="@Permissions.CanManageSponsorsPolicyName">
        <Authorized>
            @if (_model?.IsCurrentSponsor == true)
            {
                <span class="sponsor-detail__badge sponsor-detail__badge--status-active">Active</span>
            }
            else
            {
                <span class="sponsor-detail__badge sponsor-detail__badge--status-inactive">Inactive</span>
            }
        </Authorized>
    </AuthorizeView>
</div>

<h1 class="sponsor-detail__title">@_model?.Name</h1>

@if (_model?.Tagline is not null)
{
    <p class="sponsor-detail__tagline">@_model.Tagline</p>
}

<div class="flex flex-wrap gap-3">
    @if (_model?.WebsiteUrl is not null)
    {
        <a href="@_model.WebsiteUrl" target="_blank" rel="noopener noreferrer"
           class="neba-btn neba-btn-primary sponsor-detail__website-btn">
            Visit Website
            <span class="material-symbols-outlined">open_in_new</span>
        </a>
    }
    <AuthorizeView Policy="@Permissions.EditSponsor.PolicyName">
        <Authorized>
            <a href="/sponsors/@Slug/edit" class="neba-btn neba-btn-secondary">
                <span class="material-symbols-outlined">edit</span>
                Edit Sponsor
            </a>
        </Authorized>
    </AuthorizeView>
</div>
```

...and, inside `.sponsor-detail__col-main` after the About section (admin-only block using the already-defined-but-unused CSS classes):

```razor
<AuthorizeView Policy="@Permissions.EditSponsor.PolicyName">
    <Authorized>
        @if (_model?.LiveReadText is not null || _model?.PromotionalNotes is not null)
        {
            <section class="sponsor-detail__promo">
                <div class="sponsor-detail__promo-header">
                    <span class="material-symbols-outlined">admin_panel_settings</span>
                    <h3 class="sponsor-detail__promo-title">Staff-Only Info</h3>
                </div>
                @if (_model.LiveReadText is not null)
                {
                    <div class="sponsor-detail__promo-block">
                        <p class="sponsor-detail__promo-label">Live Read Text</p>
                        <p class="sponsor-detail__live-read">@_model.LiveReadText</p>
                    </div>
                }
                @if (_model.PromotionalNotes is not null)
                {
                    <div class="sponsor-detail__promo-block">
                        <p class="sponsor-detail__promo-label">Promotional Notes</p>
                        <p class="sponsor-detail__live-read">@_model.PromotionalNotes</p>
                    </div>
                }
            </section>
        }
    </Authorized>
</AuthorizeView>
```

...and in `.sponsor-detail__col-aside`, alongside the existing contact card, an internal-contact card (only when `_model.Contact` is present):

```razor
<AuthorizeView Policy="@Permissions.EditSponsor.PolicyName">
    <Authorized>
        @if (_model?.Contact is not null)
        {
            <section class="neba-card sponsor-detail__internal-contact">
                <div class="sponsor-detail__internal-header">
                    <span class="material-symbols-outlined">badge</span>
                    <h3>Internal Contact</h3>
                </div>
                <p class="sponsor-detail__internal-name">@_model.Contact.Name</p>
                <div class="sponsor-detail__internal-details">
                    <div class="sponsor-detail__internal-row">
                        <span class="material-symbols-outlined">mail</span>
                        <a href="mailto:@_model.Contact.Email" style="color:inherit;">@_model.Contact.Email</a>
                    </div>
                    <div class="sponsor-detail__internal-row">
                        <span class="material-symbols-outlined">call</span>
                        <span>@FormatPhoneNumber(_model.Contact.Phone.PhoneNumber)</span>
                        <span class="sponsor-detail__internal-phone-type">@_model.Contact.Phone.PhoneNumberType</span>
                    </div>
                </div>
            </section>
        }
    </Authorized>
</AuthorizeView>
```

**Flag**: `SponsorDetail.razor.css` already contains `.sponsor-detail__promo*`, `.sponsor-detail__live-read`, and `.sponsor-detail__internal-contact*` classes that are currently unreferenced by any markup in `SponsorDetail.razor` — they appear to have been prepared for exactly this admin-view use case and never wired up. This code draft uses them as-is (no new CSS needed here beyond what already exists) rather than leaving them dead. This is additive display scope beyond "just add an Edit button" — flagging since it wasn't explicitly discussed, but it directly follows from Phase 1's decision to extend `GetSponsorDetail` with these fields (otherwise the API would return data the UI never shows to the very users it's gated for).

**`SponsorDetailViewModel.cs`** (edit) — add:

```csharp
public string? LiveReadText { get; init; }

public string? PromotionalNotes { get; init; }

public SponsorContactResponse? Contact { get; init; }
```

**`SponsorMappingExtensions.cs`** (edit) — add to the `SponsorDetailResponse` → `SponsorDetailViewModel` mapping:

```csharp
LiveReadText = response.LiveReadText,
PromotionalNotes = response.PromotionalNotes,
Contact = response.Contact,
```

**`src/Neba.Website.Server/Sponsors/Sponsors.razor`** (edit) — restructure each of the four tile types so the clickable card body is a nested `<a>` (like `ArticleCard.razor`'s `.article-card-link`) inside a non-link wrapper `<div>`, with the edit icon as a sibling of that inner link (not nested inside it) — per `ArticleCard.razor`'s own comment, Blazor's enhanced navigation intercepts clicks anywhere within an `<a>`'s subtree, so `@onclick:stopPropagation` on a nested button does **not** prevent the outer link from also navigating when the whole tile is itself the `<a>`. Concretely, for the Association tile (the others follow the same restructuring):

```razor
<div class="sponsor-tile neba-card flex flex-col items-center text-center p-4 gap-2 relative">
    <a href="/sponsors/@sponsor.Slug" class="sponsor-tile-link" aria-label="@sponsor.Name">
        <span class="material-symbols-outlined text-[var(--neba-blue-500)]" style="font-size: 2rem;" aria-hidden="true">
            storefront
        </span>
        <div class="text-xs font-medium uppercase tracking-wide text-[var(--neba-gray-500)]">
            @sponsor.Category
        </div>
        <h4 class="font-semibold text-sm text-[var(--neba-text)]">@sponsor.Name</h4>
    </a>
    <AuthorizeView Policy="@Permissions.EditSponsor.PolicyName">
        <Authorized>
            <a href="/sponsors/@sponsor.Slug/edit" class="icon-btn sponsor-tile-edit-btn" aria-label="Edit @sponsor.Name">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4Z" />
                </svg>
            </a>
        </Authorized>
    </AuthorizeView>
</div>
```

The Inactive tile gets the same treatment (its `.sponsor-tile__inactive-badge` stays a sibling, unaffected). The Premier card (`sponsor-card-premier`) and Title Sponsor hero card get the analogous split — inner `<a class="...-link">` wraps the existing content, edit icon becomes an absolutely-positioned sibling `<a class="icon-btn ...">`.

**`Sponsors.razor.css`** (edit) — add, mirroring `ArticleCard.razor.css`'s `.icon-btn`/`.card-edit-btn`:

```css
.sponsor-tile-link,
.sponsor-card-premier-link,
.title-sponsor-link {
    text-decoration: none;
    color: inherit;
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
}

.icon-btn {
    background: transparent;
    border: none;
    padding: 0.3rem;
    width: 1.9rem;
    height: 1.9rem;
    border-radius: var(--neba-radius, 0.375rem);
    color: var(--neba-gray-500, #737373);
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex: none;
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
}

.icon-btn:hover {
    background-color: var(--neba-gray-100, #F5F5F5);
    color: var(--neba-blue-600, #5563DD);
}

.sponsor-tile-edit-btn {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    z-index: 2;
    background-color: var(--neba-bg-panel, #fff);
}
```

(`.sponsor-tile__inactive-badge` already occupies top-right on inactive tiles — the edit button on that variant shifts to `right: 2.3rem` to avoid overlapping it; a small tile-specific override rule handles that case.)

### API Client

No new Refit interface needed beyond Phase 1's `ISponsorsApi.EditSponsorAsync` addition.

### State / Dirty Tracking

Same `EditContext` + `OnFieldChanged` → `MarkDirty()` wiring as `CreateSponsor.razor`/`EditArticle.razor` (see the full `EditSponsor.razor` code above). Non-`InputBase` controls with explicit `MarkDirty()` calls: phone-number rows, the Business State `<select>`, and the logo `FileUpload` callbacks.

### Page Title / Render Mode

Covered inline in the `EditSponsor.razor` code above — conditional `<PageTitle>`, `@rendermode @(new InteractiveServerRenderMode(prerender: false))`.

### Tests

- **`tests/Neba.Website.Tests/Sponsors/EditSponsorTests.cs`** (new, bUnit) — mirrors `EditArticleTests.cs`: mock `ISponsorsApi` (`MockBehavior.Strict`), `BunitAuthorizationContext`, cases for: loads and populates all fields (incl. admin-only `LiveReadText`/`PromotionalNotes`/`Contact`), not-found → empty state, load-error → alert, logo replace/remove flow, phone-number add/remove rows, contact all-or-nothing validation surfaced from the API's 422, successful save → navigates to `/sponsors/{slug}`, dirty-guard triggers on field edits.
- **`tests/Neba.Website.Tests/Sponsors/SponsorDetailTests.cs`** (edit, if it exists — else new) — add cases for the Edit button's visibility gating and the new Staff-Only Info / Internal Contact sections rendering only for `EditSponsor`-authorized viewers.
- **`tests/e2e/Sponsors.spec.ts`** (edit) — add a scenario: log in with `Sponsors.EditSponsor`, click a tile's edit icon (verify it doesn't navigate to the detail page instead), land on the edit form, change a field, save, verify the change reflects on the detail page.
- **Test factories**: `tests/Neba.TestFactory/Sponsors/EditSponsorInputFactory.cs` / `EditSponsorRequestFactory.cs` (from Phase 1) reused here; `SponsorDetailViewModelFactory.cs` (existing) gets `LiveReadText`/`PromotionalNotes`/`Contact` nullable params added.

### Deferred / out of scope

- A docs-screenshots/help-doc pass for Edit Sponsor — left to a follow-up `/help-documentation` invocation once this feature is implemented, matching how `edit-article.md`'s help doc was generated separately from the feature PR.
