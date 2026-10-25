# Hall of Fame Photo

Let authorized users upload, replace, and remove a Hall of Fame inductee's photo, and click the face to set a focal point so the round avatar frames it. Without a photo, the page keeps initials. GitHub issue #215.

## Decisions locked in during scoping

- **Not critical.** NFR checkpoint skipped. Routine edit on an existing feature; no money, no new PII.
- **Permission:** new `EditHallOfFamePhoto` (`HallOfFame.EditHallOfFamePhoto`) in a new `#region Hall of Fame`. No grouped `CanManage...` policy while it is the only one.
- **Roles:** Webmaster, Manager, Journalist. (The issue says "Tournament Manager"; no such role exists. Manager chosen. TournamentDirector does not get it.) No new role.
- **UI flow (confirmed):** Hall of Fame page lists inductees (avatar = photo at focal point, or initials). Users with the permission see an edit button per inductee. It opens a modal: upload/replace, click the face to set the focal point, drag or use arrow keys to refine (1%, Shift 5%), zoom slider, live round preview, remove photo. Save is disabled until something changes; `DirtyFormGuard` guards the modal. Save = `PUT`; cancel leaves the pending upload to the cleaner.
- **Shared upload pieces:** build generic versions under `src/Neba.Api/Uploads/` in this PR and use them for Hall of Fame only. Tournaments, Sponsors, and News migrate in a tech-debt branch opened right after this merges to the release branch (nothing is queued in Hangfire, so renaming job types is safe). This PR also adds `docs/plans/shared-upload-migration.md` describing that migration.
- **Focal point rules:** a new photo with no framing defaults to center and no zoom (50, 50, 100) in the handler; `Photo: null` with the framing set keeps the current photo and changes the framing (409 if the induction has no photo); `Photo: null` with no framing removes the photo. (Amended during Phase 2 planning: the modal cannot resend the current photo's storage reference.)
- **Zoom (added after reviewing the mockup, beyond the issue's written scope):** a face near a photo's edge cannot be centered in the round avatar, because the circle cannot leave the photo. Zoom shrinks the circle relative to the photo, so such a face can be centered. Stored as three whole numbers: `FocalX`, `FocalY` (0 to 100, the CSS `object-position` percents) and `Zoom` (100 to 300, where 100 is no zoom). Rendered with `object-position: X% Y%`, `transform-origin: X% Y%`, `transform: scale(Zoom/100)` on the `<img>`. X, Y, and Zoom are all set or all omitted. The picker supports click to place, then drag or arrow keys to refine. Update issue #215's text to match when this merges.
- **Gap in the issue:** `PUT /hall-of-fame/{id}/photo` needs the induction id, but the list returns none. The list DTO, summary, and response gain `Id`.

## Phase 1: API

Paths are relative to `src/`. Namespaces follow the folder.

### Contracts (`Neba.Api.Contracts`)

`Security/Permission.cs`: new region, placed between Sponsors and Tournaments (alphabetical by feature, as the file has it).

```csharp
#region Hall of Fame

/// <summary>
/// Permission to upload, replace, or remove a Hall of Fame inductee's photo and set its focal point.
/// </summary>
public static readonly Permissions EditHallOfFamePhoto = new("HallOfFame.EditHallOfFamePhoto", "Edit Hall of Fame Photo");

#endregion
```

`HallOfFame/ListHallOfFameInductions/HallOfFameInductionResponse.cs`: add three members.

```csharp
/// <summary>
/// The ULID string identifying the induction. Used to address the induction's photo.
/// </summary>
public required string Id { get; init; }

/// <summary>
/// Horizontal focal point of the photo, 0 to 100 percent from the left; null when no focal point is set.
/// </summary>
public int? PhotoFocalX { get; init; }

/// <summary>
/// Vertical focal point of the photo, 0 to 100 percent from the top; null when no focal point is set.
/// </summary>
public int? PhotoFocalY { get; init; }

/// <summary>
/// Zoom of the photo about its focal point, 100 to 300 percent (100 is no zoom); null when none is set.
/// </summary>
public int? PhotoZoom { get; init; }
```

`HallOfFame/UploadHallOfFamePhoto/UploadHallOfFamePhotoRequest.cs` (new):

```csharp
using Microsoft.AspNetCore.Http;

namespace Neba.Api.Contracts.HallOfFame.UploadHallOfFamePhoto;

/// <summary>
/// Request model for uploading a Hall of Fame photo.
/// </summary>
public sealed record UploadHallOfFamePhotoRequest
{
    /// <summary>
    /// The image file to be uploaded as an inductee's photo.
    /// </summary>
    public required IFormFile File { get; init; }
}
```

`HallOfFame/EditHallOfFamePhoto/HallOfFamePhotoInput.cs` (new), same shape as `TournamentLogoInput`:

```csharp
namespace Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

/// <summary>
/// An inductee photo already uploaded to storage.
/// </summary>
public sealed record HallOfFamePhotoInput
{
    /// <summary>The storage container the photo was uploaded to.</summary>
    public required string Container { get; init; }

    /// <summary>The storage path of the uploaded photo.</summary>
    public required string Path { get; init; }

    /// <summary>The MIME content type of the uploaded photo.</summary>
    public required string ContentType { get; init; }

    /// <summary>The size, in bytes, of the uploaded photo.</summary>
    public required long SizeInBytes { get; init; }
}
```

`HallOfFame/EditHallOfFamePhoto/EditHallOfFamePhotoInput.cs` and `EditHallOfFamePhotoRequest.cs` (new):

```csharp
/// <summary>
/// The photo change to apply to an induction.
/// </summary>
public sealed record EditHallOfFamePhotoInput
{
    /// <summary>The already-uploaded photo to use. <see langword="null"/> with no framing removes the current photo; <see langword="null"/> with framing keeps the current photo and changes the framing.</summary>
    public required HallOfFamePhotoInput? Photo { get; init; }

    /// <summary>Horizontal focal point, 0 to 100. Set with <see cref="FocalY"/> and <see cref="Zoom"/>, or omit all three. A new photo with all omitted uses the center and no zoom.</summary>
    public int? FocalX { get; init; }

    /// <summary>Vertical focal point, 0 to 100.</summary>
    public int? FocalY { get; init; }

    /// <summary>Zoom about the focal point, 100 to 300 percent (100 is no zoom).</summary>
    public int? Zoom { get; init; }
}

/// <summary>
/// Sets, replaces, or removes an inductee's photo and focal point.
/// </summary>
public sealed record EditHallOfFamePhotoRequest
{
    /// <summary>The ULID string identifying the induction.</summary>
    public required string Id { get; init; }

    /// <summary>The photo change to apply.</summary>
    public required EditHallOfFamePhotoInput Input { get; init; }
}
```

`HallOfFame/IHallOfFameApi.cs`: add two Refit methods.

```csharp
/// <summary>
/// Uploads a Hall of Fame photo to staging. Requires the HallOfFame.EditHallOfFamePhoto permission.
/// </summary>
[Multipart]
[Post("/hall-of-fame/photos")]
Task<IApiResponse<UploadedFileResponse>> UploadHallOfFamePhotoAsync(
    [AliasAs("File")] StreamPart file,
    CancellationToken cancellationToken = default);

/// <summary>
/// Sets, replaces, or removes an inductee's photo and focal point. Requires the HallOfFame.EditHallOfFamePhoto permission.
/// </summary>
[Put("/hall-of-fame/{id}/photo")]
Task<IApiResponse> EditHallOfFamePhotoAsync(string id, EditHallOfFamePhotoRequest request, CancellationToken cancellationToken = default);
```

### Domain (`Neba.Api/Features/HallOfFame/Domain`)

`PhotoFocalPoint.cs` (new):

```csharp
using ErrorOr;

namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// The point of a photo kept in frame when the photo is cropped to a shape, and how far the photo is zoomed in on it.
/// X and Y are the CSS <c>object-position</c> percents; the zoom scales the photo about that same point.
/// </summary>
public sealed record PhotoFocalPoint
{
    public const int Min = 0;
    public const int Max = 100;
    public const int MinZoom = 100;
    public const int MaxZoom = 300;

    /// <summary>The middle of the photo, no zoom.</summary>
    public static PhotoFocalPoint Default { get; } = new() { X = 50, Y = 50, Zoom = MinZoom };

    /// <summary>Percent from the left edge, 0 to 100.</summary>
    public int X { get; init; }

    /// <summary>Percent from the top edge, 0 to 100.</summary>
    public int Y { get; init; }

    /// <summary>Zoom in percent, 100 (none) to 300.</summary>
    public int Zoom { get; init; }

    internal static ErrorOr<PhotoFocalPoint> Create(int x, int y, int zoom)
    {
        if (x is < Min or > Max || y is < Min or > Max || zoom is < MinZoom or > MaxZoom)
        {
            return HallOfFameErrors.FocalPointOutOfRange;
        }

        return new PhotoFocalPoint { X = x, Y = y, Zoom = zoom };
    }
}
```

`HallOfFameErrors.cs` (new):

```csharp
using ErrorOr;

namespace Neba.Api.Features.HallOfFame.Domain;

internal static class HallOfFameErrors
{
    public static Error InductionNotFound(HallOfFameId id)
        => Error.NotFound(
            code: "HallOfFame.NotFound",
            description: "Hall of Fame induction was not found.",
            metadata: new Dictionary<string, object> { { "HallOfFameId", id.ToString() } });

    public static Error PhotoNotUploaded
        => Error.Validation("HallOfFame.Photo.NotUploaded", "Photo must be a file uploaded through the photo upload endpoint.");

    public static Error NoPhoto
        => Error.Conflict("HallOfFame.Photo.Missing", "The induction has no photo to position.");

    public static Error FocalPointOutOfRange
        => Error.Validation("HallOfFame.Photo.FocalPointOutOfRange", "Focal point must be between 0 and 100 and zoom between 100 and 300.");
}
```

`HallOfFameInduction.cs`: replace the `Photo` property and add the focal point and methods. `Photo` changes from `init` to `private set`, so the factory calls `SetPhoto` (no setter bypass).

```csharp
/// <summary>
/// An optional photo ... (existing summary kept)
/// </summary>
public StoredFile? Photo { get; private set; }

/// <summary>
/// The point and zoom of <see cref="Photo"/> kept in frame in round avatars. Null exactly when <see cref="Photo"/> is null.
/// </summary>
public PhotoFocalPoint? PhotoFocalPoint { get; private set; }

/// <summary>
/// Sets the photo and its focal point and zoom together.
/// </summary>
public ErrorOr<Success> SetPhoto(StoredFile photo, int focalX, int focalY, int zoom)
{
    ArgumentNullException.ThrowIfNull(photo);

    var focalPoint = PhotoFocalPoint.Create(focalX, focalY, zoom);
    if (focalPoint.IsError)
    {
        return focalPoint.Errors;
    }

    Photo = photo;
    PhotoFocalPoint = focalPoint.Value;
    return Result.Success;
}

/// <summary>
/// Removes the photo and its focal point. Does nothing when there is no photo.
/// </summary>
public void ClearPhoto()
{
    Photo = null;
    PhotoFocalPoint = null;
}

/// <summary>
/// Moves the focal point and changes the zoom of the current photo.
/// </summary>
public ErrorOr<Success> SetFocalPoint(int x, int y, int zoom)
{
    if (Photo is null)
    {
        return HallOfFameErrors.NoPhoto;
    }

    var focalPoint = PhotoFocalPoint.Create(x, y, zoom);
    if (focalPoint.IsError)
    {
        return focalPoint.Errors;
    }

    PhotoFocalPoint = focalPoint.Value;
    return Result.Success;
}
```

### Infrastructure / database

`Database/Configurations/HallOfFameInductionConfiguration.cs`: after `HasStoredFile(...)`.

```csharp
builder.ComplexProperty(induction => induction.PhotoFocalPoint, focalPoint =>
{
    focalPoint.Property(point => point.X).HasColumnName("photo_focal_x");
    focalPoint.Property(point => point.Y).HasColumnName("photo_focal_y");
    focalPoint.Property(point => point.Zoom).HasColumnName("photo_zoom");
});
```

Migration `AddHallOfFamePhotoFocalPoint`: `dotnet ef migrations add AddHallOfFamePhotoFocalPoint --project src/Neba.Api` (use the repo's existing migration command). Check the generated file adds three **nullable** `integer` columns `photo_focal_x`, `photo_focal_y`, `photo_zoom` and nothing else. If EF makes them non-nullable, add `.IsRequired(false)` is not valid on complex members; fix by confirming the complex property is declared nullable (`PhotoFocalPoint?`). Existing rows stay null.

`Auditing/AuditingConfiguration.cs`: open it during implementation; change only if it lists Hall of Fame columns.

### Shared upload pieces (`Neba.Api/Uploads`, new files)

`StoredFileClaim.cs`: generic form of `TournamentLogoClaim`; the "not uploaded" error is a parameter.

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Uploads;

/// <summary>
/// Confirms a caller-supplied file is one this API staged, so a request cannot attach (or later delete)
/// an arbitrary blob by naming its container and path.
/// </summary>
internal static class StoredFileClaim
{
    /// <summary>
    /// Returns <paramref name="current"/> when <paramref name="file"/> names the same blob, so a re-save
    /// never lets the caller overwrite the stored content type or size.
    /// </summary>
    public static StoredFile? Resolve(StoredFile? file, StoredFile? current)
        => file is not null && NamesFile(file, current) ? current : file;

    /// <summary>
    /// A file is claimable when it is null (removal), when it already is the current file
    /// (a re-save resubmits it), or when it matches a pending upload.
    /// </summary>
    public static async Task<ErrorOr<Success>> EnsureClaimableAsync(
        AppDbContext appDbContext,
        StoredFile? file,
        StoredFile? current,
        Error notUploadedError,
        CancellationToken cancellationToken)
    {
        if (file is null || NamesFile(file, current))
        {
            return Result.Success;
        }

        var isPending = await appDbContext.PendingUploads
            .AnyAsync(pending => pending.Container == file.Container && pending.Path == file.Path, cancellationToken);

        return isPending ? Result.Success : notUploadedError;
    }

    /// <summary>
    /// True when both name the same blob, or both are null.
    /// </summary>
    public static bool IsSameFile(StoredFile? file, StoredFile? current)
        => file is null ? current is null : NamesFile(file, current);

    private static bool NamesFile(StoredFile file, StoredFile? current)
        => current is not null
            && current.Container == file.Container
            && current.Path == file.Path;
}
```

`PendingUploadCleaner.cs`: the body of `TournamentPendingUploadCleaner` / `SponsorPendingUploadCleaner`, in `Neba.Api.Uploads`, named `PendingUploadCleaner.RemoveClaimedAsync(AppDbContext, StoredFile?, CancellationToken)`.

`DeleteStoredFilesJob.cs`: generic form of `DeleteTournamentFilesJob`, keeping its Hangfire `List<T>` workaround.

```csharp
using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;

namespace Neba.Api.Uploads;

/// <summary>
/// Deletes blobs that no entity points at any more.
/// </summary>
public sealed record DeleteStoredFilesJob
    : IBackgroundJob
{
    /// <remarks>
    /// Copied into a <see cref="List{T}"/> because Hangfire's JSON serializer records the runtime type,
    /// and it cannot rebuild the compiler-generated type a collection expression produces.
    /// </remarks>
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    [SuppressMessage("Style", "IDE0306:Use collection expression for new", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    public required IReadOnlyCollection<BlobReference> Files
    {
        get;
        init => field = new List<BlobReference>(value);
    }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteStoredFilesJob)}: {Files.Count} file(s)";
}

/// <summary>
/// The container and path of a blob.
/// </summary>
public sealed record BlobReference
{
    public required string Container { get; init; }

    public required string Path { get; init; }
}
```

`DeleteStoredFilesJobHandler.cs`: copy of `DeleteTournamentFilesJobHandler` with the log messages reworded ("Deleted stored file ...", "Failed to delete stored file ...") and the `DeleteStoredFilesJobLogMessages` class. The Scrutor scan in `BackgroundJobConfiguration.cs` registers it; no DI edit.

`StoredFileCleanup.cs`:

```csharp
using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Uploads;

internal static class StoredFileCleanup
{
    /// <summary>
    /// Queues deletion of the previous file when the entity no longer points at it.
    /// Files are the same when container and path match; content type and size do not count.
    /// </summary>
    public static void ScheduleDeleteOfReplaced(
        IBackgroundJobScheduler backgroundJobScheduler,
        StoredFile? previous,
        StoredFile? current)
    {
        if (previous is null || StoredFileClaim.IsSameFile(previous, current))
        {
            return;
        }

        backgroundJobScheduler.Enqueue(new DeleteStoredFilesJob
        {
            Files = [new BlobReference { Container = previous.Container, Path = previous.Path }]
        });
    }
}
```

### Application: upload (`Neba.Api/Features/HallOfFame/UploadHallOfFamePhoto`)

`UploadHallOfFamePhotoEndpoint.cs`:

```csharp
internal sealed class UploadHallOfFamePhotoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService)
    : Endpoint<UploadHallOfFamePhotoRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("photos");
        Group<HallOfFameEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditHallOfFamePhoto.PolicyName);

        Description(description => description
            .WithName("UploadHallOfFamePhoto")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(UploadHallOfFamePhotoRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "bowlneba-public", "hall-of-fame/photo", null, ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            FileName = req.File.FileName,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes,
            Url = fileStorageService.GetBlobUri(storedFile.Container, storedFile.Path)
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

`UploadHallOfFamePhotoRequestValidator.cs`: same as `UploadTournamentLogoRequestValidator` with `MaxSizeMb = 5`, the four image types, error codes prefixed `UploadHallOfFamePhotoRequest.`, messages "Photo must be JPEG, PNG, WebP, or GIF." and "Photo must not exceed 5 MB.".

`UploadHallOfFamePhotoSummary.cs`: `Summary = "Uploads a Hall of Fame photo."`; description says the pointer is swept unless included in a later `EditHallOfFamePhoto`; responses 200, 400, 401, 403 (permission `HallOfFame.EditHallOfFamePhoto`).

### Application: edit (`Neba.Api/Features/HallOfFame/EditHallOfFamePhoto`)

`EditHallOfFamePhotoCommand.cs`:

```csharp
internal sealed record EditHallOfFamePhotoCommand
    : ICommand<Updated>
{
    public required HallOfFameId HallOfFameId { get; init; }

    public StoredFile? Photo { get; init; }

    public int? FocalX { get; init; }

    public int? FocalY { get; init; }

    public int? Zoom { get; init; }
}
```

`EditHallOfFamePhotoCommandHandler.cs`:

```csharp
internal sealed class EditHallOfFamePhotoCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache,
        IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<EditHallOfFamePhotoCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditHallOfFamePhotoCommand command, CancellationToken cancellationToken)
    {
        var induction = await appDbContext.HallOfFameInductions
            .SingleOrDefaultAsync(i => i.Id == command.HallOfFameId, cancellationToken);

        if (induction is null)
        {
            return HallOfFameErrors.InductionNotFound(command.HallOfFameId);
        }

        var previousPhoto = induction.Photo;

        var applied = await ApplyAsync(induction, command, previousPhoto, cancellationToken);
        if (applied.IsError)
        {
            return applied.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:hall-of-fame:inductions", token: cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousPhoto, induction.Photo);

        return Result.Updated;
    }

    private async Task<ErrorOr<Success>> ApplyAsync(
        HallOfFameInduction induction,
        EditHallOfFamePhotoCommand command,
        StoredFile? previousPhoto,
        CancellationToken cancellationToken)
    {
        if (command.Photo is null)
        {
            // No photo and no framing: remove. Framing alone keeps the current photo and changes the framing.
            if (command.FocalX is null || command.FocalY is null || command.Zoom is null)
            {
                induction.ClearPhoto();
                return Result.Success;
            }

            return induction.SetFocalPoint(command.FocalX.Value, command.FocalY.Value, command.Zoom.Value);
        }

        // Re-save of the current photo: only the framing can change.
        if (StoredFileClaim.IsSameFile(command.Photo, previousPhoto))
        {
            var current = induction.PhotoFocalPoint ?? PhotoFocalPoint.Default;
            return induction.SetFocalPoint(
                command.FocalX ?? current.X,
                command.FocalY ?? current.Y,
                command.Zoom ?? current.Zoom);
        }

        var claim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Photo, previousPhoto, HallOfFameErrors.PhotoNotUploaded, cancellationToken);
        if (claim.IsError)
        {
            return claim.Errors;
        }

        var result = induction.SetPhoto(
            command.Photo,
            command.FocalX ?? PhotoFocalPoint.Default.X,
            command.FocalY ?? PhotoFocalPoint.Default.Y,
            command.Zoom ?? PhotoFocalPoint.Default.Zoom);
        if (result.IsError)
        {
            return result.Errors;
        }

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Photo, cancellationToken);
        return Result.Success;
    }
}
```

`EditHallOfFamePhotoEndpoint.cs`:

```csharp
internal sealed class EditHallOfFamePhotoEndpoint(Messaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated> commandHandler)
    : Endpoint<EditHallOfFamePhotoRequest>
{
    public override void Configure()
    {
        Put("{id}/photo");
        Group<HallOfFameEndpointGroup>();

        Options(options => options
            .WithVersionSet("HallOfFame")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditHallOfFamePhoto.PolicyName);

        Description(description => description
            .WithName("EditHallOfFamePhoto")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditHallOfFamePhotoRequest req, CancellationToken ct)
    {
        var photo = req.Input.Photo;

        var command = new EditHallOfFamePhotoCommand
        {
            HallOfFameId = new HallOfFameId(req.Id),
            Photo = photo is null
                ? null
                : new StoredFile
                {
                    Container = photo.Container,
                    Path = photo.Path,
                    ContentType = photo.ContentType,
                    SizeInBytes = photo.SizeInBytes
                },
            FocalX = req.Input.FocalX,
            FocalY = req.Input.FocalY,
            Zoom = req.Input.Zoom
        };

        var result = await commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(
                result.FirstError.Type == ErrorType.Conflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status422UnprocessableEntity,
                ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

The Tournaments mapper and result sender stay in Tournaments (the feature-boundary architecture test forbids reaching across features), so the small mapping lives here. Verify the `HallOfFameId(string)` constructor against `TournamentId` when coding; use `HallOfFameId.Parse` if it differs.

`EditHallOfFamePhotoRequestValidator.cs` (structure only):

```csharp
internal sealed class EditHallOfFamePhotoRequestValidator
    : Validator<EditHallOfFamePhotoRequest>
{
    public EditHallOfFamePhotoRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.IdRequired").WithMessage("Id is required.")
            .Length(26).WithErrorCode("EditHallOfFamePhotoRequest.IdInvalidLength").WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Input)
            .NotNull().WithErrorCode("EditHallOfFamePhotoRequest.InputRequired").WithMessage("Input is required.");

        When(r => r.Input is not null, () =>
        {
            RuleFor(r => r.Input.FocalX)
                .InclusiveBetween(PhotoFocalPoint.Min, PhotoFocalPoint.Max)
                .WithErrorCode("EditHallOfFamePhotoRequest.FocalXOutOfRange").WithMessage("FocalX must be between 0 and 100.")
                .When(r => r.Input.FocalX.HasValue);

            RuleFor(r => r.Input.FocalY)
                .InclusiveBetween(PhotoFocalPoint.Min, PhotoFocalPoint.Max)
                .WithErrorCode("EditHallOfFamePhotoRequest.FocalYOutOfRange").WithMessage("FocalY must be between 0 and 100.")
                .When(r => r.Input.FocalY.HasValue);

            RuleFor(r => r.Input.Zoom)
                .InclusiveBetween(PhotoFocalPoint.MinZoom, PhotoFocalPoint.MaxZoom)
                .WithErrorCode("EditHallOfFamePhotoRequest.ZoomOutOfRange").WithMessage("Zoom must be between 100 and 300.")
                .When(r => r.Input.Zoom.HasValue);

            RuleFor(r => r.Input)
                .Must(input => input.FocalX.HasValue == input.FocalY.HasValue && input.FocalY.HasValue == input.Zoom.HasValue)
                .WithErrorCode("EditHallOfFamePhotoRequest.FramingIncomplete").WithMessage("FocalX, FocalY, and Zoom must all be set or all be omitted.");
        });

        When(r => r.Input?.Photo is not null, () =>
        {
            RuleFor(r => r.Input.Photo!.Container)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoContainerRequired").WithMessage("Photo container is required.");

            RuleFor(r => r.Input.Photo!.Path)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoPathRequired").WithMessage("Photo path is required.");

            RuleFor(r => r.Input.Photo!.ContentType)
                .NotEmpty().WithErrorCode("EditHallOfFamePhotoRequest.PhotoContentTypeRequired").WithMessage("Photo content type is required.");

            RuleFor(r => r.Input.Photo!.SizeInBytes)
                .GreaterThan(0).WithErrorCode("EditHallOfFamePhotoRequest.PhotoSizeInvalid").WithMessage("Photo size must be greater than zero.");
        });
    }
}
```

`EditHallOfFamePhotoSummary.cs`: modeled on `EditTournamentLogoSummary`; example request `Id = "01JZ8K3M4N5P6Q7R8S9T0V1W2X"`, photo `Container = "bowlneba-public"`, `Path = "hall-of-fame/photo/01JZ8K3M4N5P6Q7R8S9T0V1W2X.jpg"`, `FocalX = 42`, `FocalY = 28`, `Zoom = 150`. Responses: 204, 400 (structure), 401, 403 (`HallOfFame.EditHallOfFamePhoto`), 404, 409 (focal point on an induction with no photo, sent as `Photo: null` plus a focal point), 422 (photo not uploaded through the upload endpoint).

### Application: list (edit existing files)

`ListHallOfFameInductions/HallOfFameInductionDto.cs`: add primitives (cached DTO).

```csharp
/// <summary>
/// The ULID string identifying the induction.
/// </summary>
public required string Id { get; init; }

/// <summary>
/// Horizontal focal point of the photo, 0 to 100; null when none is set.
/// </summary>
public int? PhotoFocalX { get; init; }

/// <summary>
/// Vertical focal point of the photo, 0 to 100; null when none is set.
/// </summary>
public int? PhotoFocalY { get; init; }

/// <summary>
/// Zoom about the focal point, 100 to 300 percent; null when none is set.
/// </summary>
public int? PhotoZoom { get; init; }
```

`ListHallOfFameInductionsQueryHandler.cs`: extend the projection and the mapping.

```csharp
.Select(induction => new
{
    induction.Id,
    induction.Year,
    BowlerName = induction.Bowler.Name,
    induction.Categories,
    PhotoContainer = induction.Photo != null ? induction.Photo.Container : null,
    PhotoPath = induction.Photo != null ? induction.Photo.Path : null,
    PhotoFocalX = induction.PhotoFocalPoint != null ? (int?)induction.PhotoFocalPoint.X : null,
    PhotoFocalY = induction.PhotoFocalPoint != null ? (int?)induction.PhotoFocalPoint.Y : null,
    PhotoZoom = induction.PhotoFocalPoint != null ? (int?)induction.PhotoFocalPoint.Zoom : null
})
...
Id = row.Id.ToString(),
PhotoFocalX = row.PhotoFocalX,
PhotoFocalY = row.PhotoFocalY,
PhotoZoom = row.PhotoZoom,
```

`ListHallOfFameInductionsEndpoint.cs`: add `Id = i.Id, PhotoFocalX = i.PhotoFocalX, PhotoFocalY = i.PhotoFocalY, PhotoZoom = i.PhotoZoom` to the response mapping. `ListHallOfFameInductionsSummary.cs`: add `Id` to both examples and `PhotoFocalX = 42, PhotoFocalY = 28, PhotoZoom = 150` to the one with a photo.

### Authorization

`Security/Infrastructure/SecurityRoleSeeder.cs`: add `Permissions.EditHallOfFamePhoto` to three roles.

```csharp
[Roles.Webmaster] =
[
    ...
    Permissions.EditSponsor,

    Permissions.EditHallOfFamePhoto,

    Permissions.CreateTournament,
    ...
],

[Roles.Manager] =
[
    ...
    Permissions.EditSponsor,

    Permissions.EditHallOfFamePhoto,

    Permissions.CreateTournament,
    ...
],

[Roles.Journalist] =
[
    Permissions.CreateArticle,
    Permissions.EditArticle,
    Permissions.DeleteArticle,

    Permissions.EditHallOfFamePhoto
],
```

`docs/policies/README.md`: run `/policy-documentation EditHallOfFamePhoto` for the row.

### Sync

No code change in `Legacy/HallOfFame/HallOfFame.cs`: `SyncRowAsync` only calls `ApplyLegacyCategory` and never touches `Photo` or `PhotoFocalPoint`.

### Test factories (`tests/Neba.TestFactory`)

```csharp
// HallOfFame/HallOfFameInductionFactory.cs: Create() unchanged in behavior (no photo, no focal point).
// New optional parameters; Photo is set through the aggregate, not an initializer.
public static HallOfFameInduction Create(
    HallOfFameId? id = null,
    int? year = null,
    BowlerId? bowlerId = null,
    IReadOnlyCollection<HallOfFameCategory>? categories = null,
    StoredFile? photo = null,
    int? photoFocalX = null,
    int? photoFocalY = null,
    int? photoZoom = null)
{
    var induction = new HallOfFameInduction { /* existing defaults */ };

    if (photo is not null)
    {
        induction.SetPhoto(
            photo,
            photoFocalX ?? PhotoFocalPoint.Default.X,
            photoFocalY ?? PhotoFocalPoint.Default.Y,
            photoZoom ?? PhotoFocalPoint.Default.Zoom)
            .IsError.ShouldBeFalse();
    }

    return induction;
}

// HallOfFame/HallOfFameInductionDtoFactory.cs and HallOfFameInductionResponseFactory.cs:
// add string? id (default a const ULID), int? photoFocalX, int? photoFocalY, int? photoZoom; Bogus() generates distinct ids.

// HallOfFame/PhotoFocalPointFactory.cs (new)
public static PhotoFocalPoint Create(int? x = null, int? y = null, int? zoom = null)
    => new() { X = x ?? DefaultX, Y = y ?? DefaultY, Zoom = zoom ?? DefaultZoom };

// HallOfFame/EditHallOfFamePhotoCommandFactory.cs (new): Create(HallOfFameId? id, StoredFile? photo, int? focalX, int? focalY, int? zoom)
// HallOfFame/EditHallOfFamePhotoRequestFactory.cs (new): Create(string? id, HallOfFamePhotoInput? photo, int? focalX, int? focalY, int? zoom)
```

`Create()` with no arguments still returns a persistable induction (no photo). A `WithPhoto` default uses `StoredFileFactory.Create()`.

### Tests (`tests/Neba.Api.Tests`)

Each test has `[UnitTest]` or `[IntegrationTest]`, `[Component("HallOfFame")]` (or `Uploads`/`Security`), a `DisplayName`, AAA comments, strict mocks.

- `Features/HallOfFame/Domain/PhotoFocalPointTests.cs`: `Create` accepts 0 and 100 on each axis and zoom 100 and 300; rejects -1 and 101 on each axis and zoom 99 and 301.
- `Features/HallOfFame/Domain/HallOfFameInductionTests.cs`:
  - `SetPhoto` sets photo and focal point; rejects an out-of-range point and leaves the induction unchanged.
  - `ClearPhoto` clears both; calling it with no photo does nothing.
  - `SetFocalPoint` changes the point and zoom; returns `NoPhoto` (Conflict) without a photo; rejects out of range.
- `Features/HallOfFame/EditHallOfFamePhoto/EditHallOfFamePhotoCommandHandlerTests.cs`:
  - Sets a new photo from a pending upload (center default; pending record removed; list cache evicted; no delete job).
  - Replaces a photo (old blob delete job enqueued with the old container and path).
  - Clears a photo (delete job enqueued; focal point cleared).
  - Clears when already clear (no delete job, still 204-shaped success).
  - `Photo: null` with a focal point moves the point and keeps the photo (no delete job); with no photo on the induction it returns `HallOfFame.Photo.Missing`.
  - Same file with new framing (framing only; stored content type and size kept; no delete job).
  - Same file with no framing keeps the current framing.
  - Not found returns `HallOfFame.NotFound`.
  - A file that is not pending returns `HallOfFame.Photo.NotUploaded`.
- `Features/HallOfFame/EditHallOfFamePhoto/EditHallOfFamePhotoRequestValidatorTests.cs`: id length; focal range per axis; zoom range; all-three-or-none; photo field rules (a focal point without a photo is valid).
- `Features/HallOfFame/EditHallOfFamePhoto/EditHallOfFamePhotoEndpointTests.cs`: maps request to command; 204; 404; 409; 422.
- `Features/HallOfFame/EditHallOfFamePhoto/EditHallOfFamePhotoEndpointAuthorizationTests.cs`: 401 without a token, 403 without the permission, 204 with it. Same for `UploadHallOfFamePhoto/UploadHallOfFamePhotoEndpointAuthorizationTests.cs`.
- `Features/HallOfFame/UploadHallOfFamePhoto/UploadHallOfFamePhotoEndpointTests.cs`, `...RequestValidatorTests.cs`, `...SummaryTests.cs`: mirror the tournament logo versions.
- `Features/HallOfFame/ListHallOfFameInductions/*`: handler returns `Id` and the focal fields (null without a photo); endpoint maps them.
- `Database/Configurations/HallOfFameInductionConfigurationTests.cs`: columns `photo_focal_x`, `photo_focal_y`, `photo_zoom`, nullable; a row round-trips a focal point; a row with no focal point reads back null.
- `Legacy/HallOfFame/HallOfFameTests.cs`: sync over an induction that has a website photo and focal point leaves both unchanged and still adds the new category.
- `Uploads/StoredFileClaimTests.cs`, `Uploads/PendingUploadCleanerTests.cs`, `Uploads/StoredFileCleanupTests.cs`, `Uploads/DeleteStoredFilesJobHandlerTests.cs`: port from `TournamentLogoClaimTests`, `TournamentLogoClaimEnsureClaimableTests`, `TournamentPendingUploadCleanerTests`, `TournamentLogoCleanupTests`, and the `DeleteTournamentFilesJobHandler` tests, parameterised on the error.
- `Security/Infrastructure/SecurityRoleSeederTests.cs`: Webmaster, Manager, Journalist create and already-synced cases include `EditHallOfFamePhoto`; update the DisplayName lists; TournamentDirector expectations unchanged.

### Docs and e2e (part of the API PR)

- `docs/plans/shared-upload-migration.md` (new, written with the code): a step list for the tech-debt branch.
  1. Tournaments: replace `TournamentLogoClaim`, `TournamentLogoCleanup`, `TournamentPendingUploadCleaner` with `StoredFileClaim` (pass `TournamentErrors.LogoNotUploaded`), `StoredFileCleanup`, `PendingUploadCleaner` in `CreateTournamentCommandHandler`, `EditTournamentCommandHandler`, `EditTournamentLogoCommandHandler`, and `DeleteTournamentCommandHandler`; replace `DeleteTournamentFilesJob`, its handler, and `TournamentFileReference` with `DeleteStoredFilesJob` and `BlobReference`; port and delete the matching tests.
  2. Sponsors: replace `SponsorPendingUploadCleaner`, `DeleteSponsorFilesJob`, its handler, and `StoredFileReference` in `CreateSponsorCommandHandler` and `EditSponsorCommandHandler`.
  3. News: replace `DeleteArticleFilesJob` and its handler in `DeleteArticleCommandHandler`, `CreateArticleCommandHandler`, `EditArticleCommandHandler`.
  4. Confirm nothing is queued in Hangfire under the old job type names before deleting them.
  5. Run the Tournaments, Sponsors, News, and Uploads test components.
- `docs/ubiquitous-language.md`: add "Focal Point". `docs/policies/README.md`: permission row.
- `tests/e2e/mock-api/mock-api-server.ts`: add `id` to each mocked induction (and optional `photoFocalX`/`photoFocalY`/`photoZoom` for a mocked photo) because `HallOfFameInductionResponse.Id` is `required`.

### Deliberately deferred

- Migrating Tournaments, Sponsors, and News onto the shared pieces (tech-debt branch after merge).
- Adding or editing inductees; news header images.
- All UI (Phase 2).

## Phase 2: UI

Paths are relative to `src/Neba.Website.Server/` unless noted. The Hall of Fame page already has `<PageTitle>` and `@rendermode InteractiveServer` (prerender off); both stay. Razor `@code` follows the repo rules: no `$"{x}"` interpolation, `@` on component attribute values, `[Parameter, EditorRequired]` instead of `required`. String building that needs interpolation lives in `.cs` files.

The mockup (`mockups/hall-of-fame-photo/edit-photo-modal.html`) is the source for markup, classes, and the picker's behavior.

### Components (shared, `Components/`)

`Components/FocalPoint.cs` (new): the value the picker reports, plus the one place that turns it into CSS. The avatar, the modal preview, and the picker all use `ToImageStyle()`, so they cannot drift apart.

```csharp
using System.Globalization;

namespace Neba.Website.Server.Components;

/// <summary>
/// Where a photo is framed: the CSS <c>object-position</c> percents (X, Y, 0 to 100) and a zoom in percent
/// (100 is none, 300 is the most). The zoom scales the photo about the same point.
/// </summary>
public readonly record struct FocalPoint(int X, int Y, int Zoom)
{
    /// <summary>The middle of the photo, no zoom.</summary>
    public static FocalPoint Default => new(50, 50, 100);

    /// <summary>
    /// Inline style for an <c>&lt;img&gt;</c> with <c>object-fit: cover</c> inside an <c>overflow: hidden</c> shape.
    /// </summary>
    public string ToImageStyle()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"object-position:{X}% {Y}%;transform-origin:{X}% {Y}%;transform:scale({Zoom / 100.0})");
}

/// <summary>
/// The shape a photo is shown in.
/// </summary>
public enum FocalPointShape
{
    Circle,
    Rectangle
}
```

`Components/FocalPointPicker.razor` (new):

```razor
@implements IAsyncDisposable
@inject IJSRuntime JsRuntime

<div id="@_rootId" class="focal-picker" data-shape="@(Shape == FocalPointShape.Circle ? "circle" : "rectangle")" data-aspect="@AspectText">
    <div class="focal-picker__stage">
        <div class="focal-picker__surface" data-fp-surface tabindex="0" role="slider"
             aria-label="@AriaLabel" aria-valuemin="0" aria-valuemax="100" aria-valuenow="@X"
             aria-valuetext="@ValueText">
            <img src="@ImageUrl.AbsoluteUri" alt="" draggable="false" />
            <span class="focal-picker__crop" data-fp-crop aria-hidden="true"></span>
            <span class="focal-picker__marker" data-fp-marker aria-hidden="true"></span>
        </div>
        <p class="focal-picker__hint">@Hint</p>
    </div>

    <div class="focal-picker__side">
        <div class="focal-picker__preview @(Shape == FocalPointShape.Circle ? "focal-picker__preview--circle" : null)"
             style="@PreviewAspectStyle">
            <img src="@ImageUrl.AbsoluteUri" alt="" style="@Current.ToImageStyle()" />
        </div>

        <div class="focal-picker__zoom">
            <label for="@ZoomId">Zoom <output>@ZoomText</output></label>
            <input id="@ZoomId" type="range" min="100" max="300" step="5" value="@Zoom" data-fp-zoom />
        </div>

        @SideContent
    </div>
</div>

@code {
    private readonly string _rootId = "focal-picker-" + Guid.NewGuid().ToString("N");
    private string ZoomId => _rootId + "-zoom";
    private DotNetObjectReference<FocalPointPicker>? _dotNetHelper;
    private IJSObjectReference? _jsModule;
    private FocalPoint _lastSentToJs;

    [Parameter, EditorRequired]
    public Uri ImageUrl { get; set; } = default!;

    [Parameter]
    public int X { get; set; } = 50;

    [Parameter]
    public int Y { get; set; } = 50;

    [Parameter]
    public int Zoom { get; set; } = 100;

    [Parameter]
    public FocalPointShape Shape { get; set; } = FocalPointShape.Circle;

    /// <summary>
    /// Width divided by height of the shape. Ignored for a circle.
    /// </summary>
    [Parameter]
    public double Aspect { get; set; } = 1;

    [Parameter, EditorRequired]
    public string AriaLabel { get; set; } = string.Empty;

    [Parameter]
    public string Hint { get; set; } =
        "Click the face to center it in the circle, then drag the circle to fine-tune. If the face sits near the edge, zoom in first. Arrow keys also work; hold Shift to move further.";

    /// <summary>
    /// Extra content under the preview and zoom (state label, remove button).
    /// </summary>
    [Parameter]
    public RenderFragment? SideContent { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<FocalPoint> OnChange { get; set; }

    private FocalPoint Current => new(X, Y, Zoom);

    private string AspectText
        => Shape == FocalPointShape.Circle ? "1" : Aspect.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    private string PreviewAspectStyle
        => Shape == FocalPointShape.Circle ? string.Empty : "aspect-ratio:" + AspectText;

    private string ZoomText
        => (Zoom / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "×";

    private string ValueText
        => "Photo positioned " + X + " percent across and " + Y + " percent down, zoom " + ZoomText;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./Components/FocalPointPicker.razor.js");
            await _jsModule.InvokeVoidAsync("initialize", _rootId, _dotNetHelper, X, Y, Zoom);
            _lastSentToJs = Current;
            return;
        }

        // The parent changed the value (new photo staged, discard): tell the module, which owns the geometry.
        if (_jsModule is not null && Current != _lastSentToJs)
        {
            _lastSentToJs = Current;
            await _jsModule.InvokeVoidAsync("update", _rootId, X, Y, Zoom);
        }
    }

    /// <summary>
    /// Called by the module after a click, drag, key press, or zoom change. At most once per animation frame.
    /// </summary>
    [JSInvokable]
    public async Task NotifyChanged(int x, int y, int zoom)
    {
        _lastSentToJs = new FocalPoint(x, y, zoom);
        await OnChange.InvokeAsync(_lastSentToJs);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("dispose", _rootId);
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone — nothing to clean up client-side.
            }

            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone — nothing to clean up client-side.
            }
        }

        _dotNetHelper?.Dispose();
    }
}
```

`Components/FocalPointPicker.razor.js` (new). The math is exported so the JS tests can call it. Geometry (the photo's aspect ratio, the crop outline) lives here because it needs the image's natural size; Razor never renders `style` on the surface, crop, or marker, so Blazor re-renders do not overwrite what the module sets.

```javascript
const instances = new Map();

export const clamp = (value) => Math.max(0, Math.min(100, value));

/** Size of the visible window as a fraction of the photo's width and height. Zooming in shrinks it. */
export function cropSize(photoAspect, shapeAspect, zoom) {
  const zoomFactor = zoom / 100;
  return {
    cw: Math.min(1, shapeAspect / photoAspect) / zoomFactor,
    ch: Math.min(1, photoAspect / shapeAspect) / zoomFactor,
  };
}

/** Center of the visible window, as a fraction of the photo, for object-position percents x and y. */
export function centerOf(x, y, size) {
  return {
    cx: size.cw / 2 + (x / 100) * (1 - size.cw),
    cy: size.ch / 2 + (y / 100) * (1 - size.ch),
  };
}

/**
 * The object-position percents that put the window's center at (cx, cy), as near as the photo allows.
 * An axis with no overflow cannot move and stays at 50.
 */
export function positionFor(cx, cy, size) {
  return {
    x: size.cw < 1 ? clamp(Math.round(((cx - size.cw / 2) / (1 - size.cw)) * 100)) : 50,
    y: size.ch < 1 ? clamp(Math.round(((cy - size.ch / 2) / (1 - size.ch)) * 100)) : 50,
  };
}

export function initialize(rootId, dotNetHelper, x, y, zoom) {
  const root = document.getElementById(rootId);
  if (!root) {
    return;
  }

  const surface = root.querySelector('[data-fp-surface]');
  const image = surface.querySelector('img');
  const instance = {
    root,
    surface,
    image,
    crop: surface.querySelector('[data-fp-crop]'),
    marker: surface.querySelector('[data-fp-marker]'),
    slider: root.querySelector('[data-fp-zoom]'),
    dotNetHelper,
    x,
    y,
    zoom,
    photoAspect: image.naturalWidth > 0 ? image.naturalWidth / image.naturalHeight : 4 / 3,
    shapeAspect: Number(root.dataset.aspect) || 1,
    isCircle: root.dataset.shape === 'circle',
    drag: null,
    notifyQueued: false,
    cleanups: [],
  };

  const on = (target, type, handler) => {
    target.addEventListener(type, handler);
    instance.cleanups.push(() => target.removeEventListener(type, handler));
  };

  on(image, 'load', () => {
    instance.photoAspect = image.naturalWidth / image.naturalHeight;
    render(instance);
  });
  on(surface, 'pointerdown', (event) => pointerDown(instance, event));
  on(surface, 'pointermove', (event) => pointerMove(instance, event));
  on(surface, 'pointerup', (event) => endDrag(instance, event));
  on(surface, 'pointercancel', (event) => endDrag(instance, event));
  on(surface, 'pointerleave', () => {
    if (!instance.drag) {
      surface.classList.remove('focal-picker__surface--over-crop');
    }
  });
  on(surface, 'keydown', (event) => keyDown(instance, event));
  on(instance.slider, 'input', (event) => zoomChanged(instance, Number(event.target.value)));

  instances.set(rootId, instance);
  render(instance);
}

export function update(rootId, x, y, zoom) {
  const instance = instances.get(rootId);
  if (!instance) {
    return;
  }

  instance.x = x;
  instance.y = y;
  instance.zoom = zoom;
  render(instance);
}

export function dispose(rootId) {
  const instance = instances.get(rootId);
  if (!instance) {
    return;
  }

  instance.cleanups.forEach((cleanup) => cleanup());
  instances.delete(rootId);
}

function size(instance) {
  return cropSize(instance.photoAspect, instance.shapeAspect, instance.zoom);
}

function render(instance) {
  const crop = size(instance);
  const { cx, cy } = centerOf(instance.x, instance.y, crop);

  instance.surface.style.setProperty('--fp-aspect', String(instance.photoAspect));
  instance.crop.style.width = `${crop.cw * 100}%`;
  instance.crop.style.height = `${crop.ch * 100}%`;
  instance.crop.style.left = `${cx * 100}%`;
  instance.crop.style.top = `${cy * 100}%`;
  instance.crop.style.borderRadius = instance.isCircle ? '50%' : '4px';
  instance.marker.style.left = `${cx * 100}%`;
  instance.marker.style.top = `${cy * 100}%`;
  instance.slider.value = String(instance.zoom);
}

/** Sends the latest value to .NET at most once per animation frame, so a fast drag does not flood SignalR. */
function notify(instance) {
  if (instance.notifyQueued) {
    return;
  }

  instance.notifyQueued = true;
  requestAnimationFrame(() => {
    instance.notifyQueued = false;
    instance.dotNetHelper.invokeMethodAsync('NotifyChanged', instance.x, instance.y, instance.zoom);
  });
}

function photoPoint(instance, event) {
  const rect = instance.surface.getBoundingClientRect();
  return { fx: (event.clientX - rect.left) / rect.width, fy: (event.clientY - rect.top) / rect.height };
}

function insideCrop(instance, fx, fy) {
  const crop = size(instance);
  const { cx, cy } = centerOf(instance.x, instance.y, crop);
  const nx = (fx - cx) / (crop.cw / 2);
  const ny = (fy - cy) / (crop.ch / 2);
  return instance.isCircle ? nx * nx + ny * ny <= 1 : Math.abs(nx) <= 1 && Math.abs(ny) <= 1;
}

function moveCenterTo(instance, cx, cy) {
  const next = positionFor(cx, cy, size(instance));
  if (next.x === instance.x && next.y === instance.y) {
    return;
  }

  instance.x = next.x;
  instance.y = next.y;
  render(instance);
  notify(instance);
}

// A click jumps the window to that point; pressing on the window and dragging moves it from where it was
// grabbed, so it does not jump under the pointer.
function pointerDown(instance, event) {
  if (event.button !== 0) {
    return;
  }

  const { fx, fy } = photoPoint(instance, event);
  if (insideCrop(instance, fx, fy)) {
    const { cx, cy } = centerOf(instance.x, instance.y, size(instance));
    instance.drag = { dx: cx - fx, dy: cy - fy };
  } else {
    instance.drag = { dx: 0, dy: 0 };
    moveCenterTo(instance, fx, fy);
  }

  instance.surface.setPointerCapture(event.pointerId);
  instance.surface.classList.add('focal-picker__surface--dragging');
  instance.surface.focus({ preventScroll: true });
}

function pointerMove(instance, event) {
  const { fx, fy } = photoPoint(instance, event);
  if (!instance.drag) {
    instance.surface.classList.toggle('focal-picker__surface--over-crop', insideCrop(instance, fx, fy));
    return;
  }

  moveCenterTo(instance, fx + instance.drag.dx, fy + instance.drag.dy);
}

function endDrag(instance, event) {
  if (!instance.drag) {
    return;
  }

  instance.drag = null;
  instance.surface.classList.remove('focal-picker__surface--dragging');
  if (instance.surface.hasPointerCapture(event.pointerId)) {
    instance.surface.releasePointerCapture(event.pointerId);
  }
}

const KEY_DIRECTIONS = {
  ArrowLeft: [-1, 0],
  ArrowRight: [1, 0],
  ArrowUp: [0, -1],
  ArrowDown: [0, 1],
};

function keyDown(instance, event) {
  const direction = KEY_DIRECTIONS[event.key];
  if (!direction) {
    return;
  }

  event.preventDefault();
  const step = event.shiftKey ? 5 : 1;
  const crop = size(instance);
  if (crop.cw < 1) {
    instance.x = clamp(instance.x + direction[0] * step);
  }

  if (crop.ch < 1) {
    instance.y = clamp(instance.y + direction[1] * step);
  }

  render(instance);
  notify(instance);
}

// Zooming keeps the center of the window where it is, then re-derives the position percents.
function zoomChanged(instance, zoom) {
  const before = centerOf(instance.x, instance.y, size(instance));
  instance.zoom = zoom;
  const next = positionFor(before.cx, before.cy, size(instance));
  instance.x = next.x;
  instance.y = next.y;
  render(instance);
  notify(instance);
}
```

`Components/FocalPointPicker.razor.css` (new, scoped): port the picker rules from the mockup `<style>` block, renamed to BEM:

| Mockup | Component |
| --- | --- |
| `.picker` | `.focal-picker` (grid `1fr 160px`, one column at 640px) |
| `.pick-surface` | `.focal-picker__surface` (`width: min(100%, calc(320px * var(--fp-aspect, 1.3333)))`, `aspect-ratio: var(--fp-aspect, 1.3333)`, `touch-action: none`, `cursor: crosshair`; `--over-crop` grab, `--dragging` grabbing and no transitions) |
| `.crop`, `.marker` | `.focal-picker__crop`, `.focal-picker__marker` (the crop uses `box-shadow: 0 0 0 999px rgba(5,39,103,.5)` to dim the rest; both honor `prefers-reduced-motion`) |
| `.readout` | `.focal-picker__hint` |
| `.side`, `.preview`, `.zoom` | `.focal-picker__side`, `.focal-picker__preview` (`overflow: hidden`; `--circle` adds `border-radius: 50%` and a 132px square), `.focal-picker__zoom` |

Plus `.focal-picker__preview img { width: 100%; height: 100%; object-fit: cover; display: block; }`, a visible `:focus-visible` ring on the surface, and the phone layout from the mockup (`grid-template-areas: "pv tx" "zm zm"`, 96px preview). Colors use the existing `--neba-*` tokens.

### Hall of Fame (`HallOfFame/`)

`HallOfFameInductionViewModel.cs` (edit): add three members and `Id`.

```csharp
/// <summary>
/// The ULID string identifying the induction. Used to address the induction's photo.
/// </summary>
public required string Id { get; init; }

/// <summary>
/// Saved framing of the photo; null when none is set (the page then centers the photo with no zoom).
/// </summary>
public int? PhotoFocalX { get; init; }

public int? PhotoFocalY { get; init; }

public int? PhotoZoom { get; init; }
```

`HallOfFameMappingExtensions.cs` (edit): add to the initializer.

```csharp
Id = dto.Id,
PhotoUri = dto.PhotoUri,
PhotoFocalX = dto.PhotoFocalX,
PhotoFocalY = dto.PhotoFocalY,
PhotoZoom = dto.PhotoZoom
```

`HallOfFameAvatar.razor` (new): replaces the two duplicated avatar blocks. `GetInitials` moves here from the page.

```razor
<div class="avatar-wrap">
    <div class="inductee-photo">
        @if (PhotoUri is not null)
        {
            <img class="inductee-photo__img" src="@PhotoUri.AbsoluteUri" alt="" style="@ImageStyle" loading="lazy" />
        }
        else
        {
            @GetInitials(BowlerName)
        }
    </div>

    @if (OnEdit.HasDelegate)
    {
        <HallOfFamePhotoEditButton BowlerName="@BowlerName" OnClick="OnEdit" />
    }
</div>

@code {
    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    [Parameter]
    public Uri? PhotoUri { get; set; }

    [Parameter]
    public int? FocalX { get; set; }

    [Parameter]
    public int? FocalY { get; set; }

    [Parameter]
    public int? Zoom { get; set; }

    /// <summary>
    /// When set, the avatar shows the edit button (visible only to users with the permission).
    /// </summary>
    [Parameter]
    public EventCallback OnEdit { get; set; }

    private string ImageStyle
        => new FocalPoint(FocalX ?? FocalPoint.Default.X, FocalY ?? FocalPoint.Default.Y, Zoom ?? FocalPoint.Default.Zoom)
            .ToImageStyle();

    internal static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            return string.Concat(parts[0][0], parts[^1][0]);
        }

        return parts.Length > 0 ? parts[0][0].ToString() : "?";
    }
}
```

`HallOfFameAvatar.razor.css` (new): move `.inductee-photo` (with `overflow: hidden`, gradient, 140px circle) out of `HallOfFame.razor.css`, add `.avatar-wrap { position: relative; margin-bottom: 0.75rem; }` (the margin moves from `.inductee-photo`) and `.inductee-photo__img { width: 100%; height: 100%; object-fit: cover; display: block; }`. In `HallOfFame.razor.css` the hover rule becomes `.inductee-item:hover ::deep .inductee-photo { transform: scale(1.05); }`, because the avatar is now a child component.

`HallOfFamePhotoEditButton.razor` (new): same pattern as `TournamentLogoEditButton`.

```razor
@using Neba.Api.Contracts.Security

<AuthorizeView Policy="@Permissions.EditHallOfFamePhoto.PolicyName">
    <Authorized>
        <button type="button" class="hof-photo-edit" aria-label="@("Edit photo for " + BowlerName)" @onclick="OnClick">
            <span class="material-symbols-outlined" aria-hidden="true">edit</span>
        </button>
    </Authorized>
</AuthorizeView>

@code {
    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public EventCallback OnClick { get; set; }
}
```

`HallOfFamePhotoEditButton.razor.css` (new): the mockup's `.photo-edit` rules as `.hof-photo-edit` (44px minimum, bottom-right of the avatar, white pill, focus ring).

`EditHallOfFamePhotoModal.razor` (new): feature-agnostic, like `EditTournamentLogoModal`. The host supplies the upload and save calls.

```razor
@using ErrorOr
@using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto
@using Neba.Api.Contracts.Uploads

<DirtyFormGuard IsDirty="@(IsOpen && IsDirty)" />

<ConfirmActionModal IsOpen="@_isCancelConfirmOpen"
                    Title="Discard unsaved changes?"
                    Message="You have unsaved changes. If you close this dialog now, they will be lost."
                    ConfirmLabel="Discard"
                    CancelLabel="Keep Editing"
                    OnConfirm="ConfirmCancelAsync"
                    OnCancel="@(() => _isCancelConfirmOpen = false)" />

<NebaModal IsOpen="@IsOpen" OnClose="@HandleCancelRequestedAsync" MaxWidth="640px" CompactSize="true"
           AriaLabelledById="edit-photo-title">
    <HeaderContent>
        <div>
            <h2 class="neba-modal-title" id="edit-photo-title">Edit photo</h2>
            <div class="ehp-subtitle">@BowlerName</div>
        </div>
    </HeaderContent>
    <ChildContent>
        @if (_errorMessage is not null)
        {
            <NebaAlert Severity="NotifySeverity.Error" Title="Couldn't Save Photo" Message="@_errorMessage"
                       Dismissible="true" OnDismiss="@(() => _errorMessage = null)" />
        }

        @if (ShownPhotoUrl is not null)
        {
            <FocalPointPicker ImageUrl="@ShownPhotoUrl"
                              X="@_point.X" Y="@_point.Y" Zoom="@_point.Zoom"
                              AriaLabel="@("Photo position for " + BowlerName)"
                              OnChange="HandlePointChanged">
                <SideContent>
                    @if (_staged is not null)
                    {
                        <span class="ehp-badge">New photo</span>
                    }
                    <p class="ehp-state">@(_staged is not null ? "New photo" : "Current photo")</p>
                    <p class="ehp-help">@(_staged is not null
                        ? "Replaces the current photo when you save."
                        : "This is how the photo shows on the Hall of Fame page.")</p>

                    @if (_staged is not null)
                    {
                        <button type="button" class="ehp-link-danger" @onclick="DiscardStaged">Discard new photo</button>
                    }
                    else
                    {
                        <button type="button" class="ehp-link-danger" @onclick="RemoveCurrent">Remove photo</button>
                    }
                </SideContent>
            </FocalPointPicker>
        }
        else
        {
            <div class="ehp-empty">
                <div class="ehp-empty__box">
                    <strong>@(_removed ? "Photo will be removed" : "No photo yet")</strong>
                    <span>@(_removed ? "The page shows initials after you save." : "The page shows initials until you add one.")</span>
                </div>
                <div class="ehp-empty__preview" aria-hidden="true">@HallOfFameAvatar.GetInitials(BowlerName)</div>
            </div>
        }

        <FileUpload @key="_uploadKey" MaxFiles="1" Accept="image/*" MaxFileSizeBytes="@(5 * 1024 * 1024)"
                    Label="@UploadLabel"
                    OnUploadRequestedAsync="OnUploadRequestedAsync"
                    OnFileUploaded="HandleFileUploaded"
                    OnFileRemoved="HandleFileRemoved"
                    OnBusyChanged="@(busy => _isUploading = busy)" />
    </ChildContent>
    <FooterContent>
        <div class="ehp-actions">
            <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancelRequestedAsync" disabled="@_isSubmitting">Cancel</button>
            <button type="button" class="neba-btn neba-btn-primary" @onclick="SubmitAsync"
                    disabled="@(!IsDirty || _isUploading || _isSubmitting)">
                @(_isSubmitting ? "Saving..." : "Save photo")
            </button>
        </div>
    </FooterContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    /// <summary>
    /// The induction's own photo, or null when it has none.
    /// </summary>
    [Parameter]
    public Uri? CurrentPhotoUrl { get; set; }

    [Parameter]
    public int? CurrentFocalX { get; set; }

    [Parameter]
    public int? CurrentFocalY { get; set; }

    [Parameter]
    public int? CurrentZoom { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSaved { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    [Parameter, EditorRequired]
    public Func<Stream, string, string, IProgress<int>, CancellationToken, Task<ErrorOr<UploadedFileResponse>>> OnUploadRequestedAsync { get; set; } = default!;

    /// <summary>
    /// Saves the photo. Both null removes the photo; a null photo with a framing keeps the current photo and
    /// changes its framing; a photo with a framing sets or replaces it. The host wires the real call.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<HallOfFamePhotoInput?, FocalPoint?, CancellationToken, Task<ErrorOr<Success>>> OnSubmitRequestedAsync { get; set; } = default!;

    private UploadedFileResponse? _staged;
    private bool _removed;
    private bool _wasOpen;
    private bool _isUploading;
    private bool _isSubmitting;
    private bool _isCancelConfirmOpen;
    private string? _errorMessage;
    private Guid _uploadKey = Guid.NewGuid();
    private FocalPoint _saved = FocalPoint.Default;
    private FocalPoint _point = FocalPoint.Default;

    private bool IsDirty
        => _staged is not null
            || _removed
            || (CurrentPhotoUrl is not null && _point != _saved);

    private Uri? ShownPhotoUrl
    {
        get
        {
            if (_staged is not null)
            {
                return _staged.Url;
            }

            return _removed ? null : CurrentPhotoUrl;
        }
    }

    private string UploadLabel
    {
        get
        {
            if (ShownPhotoUrl is null)
            {
                return "Upload a photo";
            }

            return _staged is null ? "Upload a replacement photo" : "Upload a different photo";
        }
    }

    protected override void OnParametersSet()
    {
        if (IsOpen && !_wasOpen)
        {
            ResetForm();
        }

        _wasOpen = IsOpen;
    }

    private void HandlePointChanged(FocalPoint point)
        => _point = point;

    private Task HandleFileUploaded(UploadedFileResponse upload)
    {
        _staged = upload;
        _removed = false;
        _point = FocalPoint.Default;
        return Task.CompletedTask;
    }

    private Task HandleFileRemoved(UploadedFileResponse removed)
    {
        _staged = null;
        _point = _saved;
        return Task.CompletedTask;
    }

    private void DiscardStaged()
    {
        _staged = null;
        _point = _saved;
        _uploadKey = Guid.NewGuid();
    }

    private void RemoveCurrent()
        => _removed = true;

    private Task HandleCancelRequestedAsync()
    {
        if (IsDirty)
        {
            _isCancelConfirmOpen = true;
            return Task.CompletedTask;
        }

        return CloseAsync();
    }

    private Task ConfirmCancelAsync()
    {
        _isCancelConfirmOpen = false;
        return CloseAsync();
    }

    private Task CloseAsync()
    {
        ResetForm();
        return OnClose.InvokeAsync();
    }

    private void ResetForm()
    {
        _staged = null;
        _removed = false;
        _errorMessage = null;
        _uploadKey = Guid.NewGuid();
        _saved = new FocalPoint(
            CurrentFocalX ?? FocalPoint.Default.X,
            CurrentFocalY ?? FocalPoint.Default.Y,
            CurrentZoom ?? FocalPoint.Default.Zoom);
        _point = _saved;
    }

    private async Task SubmitAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;

        HallOfFamePhotoInput? photo = null;
        FocalPoint? framing = null;

        if (_staged is not null)
        {
            photo = new HallOfFamePhotoInput
            {
                Container = _staged.Container,
                Path = _staged.Path,
                ContentType = _staged.ContentType,
                SizeInBytes = _staged.SizeInBytes
            };
            framing = _point;
        }
        else if (!_removed)
        {
            // Same photo, new framing: a null photo with a framing keeps the current photo.
            framing = _point;
        }

        var result = await OnSubmitRequestedAsync(photo, framing, CancellationToken.None);

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        ResetForm();

        await OnSaved.InvokeAsync();
    }
}
```

`EditHallOfFamePhotoModal.razor.css` (new): `.ehp-subtitle`, `.ehp-badge`, `.ehp-state`, `.ehp-help`, `.ehp-link-danger`, `.ehp-actions` are `.elm-*` from `EditTournamentLogoModal.razor.css` renamed. `.ehp-empty` (grid `1fr 160px`, one column at 640px), `.ehp-empty__box` (dashed border, gray background), and `.ehp-empty__preview` (132px gradient circle with initials, same look as the avatar) come from the mockup's `.empty`, `.empty-box`, and `.preview`. Possible follow-up for the tech-debt branch: extract the shared `elm-*`/`ehp-*` rules into one partial.

`HallOfFamePhotoEditor.razor` (new): host wrapper with the API calls, as `TournamentLogoEditor` does.

```razor
@using ErrorOr
@using Neba.Api.Contracts.HallOfFame
@using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto
@using Neba.Api.Contracts.Uploads
@using Neba.Website.Server.Services
@using Refit
@inject ApiExecutor ApiExecutor
@inject IHallOfFameApi HallOfFameApi
@inject ToastService ToastService

<EditHallOfFamePhotoModal IsOpen="@IsOpen"
                          BowlerName="@BowlerName"
                          CurrentPhotoUrl="@CurrentPhotoUrl"
                          CurrentFocalX="@CurrentFocalX"
                          CurrentFocalY="@CurrentFocalY"
                          CurrentZoom="@CurrentZoom"
                          OnClose="OnClose"
                          OnSaved="HandleSavedAsync"
                          OnUploadRequestedAsync="UploadPhotoAsync"
                          OnSubmitRequestedAsync="SavePhotoAsync" />

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public string InductionId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    [Parameter]
    public Uri? CurrentPhotoUrl { get; set; }

    [Parameter]
    public int? CurrentFocalX { get; set; }

    [Parameter]
    public int? CurrentFocalY { get; set; }

    [Parameter]
    public int? CurrentZoom { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Raised after the photo saved. The host closes the editor and reloads its data; the toast follows.
    /// </summary>
    [Parameter, EditorRequired]
    public EventCallback OnSaved { get; set; }

    private Task<ErrorOr<UploadedFileResponse>> UploadPhotoAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => ApiExecutor.ExecuteAsync(
            "HallOfFameApi",
            "UploadHallOfFamePhoto",
            c => HallOfFameApi.UploadHallOfFamePhotoAsync(new StreamPart(stream, fileName, contentType), c),
            ct);

    private Task<ErrorOr<Success>> SavePhotoAsync(HallOfFamePhotoInput? photo, FocalPoint? framing, CancellationToken ct)
    {
        var request = new EditHallOfFamePhotoRequest
        {
            Id = InductionId,
            Input = new EditHallOfFamePhotoInput
            {
                Photo = photo,
                FocalX = framing?.X,
                FocalY = framing?.Y,
                Zoom = framing?.Zoom
            }
        };

        return ApiExecutor.ExecuteAsync(
            "HallOfFameApi",
            "EditHallOfFamePhoto",
            c => HallOfFameApi.EditHallOfFamePhotoAsync(InductionId, request, c),
            ct);
    }

    private async Task HandleSavedAsync()
    {
        await OnSaved.InvokeAsync();
        ToastService.Show("Photo Saved", "The inductee photo was updated.", NotifySeverity.Success);
    }
}
```

`HallOfFame.razor` (edit). Both avatar blocks become the shared component:

```razor
<div class="inductee-item">
    <HallOfFameAvatar BowlerName="@inductee.BowlerName"
                      PhotoUri="@inductee.PhotoUri"
                      FocalX="@inductee.PhotoFocalX"
                      FocalY="@inductee.PhotoFocalY"
                      Zoom="@inductee.PhotoZoom"
                      OnEdit="@(() => OpenEditor(inductee))" />
    <div class="inductee-name">@inductee.BowlerName</div>
</div>
```

The combined grid keeps its extra `<div class="combined-categories">` line after the name. One editor sits after the "Eligibility & Criteria" block, inside the page's root `<div>`:

```razor
<HallOfFamePhotoEditor IsOpen="@(_editing is not null)"
                       InductionId="@(_editing?.Id ?? string.Empty)"
                       BowlerName="@(_editing?.BowlerName ?? string.Empty)"
                       CurrentPhotoUrl="@_editing?.PhotoUri"
                       CurrentFocalX="@_editing?.PhotoFocalX"
                       CurrentFocalY="@_editing?.PhotoFocalY"
                       CurrentZoom="@_editing?.PhotoZoom"
                       OnClose="CloseEditor"
                       OnSaved="HandleSavedAsync" />
```

`@code` changes: loading moves into `LoadAsync` so a save can reload without flashing the skeleton; `GetPhotoStyle` and `GetInitials` are removed.

```csharp
private HallOfFameInductionViewModel? _editing;

protected override Task OnInitializedAsync()
    => LoadAsync();

private async Task LoadAsync()
{
    var result = await ApiExecutor.ExecuteAsync(
        "HallOfFame",
        "ListHallOfFameInductions",
        ct => HallOfFameApi.ListHallOfFameInductionsAsync(ct));

    _isLoading = false;

    if (result.IsError)
    {
        _errorMessage = result.FirstError.Description;
        return;
    }

    _errorMessage = null;
    _inductionsByYear = /* existing grouping code, unchanged */;
}

private void OpenEditor(HallOfFameInductionViewModel inductee)
    => _editing = inductee;

private void CloseEditor()
    => _editing = null;

private async Task HandleSavedAsync()
{
    _editing = null;
    await LoadAsync();
}
```

### Tests

bUnit (`tests/Neba.Website.Tests`; follow `TournamentLogoEditButtonTests` for the authorization setup):

```csharp
// HallOfFame/HallOfFameAvatarTests.cs
[UnitTest]
[Component("Website.HallOfFame.HallOfFameAvatar")]
public sealed class HallOfFameAvatarTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;

    public HallOfFameAvatarTests() => _authContext = _ctx.AddAuthorization();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Avatar should show initials when there is no photo")]
    public void Avatar_ShouldShowInitials_WhenNoPhoto()
    {
        // Arrange
        // Act
        var cut = _ctx.Render<HallOfFameAvatar>(p => p.Add(x => x.BowlerName, "Jane Smith"));

        // Assert
        cut.Find(".inductee-photo").TextContent.Trim().ShouldBe("JS");
        cut.FindAll("img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Avatar should position the photo from the saved focal point and zoom")]
    public void Avatar_ShouldPositionPhoto_FromFocalPointAndZoom()
    {
        // Arrange
        // Act
        var cut = _ctx.Render<HallOfFameAvatar>(p => p
            .Add(x => x.BowlerName, "Jane Smith")
            .Add(x => x.PhotoUri, new Uri("https://files.example.test/jane.jpg"))
            .Add(x => x.FocalX, 31)
            .Add(x => x.FocalY, 36)
            .Add(x => x.Zoom, 150));

        // Assert
        cut.Find("img").GetAttribute("style")
            .ShouldBe("object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.5)");
    }

    [Fact(DisplayName = "Avatar should center the photo with no zoom when no framing is saved")]
    public void Avatar_ShouldCenterPhoto_WhenNoFraming() { /* style is 50% 50%, scale(1) */ }

    [Fact(DisplayName = "Avatar should hide the edit button without the permission")]
    public void Avatar_ShouldHideEditButton_WithoutPermission() { /* OnEdit set, user unauthorized: no button */ }

    [Fact(DisplayName = "Avatar should show the edit button with the permission and raise OnEdit when clicked")]
    public void Avatar_ShouldShowEditButton_WithPermission() { /* SetPolicies(EditHallOfFamePhoto); click; callback fired */ }

    // GetInitials: the four existing cases move here from HallOfFameTests
    // ("Jane Smith" -> "JS", "Mary Jo Johnson" -> "MJ", "Cher" -> "C", "" -> "?").
}
```

- `HallOfFame/HallOfFamePhotoEditButtonTests`: button hidden without the permission, shown with it, `aria-label` is "Edit photo for {name}", click raises `OnClick`.
- `HallOfFame/EditHallOfFamePhotoModalTests`:
  - Save is disabled when the modal opens.
  - Uploading a file stages it, shows the "New photo" badge, and enables Save.
  - Save after an upload submits the photo and the default framing (50, 50, 100).
  - "Remove photo" submits a null photo and a null framing.
  - After `HandlePointChanged` (invoke the picker's `OnChange`), Save submits a **null photo with the new framing**.
  - The point reverting to the saved value makes the form clean again.
  - An API error shows the alert and keeps the modal open.
  - Cancel when dirty asks to discard; confirming closes and resets.
  - With no current photo the empty state shows initials and no picker.
- `Components/FocalPointPickerTests` (uses `JSInterop.Mode = Loose` and `SetupVoid("initialize", ...)`):
  - Renders the image, preview style, zoom text ("1.5×"), and `aria-valuetext`.
  - Imports `./Components/FocalPointPicker.razor.js` and calls `initialize` with the root id, X, Y, and zoom.
  - A new parameter value after render calls `update`; an unchanged value does not.
  - Invoking `NotifyChanged(10, 20, 150)` raises `OnChange` and does not call `update` back.
  - Dispose calls `dispose` on the module, and tolerates `JSDisconnectedException`.
- Update `HallOfFame/HallOfFameTests`: the avatar markup test changes (the `img` and its style replace the `background-image` assertion), and the four `GetInitials` tests move to the avatar tests. Add a test that an edit button reaches each avatar in both the single-category and combined grids.
- Update `HallOfFame/HallOfFameMappingExtensionsTests` for `Id` and the framing fields, and `HallOfFameInductionViewModelFactory` for the new members.

JS (`Components/FocalPointPicker.razor.tests.js`, same runner as the other `.razor.tests.js` files):

```javascript
import { describe, it, expect } from 'vitest';
import { clamp, cropSize, centerOf, positionFor } from './FocalPointPicker.razor.js';

describe('cropSize', () => {
  it('covers the full height of a landscape photo in a round shape', () => {
    const size = cropSize(4 / 3, 1, 100);
    expect(size.cw).toBeCloseTo(0.75);
    expect(size.ch).toBeCloseTo(1);
  });

  it('shrinks the window as zoom grows', () => {
    const size = cropSize(4 / 3, 1, 200);
    expect(size.cw).toBeCloseTo(0.375);
    expect(size.ch).toBeCloseTo(0.5);
  });
});

describe('positionFor', () => {
  it('stops at the photo edge when the point is too near it', () => {
    expect(positionFor(0.3, 0.5, cropSize(4 / 3, 1, 100)).x).toBe(0);
  });

  it('reaches a point near the edge once zoomed in', () => {
    const size = cropSize(4 / 3, 1, 200);
    const { x } = positionFor(0.31, 0.5, size);
    expect(centerOf(x, 50, size).cx).toBeCloseTo(0.31, 1);
  });

  it('keeps an axis with no overflow at 50', () => {
    expect(positionFor(0.5, 0.1, cropSize(4 / 3, 1, 100)).y).toBe(50);
  });

  it('clamps to 0 and 100', () => {
    const size = cropSize(4 / 3, 1, 100);
    expect(positionFor(-1, 0.5, size).x).toBe(0);
    expect(positionFor(2, 0.5, size).x).toBe(100);
  });
});

describe('centerOf', () => {
  it('round-trips with positionFor inside the reachable range', () => {
    const size = cropSize(16 / 9, 1, 150);
    const { cx, cy } = centerOf(40, 60, size);
    expect(positionFor(cx, cy, size)).toEqual({ x: 40, y: 60 });
  });
});

describe('clamp', () => {
  it('limits to 0 to 100', () => {
    expect(clamp(-5)).toBe(0);
    expect(clamp(105)).toBe(100);
    expect(clamp(42)).toBe(42);
  });
});
```

Plus DOM tests in the same file (jsdom, as the other module tests do): a click outside the crop moves the window; pressing inside the crop and moving keeps the grab offset; arrow keys move 1, Shift moves 5, and stop at 0 and 100; arrow keys on an axis with no overflow do nothing; the zoom slider keeps the window's center; `notify` fires once per animation frame during a fast drag; `dispose` removes the listeners.

Playwright (`tests/e2e/EditHallOfFamePhoto.spec.ts`, modeled on `EditTournamentLogo.spec.ts`):

```typescript
import { test, expect, type Page } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

const INDUCTEE = 'Jane Smith';
const PHOTO_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
  'base64'
);

async function uploadPhoto(page: Page) {
  await page.locator('input.neba-file-upload-input').setInputFiles({
    name: 'photo.png',
    mimeType: 'image/png',
    buffer: PHOTO_PNG,
  });
  await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();
}

test.describe('Edit Hall of Fame photo (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the edit-photo button', async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
    await expect(page.locator('button.hof-photo-edit')).toHaveCount(0);
  });
});

test.describe('Edit Hall of Fame photo (with the permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=HallOfFame.EditHallOfFamePhoto');
  });

  test('uploads a photo, zooms, sets the focal point, and shows it on the page', async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');

    await page.getByRole('button', { name: `Edit photo for ${INDUCTEE}` }).click();
    await expect(page.getByRole('dialog')).toContainText('Edit photo');
    await expect(page.getByRole('button', { name: 'Save photo' })).toBeDisabled();

    await uploadPhoto(page);
    await expect(page.locator('.ehp-badge')).toContainText('New photo');

    // The 1x1 test image has no overflow at 1x, so zoom in before placing the window.
    await page.getByLabel('Zoom').fill('200');
    const surface = page.locator('.focal-picker__surface');
    const box = (await surface.boundingBox())!;
    await page.mouse.click(box.x + box.width * 0.25, box.y + box.height * 0.25);

    await page.getByRole('button', { name: 'Save photo' }).click();
    await expect(page.locator('.neba-toast')).toContainText('Photo Saved');

    const lastRequest = await page.request
      .get('http://localhost:5151/__mock/last-request?method=PUT&path=/hall-of-fame/01JX0000000000000000000001/photo')
      .then((r) => r.json());
    expect(lastRequest.input.photo.path).toBe('hall-of-fame/photo/e2e-test-photo.png');
    expect(lastRequest.input.zoom).toBe(200);
    expect(lastRequest.input.focalX).toBe(0);

    const style = await page
      .locator('.inductee-item', { hasText: INDUCTEE })
      .locator('img.inductee-photo__img')
      .getAttribute('style');
    expect(style).toContain('scale(2)');
  });

  test('moves the focal point of the current photo with a null photo in the request', async ({ page }) => { /* open Bob Johnson, press ArrowRight, save, expect input.photo === null and focalX changed */ });

  test('removes a photo and shows initials again', async ({ page }) => { /* Remove photo, save, expect no img in the avatar and initials text */ });

  test('asks before discarding changes when Cancel is clicked', async ({ page }) => { /* upload, Cancel, "Discard unsaved changes?", Discard */ });
});
```

`tests/e2e/mock-api/mock-api-server.ts` (edit): give each mocked induction an `id` (`01JX0000000000000000000001` and so on), and add `photoFocalX`, `photoFocalY`, `photoZoom` plus a `photoUri` to one of them (Bob Johnson) so the move-focal-point test has a photo to start from. Add two handlers beside the sponsor and tournament logo ones:

```typescript
if (req.method === 'POST' && pathname === '/hall-of-fame/photos') {
  // same shape as the /sponsors/logo handler
  return json(res, 200, {
    container: 'bowlneba-public',
    path: 'hall-of-fame/photo/e2e-test-photo.png',
    fileName: 'e2e-test-photo.png',
    contentType: 'image/png',
    sizeInBytes: PHOTO_PNG_SIZE,
    url: 'http://localhost:5151/mock-storage/hall-of-fame/photo/e2e-test-photo.png',
  });
}

const photoMatch = pathname.match(/^\/hall-of-fame\/([^/]+)\/photo$/);
if (req.method === 'PUT' && photoMatch) {
  const body = await readJson(req);
  recordLastRequest('PUT', pathname, body);           // same recorder the tournament logo handler uses
  const induction = MOCK_HALL_OF_FAME.items.find((i) => i.id === photoMatch[1]);
  if (!induction) return notFound(res);
  if (body.input.photo) {
    induction.photoUri = `http://localhost:5151/mock-storage/${body.input.photo.path}`;
  } else if (body.input.focalX == null) {
    induction.photoUri = null;                        // removal
  }
  induction.photoFocalX = body.input.focalX ?? null;
  induction.photoFocalY = body.input.focalY ?? null;
  induction.photoZoom = body.input.zoom ?? null;
  return noContent(res);
}
```

(Match the helper names the file already uses; the sketch above uses placeholder names for `json`, `readJson`, `recordLastRequest`, `notFound`, `noContent`.)

Per the decision table: bUnit for component logic and API wiring, the JS suite for the picker's geometry and pointer handling, Playwright for the real upload, picker, and save flow.

### Docs

- `docs/help/`: how to add and align an inductee photo (place, drag, zoom, remove), with screenshots (run `/help-documentation`).
- `docs/ubiquitous-language.md`: "Focal Point" (position and zoom, with the object-position meaning) as listed in Phase 1.
- `docs/learnings/blazor-ui.md`: one line on the picker pattern (module owns geometry and sets styles Razor never renders), so news headers reuse it.

### Deferred in Phase 2

- News header images (the picker takes `Shape` and `Aspect` so they can reuse it).
- Pinch-to-zoom and mouse-wheel zoom; the slider and the keyboard cover zoom for now.
- Extracting the logo and photo modal styles into a shared partial (tech-debt branch).

### Mockups

Single mockup, because the modal is a data-capture form with one obvious layout and the avatar change is small. It reuses the existing NEBA tokens, `NebaModal` shell, and logo-modal patterns.

- [`mockups/hall-of-fame-photo/edit-photo-modal.html`](mockups/hall-of-fame-photo/edit-photo-modal.html): Hall of Fame grid with the edit pencil on each avatar, and the Edit photo modal. Click the pencil to open it. Pat Rowan has no photo (empty state, upload). Bob Johnson's face sits near the left edge (zoom in to center it). Jane Smith is saved at 2×. Click to place the crop outline, drag it to refine, use the arrow keys (Shift for 5%), and the zoom slider. Save stays disabled until something changes; Remove photo; Upload a replacement; Cancel with changes asks to discard. A toolbar toggle hides the pencil to show the no-permission view.
