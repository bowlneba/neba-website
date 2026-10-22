using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components.Forms;

using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Components;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.EditTournamentLogoModal")]
public sealed class EditTournamentLogoModalTests : IDisposable
{
    private static readonly Uri CurrentLogo = new("https://storage.example.com/bowlneba-public/tournaments/logo/current.png");

    private readonly BunitContext _ctx;

    public EditTournamentLogoModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<EditTournamentLogoModal> Render(
        Uri? currentLogoUrl = null,
        Func<Stream, string, string, IProgress<int>, CancellationToken, Task<ErrorOr<UploadedFileResponse>>>? onUpload = null,
        Func<TournamentLogoInput?, CancellationToken, Task<ErrorOr<Success>>>? onSubmit = null,
        Action? onSaved = null,
        Action? onClose = null)
        => _ctx.Render<EditTournamentLogoModal>(p =>
        {
            p.Add(x => x.IsOpen, true);
            p.Add(x => x.TournamentName, "Spring Open");
            p.Add(x => x.CurrentLogoUrl, currentLogoUrl);
            p.Add(x => x.OnUploadRequestedAsync, onUpload ?? DefaultUpload);
            p.Add(x => x.OnSubmitRequestedAsync, onSubmit ?? ((_, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));
            p.Add(x => x.OnSaved, onSaved ?? (() => { }));
            p.Add(x => x.OnClose, onClose ?? (() => { }));
        });

    private static readonly Func<Stream, string, string, IProgress<int>, CancellationToken, Task<ErrorOr<UploadedFileResponse>>> DefaultUpload =
        (_, _, _, _, _) => Task.FromResult<ErrorOr<UploadedFileResponse>>(UploadedFileResponseFactory.Create(
            container: "bowlneba-public",
            path: "tournaments/logo/new.png",
            contentType: "image/png",
            sizeInBytes: 4096));

    private static async Task UploadFileAsync(IRenderedComponent<EditTournamentLogoModal> cut)
    {
        var input = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "new.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));
    }

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<EditTournamentLogoModal> cut)
        => cut.Find("button.neba-btn-primary");

    private static AngleSharp.Dom.IElement CancelButton(IRenderedComponent<EditTournamentLogoModal> cut)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");

    [Fact(DisplayName = "Should show the tournament name in the header")]
    public void Render_ShouldShowTournamentName()
    {
        // Arrange & Act
        var cut = Render(CurrentLogo);

        // Assert
        cut.Find(".elm-subtitle").TextContent.Trim().ShouldBe("Spring Open");
    }

    [Fact(DisplayName = "Should show the current logo and a remove button when the tournament has a logo")]
    public void Render_ShouldShowCurrentLogoAndRemove_WhenLogoExists()
    {
        // Arrange & Act
        var cut = Render(CurrentLogo);

        // Assert
        cut.Find(".elm-tile img").GetAttribute("src").ShouldBe(CurrentLogo.AbsoluteUri);
        cut.Find(".elm-state").TextContent.Trim().ShouldBe("Current logo");
        cut.Find(".elm-link-danger").TextContent.Trim().ShouldBe("Remove current logo");
    }

    [Fact(DisplayName = "Should show the no-logo state and no remove button when the tournament has no logo")]
    public void Render_ShouldShowNoLogoState_WhenNoLogo()
    {
        // Arrange & Act
        var cut = Render(currentLogoUrl: null);

        // Assert
        cut.Find(".elm-state").TextContent.Trim().ShouldBe("No logo set");
        cut.FindAll(".elm-tile img").ShouldBeEmpty();
        cut.FindAll(".elm-link-danger").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should disable Save until the logo changes")]
    public void Render_ShouldDisableSave_WhenNothingChanged()
    {
        // Arrange & Act
        var cut = Render(CurrentLogo);

        // Assert
        SaveButton(cut).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should submit a null logo and raise OnSaved when the current logo is removed")]
    public async Task Save_ShouldSubmitNullLogo_WhenCurrentLogoRemoved()
    {
        // Arrange
        var submitted = false;
        TournamentLogoInput? captured = new() { Container = "x", Path = "x", ContentType = "x", SizeInBytes = 1 };
        var saved = 0;

        var cut = Render(
            CurrentLogo,
            onSubmit: (logo, _) =>
            {
                submitted = true;
                captured = logo;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            },
            onSaved: () => saved++);

        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        cut.Find(".elm-state").TextContent.Trim().ShouldBe("Logo will be removed");
        await SaveButton(cut).ClickAsync();

        // Assert
        submitted.ShouldBeTrue();
        captured.ShouldBeNull();
        saved.ShouldBe(1);
    }

    [Fact(DisplayName = "Should submit the uploaded file as the logo when a replacement is uploaded")]
    public async Task Save_ShouldSubmitUploadedLogo_WhenReplacementUploaded()
    {
        // Arrange
        TournamentLogoInput? captured = null;
        var cut = Render(
            CurrentLogo,
            onSubmit: (logo, _) =>
            {
                captured = logo;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });

        await UploadFileAsync(cut);

        // Act
        cut.Find(".elm-badge").TextContent.Trim().ShouldBe("New logo");
        await SaveButton(cut).ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Container.ShouldBe("bowlneba-public");
        captured.Path.ShouldBe("tournaments/logo/new.png");
        captured.ContentType.ShouldBe("image/png");
        captured.SizeInBytes.ShouldBe(4096);
    }

    [Fact(DisplayName = "Should return to the current logo and disable Save when the new logo is discarded")]
    public async Task Discard_ShouldRestoreCurrentLogo()
    {
        // Arrange
        var cut = Render(CurrentLogo);
        await UploadFileAsync(cut);

        // Act
        await cut.Find(".elm-link-danger").ClickAsync();

        // Assert
        cut.Find(".elm-state").TextContent.Trim().ShouldBe("Current logo");
        SaveButton(cut).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show the error and keep the modal open when the save fails")]
    public async Task Save_ShouldShowError_WhenSubmitFails()
    {
        // Arrange
        var saved = 0;
        var cut = Render(
            CurrentLogo,
            onSubmit: (_, _) => Task.FromResult<ErrorOr<Success>>(Error.Failure("Logo.Failed", "The logo could not be saved.")),
            onSaved: () => saved++);

        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await SaveButton(cut).ClickAsync();

        // Assert
        cut.Markup.ShouldContain("The logo could not be saved.");
        saved.ShouldBe(0);
    }

    [Fact(DisplayName = "Should close without a prompt when Cancel is clicked with no changes")]
    public async Task Cancel_ShouldClose_WhenClean()
    {
        // Arrange
        var closed = 0;
        var cut = Render(CurrentLogo, onClose: () => closed++);

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
        var cut = Render(CurrentLogo, onClose: () => closed++);
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await CancelButton(cut).ClickAsync();

        // Assert
        closed.ShouldBe(0);
        cut.Markup.ShouldContain("Discard unsaved changes?");
    }
}