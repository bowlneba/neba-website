using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components.Forms;

using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Components;
using Neba.Website.Server.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.EditHallOfFamePhotoModal")]
public sealed class EditHallOfFamePhotoModalTests : IDisposable
{
    private static readonly Uri CurrentPhoto = new("https://storage.example.com/bowlneba-public/hall-of-fame/photo/current.png");

    private readonly BunitContext _ctx;

    public EditHallOfFamePhotoModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
    }

    public void Dispose() => _ctx.Dispose();

    private static readonly Func<Stream, string, string, IProgress<int>, CancellationToken, Task<ErrorOr<UploadedFileResponse>>> DefaultUpload =
        (_, _, _, _, _) => Task.FromResult<ErrorOr<UploadedFileResponse>>(UploadedFileResponseFactory.Create(
            container: "bowlneba-public",
            path: "hall-of-fame/photo/new.png",
            contentType: "image/png",
            sizeInBytes: 4096));

    private IRenderedComponent<EditHallOfFamePhotoModal> Render(
        Uri? currentPhotoUrl = null,
        int? focalX = null,
        int? focalY = null,
        int? zoom = null,
        Func<HallOfFamePhotoInput?, FocalPoint?, CancellationToken, Task<ErrorOr<Success>>>? onSubmit = null,
        Action? onSaved = null,
        Action? onClose = null)
        => _ctx.Render<EditHallOfFamePhotoModal>(p =>
        {
            p.Add(x => x.IsOpen, true);
            p.Add(x => x.BowlerName, "Jane Smith");
            p.Add(x => x.CurrentPhotoUrl, currentPhotoUrl);
            p.Add(x => x.CurrentFocalX, focalX);
            p.Add(x => x.CurrentFocalY, focalY);
            p.Add(x => x.CurrentZoom, zoom);
            p.Add(x => x.OnUploadRequestedAsync, DefaultUpload);
            p.Add(x => x.OnSubmitRequestedAsync, onSubmit ?? ((_, _, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));
            p.Add(x => x.OnSaved, onSaved ?? (() => { }));
            p.Add(x => x.OnClose, onClose ?? (() => { }));
        });

    private static async Task UploadFileAsync(IRenderedComponent<EditHallOfFamePhotoModal> cut)
    {
        var input = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "new.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));
    }

    private static Task MovePickerAsync(IRenderedComponent<EditHallOfFamePhotoModal> cut, int x, int y, int zoom)
    {
        var picker = cut.FindComponent<FocalPointPicker>();
        return cut.InvokeAsync(() => picker.Instance.NotifyChanged(x, y, zoom));
    }

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<EditHallOfFamePhotoModal> cut)
        => cut.Find("button.neba-btn-primary");

    private static AngleSharp.Dom.IElement CancelButton(IRenderedComponent<EditHallOfFamePhotoModal> cut)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");

    [Fact(DisplayName = "Should show the inductee name in the header")]
    public void Render_ShouldShowBowlerName()
    {
        // Arrange
        // Act
        var cut = Render(CurrentPhoto);

        // Assert
        cut.Find(".ehp-subtitle").TextContent.Trim().ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Should show the picker at the saved framing and a remove button when the inductee has a photo")]
    public void Render_ShouldShowPickerAtSavedFraming_WhenPhotoExists()
    {
        // Arrange
        // Act
        var cut = Render(CurrentPhoto, focalX: 31, focalY: 36, zoom: 150);

        // Assert
        var picker = cut.FindComponent<FocalPointPicker>();
        picker.Instance.ImageUrl.ShouldBe(CurrentPhoto);
        picker.Instance.X.ShouldBe(31);
        picker.Instance.Y.ShouldBe(36);
        picker.Instance.Zoom.ShouldBe(150);
        cut.Find(".ehp-state").TextContent.Trim().ShouldBe("Current photo");
        cut.Find(".ehp-link-danger").TextContent.Trim().ShouldBe("Remove photo");
    }

    [Fact(DisplayName = "Should default to the centered framing when the saved photo has none")]
    public void Render_ShouldDefaultFraming_WhenNoneSaved()
    {
        // Arrange
        // Act
        var cut = Render(CurrentPhoto);

        // Assert
        var picker = cut.FindComponent<FocalPointPicker>();
        new FocalPoint(picker.Instance.X, picker.Instance.Y, picker.Instance.Zoom).ShouldBe(FocalPoint.Default);
    }

    [Fact(DisplayName = "Should show the empty state with initials and no picker when the inductee has no photo")]
    public void Render_ShouldShowEmptyState_WhenNoPhoto()
    {
        // Arrange
        // Act
        var cut = Render(currentPhotoUrl: null);

        // Assert
        cut.FindAll(".focal-picker").ShouldBeEmpty();
        cut.Find(".ehp-empty__box strong").TextContent.ShouldBe("No photo yet");
        cut.Find(".ehp-empty__preview").TextContent.Trim().ShouldBe("JS");
        cut.Find(".neba-file-upload-label").TextContent.ShouldContain("Upload a photo");
    }

    [Fact(DisplayName = "Should disable Save until something changes")]
    public void Render_ShouldDisableSave_WhenNothingChanged()
    {
        // Arrange
        // Act
        var cut = Render(CurrentPhoto);

        // Assert
        SaveButton(cut).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should submit a null photo and a null framing when the photo is removed")]
    public async Task Save_ShouldSubmitNullPhotoAndFraming_WhenPhotoRemoved()
    {
        // Arrange
        var submitted = false;
        HallOfFamePhotoInput? capturedPhoto = EditHallOfFamePhotoPlaceholder();
        FocalPoint? capturedFraming = FocalPoint.Default;
        var saved = 0;
        var cut = Render(
            CurrentPhoto,
            onSubmit: (photo, framing, _) =>
            {
                submitted = true;
                capturedPhoto = photo;
                capturedFraming = framing;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            },
            onSaved: () => saved++);
        await cut.Find(".ehp-link-danger").ClickAsync();

        // Act
        cut.Find(".ehp-empty__box strong").TextContent.ShouldBe("Photo will be removed");
        await SaveButton(cut).ClickAsync();

        // Assert
        submitted.ShouldBeTrue();
        capturedPhoto.ShouldBeNull();
        capturedFraming.ShouldBeNull();
        saved.ShouldBe(1);
    }

    [Fact(DisplayName = "Should submit the uploaded file with the centered default framing when a photo is uploaded")]
    public async Task Save_ShouldSubmitUploadedPhotoWithDefaultFraming_WhenPhotoUploaded()
    {
        // Arrange
        HallOfFamePhotoInput? capturedPhoto = null;
        FocalPoint? capturedFraming = null;
        var cut = Render(
            CurrentPhoto,
            focalX: 31,
            focalY: 36,
            zoom: 150,
            onSubmit: (photo, framing, _) =>
            {
                capturedPhoto = photo;
                capturedFraming = framing;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });
        await UploadFileAsync(cut);

        // Act
        cut.Find(".ehp-badge").TextContent.Trim().ShouldBe("New photo");
        await SaveButton(cut).ClickAsync();

        // Assert
        capturedPhoto.ShouldNotBeNull();
        capturedPhoto.Container.ShouldBe("bowlneba-public");
        capturedPhoto.Path.ShouldBe("hall-of-fame/photo/new.png");
        capturedPhoto.ContentType.ShouldBe("image/png");
        capturedPhoto.SizeInBytes.ShouldBe(4096);
        capturedFraming.ShouldBe(FocalPoint.Default);
    }

    [Fact(DisplayName = "Should upload a first photo for an inductee who has none")]
    public async Task Save_ShouldSubmitFirstPhoto_WhenInducteeHadNone()
    {
        // Arrange
        HallOfFamePhotoInput? capturedPhoto = null;
        var cut = Render(
            currentPhotoUrl: null,
            onSubmit: (photo, _, _) =>
            {
                capturedPhoto = photo;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });
        await UploadFileAsync(cut);

        // Act
        cut.FindComponent<FocalPointPicker>().ShouldNotBeNull();
        await SaveButton(cut).ClickAsync();

        // Assert
        capturedPhoto.ShouldNotBeNull();
        capturedPhoto.Path.ShouldBe("hall-of-fame/photo/new.png");
    }

    [Fact(DisplayName = "Should submit the framing the editor chose for a newly uploaded photo")]
    public async Task Save_ShouldSubmitChosenFraming_ForUploadedPhoto()
    {
        // Arrange
        FocalPoint? capturedFraming = null;
        var cut = Render(
            CurrentPhoto,
            onSubmit: (_, framing, _) =>
            {
                capturedFraming = framing;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });
        await UploadFileAsync(cut);
        await MovePickerAsync(cut, 20, 70, 180);

        // Act
        await SaveButton(cut).ClickAsync();

        // Assert
        capturedFraming.ShouldBe(new FocalPoint(20, 70, 180));
    }

    [Fact(DisplayName = "Should enable Save and submit a null photo with the new framing when only the framing changes")]
    public async Task Save_ShouldSubmitNullPhotoWithFraming_WhenOnlyFramingChanges()
    {
        // Arrange
        HallOfFamePhotoInput? capturedPhoto = EditHallOfFamePhotoPlaceholder();
        FocalPoint? capturedFraming = null;
        var cut = Render(
            CurrentPhoto,
            focalX: 31,
            focalY: 36,
            zoom: 150,
            onSubmit: (photo, framing, _) =>
            {
                capturedPhoto = photo;
                capturedFraming = framing;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });

        // Act
        await MovePickerAsync(cut, 60, 40, 200);
        SaveButton(cut).HasAttribute("disabled").ShouldBeFalse();
        await SaveButton(cut).ClickAsync();

        // Assert
        capturedPhoto.ShouldBeNull();
        capturedFraming.ShouldBe(new FocalPoint(60, 40, 200));
    }

    [Fact(DisplayName = "Should disable Save again when the framing returns to the saved value")]
    public async Task Save_ShouldBeDisabled_WhenFramingReturnsToSaved()
    {
        // Arrange
        var cut = Render(CurrentPhoto, focalX: 31, focalY: 36, zoom: 150);
        await MovePickerAsync(cut, 60, 40, 200);

        // Act
        await MovePickerAsync(cut, 31, 36, 150);

        // Assert
        SaveButton(cut).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return to the current photo and its framing, and disable Save, when the new photo is discarded")]
    public async Task Discard_ShouldRestoreCurrentPhotoAndFraming()
    {
        // Arrange
        var cut = Render(CurrentPhoto, focalX: 31, focalY: 36, zoom: 150);
        await UploadFileAsync(cut);

        // Act
        await cut.Find(".ehp-link-danger").ClickAsync();

        // Assert
        cut.Find(".ehp-state").TextContent.Trim().ShouldBe("Current photo");
        var picker = cut.FindComponent<FocalPointPicker>();
        new FocalPoint(picker.Instance.X, picker.Instance.Y, picker.Instance.Zoom).ShouldBe(new FocalPoint(31, 36, 150));
        SaveButton(cut).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show the error and keep the modal open when the save fails")]
    public async Task Save_ShouldShowError_WhenSubmitFails()
    {
        // Arrange
        var saved = 0;
        var cut = Render(
            CurrentPhoto,
            onSubmit: (_, _, _) => Task.FromResult<ErrorOr<Success>>(Error.Failure("Photo.Failed", "The photo could not be saved.")),
            onSaved: () => saved++);
        await cut.Find(".ehp-link-danger").ClickAsync();

        // Act
        await SaveButton(cut).ClickAsync();

        // Assert
        cut.Markup.ShouldContain("The photo could not be saved.");
        saved.ShouldBe(0);
    }

    [Fact(DisplayName = "Should close without a prompt when Cancel is clicked with no changes")]
    public async Task Cancel_ShouldClose_WhenClean()
    {
        // Arrange
        var closed = 0;
        var cut = Render(CurrentPhoto, onClose: () => closed++);

        // Act
        await CancelButton(cut).ClickAsync();

        // Assert
        closed.ShouldBe(1);
        cut.Markup.ShouldNotContain("Discard unsaved changes?");
    }

    [Fact(DisplayName = "Should ask to discard changes when Cancel is clicked with unsaved changes")]
    public async Task Cancel_ShouldPrompt_WhenDirty()
    {
        // Arrange
        var closed = 0;
        var cut = Render(CurrentPhoto, onClose: () => closed++);
        await MovePickerAsync(cut, 60, 40, 200);

        // Act
        await CancelButton(cut).ClickAsync();

        // Assert
        closed.ShouldBe(0);
        cut.Markup.ShouldContain("Discard unsaved changes?");
    }

    [Fact(DisplayName = "Should close when the discard prompt is confirmed")]
    public async Task Cancel_ShouldClose_WhenDiscardConfirmed()
    {
        // Arrange
        var closed = 0;
        var cut = Render(CurrentPhoto, onClose: () => closed++);
        await MovePickerAsync(cut, 60, 40, 200);
        await CancelButton(cut).ClickAsync();

        // Act
        await cut.FindAll("button").First(b => b.TextContent.Trim() == "Discard").ClickAsync();

        // Assert
        closed.ShouldBe(1);
    }

    // A non-null sentinel so a test can prove the submit callback assigned null.
    private static HallOfFamePhotoInput EditHallOfFamePhotoPlaceholder()
        => new() { Container = "x", Path = "x", ContentType = "x", SizeInBytes = 1 };
}
