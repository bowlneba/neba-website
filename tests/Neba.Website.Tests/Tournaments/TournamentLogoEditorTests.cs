using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.EditTournamentLogo;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.TournamentLogoEditor")]
public sealed class TournamentLogoEditorTests : IDisposable
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private static readonly Uri CurrentLogo = new("https://storage.example.com/bowlneba-public/tournaments/logo/current.png");

    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _tournamentsApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();

    public TournamentLogoEditorTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_tournamentsApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    private IRenderedComponent<TournamentLogoEditor> RenderEditor(Action onSaved)
        => _ctx.Render<TournamentLogoEditor>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.TournamentName, "Spring Open")
            .Add(x => x.CurrentLogoUrl, CurrentLogo)
            .Add(x => x.OnClose, EventCallback.Empty)
            .Add(x => x.OnSaved, EventCallback.Factory.Create(this, onSaved)));

    [Fact(DisplayName = "Should send a null logo for the tournament, notify the host, and show a toast when the current logo is removed and saved")]
    public async Task Save_ShouldSendNullLogoAndNotify_WhenCurrentLogoRemoved()
    {
        // Arrange
        EditTournamentLogoRequest? captured = null;
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _tournamentsApi
            .Setup(x => x.EditTournamentLogoAsync(TournamentId, It.IsAny<EditTournamentLogoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditTournamentLogoRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(response);

        var savedCount = 0;
        var cut = RenderEditor(() => savedCount++);
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(TournamentId);
        captured.Input.Logo.ShouldBeNull();
        savedCount.ShouldBe(1);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Logo Saved");
    }

    [Fact(DisplayName = "Should map the uploaded file to the tournament logo input when a new logo is saved")]
    public async Task Save_ShouldMapUploadToTournamentLogoInput_WhenLogoUploaded()
    {
        // Arrange
        using var uploadResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = UploadedFileResponseFactory.Create(
                container: "bowlneba-public",
                path: "tournaments/logo/new.png",
                contentType: "image/png",
                sizeInBytes: 4096)
        };
        _tournamentsApi
            .Setup(x => x.UploadTournamentLogoAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        EditTournamentLogoRequest? captured = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _tournamentsApi
            .Setup(x => x.EditTournamentLogoAsync(TournamentId, It.IsAny<EditTournamentLogoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditTournamentLogoRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(editResponse);

        var cut = RenderEditor(() => { });
        var input = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "new.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        var logo = captured.Input.Logo.ShouldNotBeNull();
        logo.Container.ShouldBe("bowlneba-public");
        logo.Path.ShouldBe("tournaments/logo/new.png");
        logo.ContentType.ShouldBe("image/png");
        logo.SizeInBytes.ShouldBe(4096);
    }

    [Fact(DisplayName = "Should neither notify the host nor show a toast when the save fails")]
    public async Task Save_ShouldNotNotifyOrToast_WhenApiFails()
    {
        // Arrange
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.UnprocessableEntity
        };
        _tournamentsApi
            .Setup(x => x.EditTournamentLogoAsync(TournamentId, It.IsAny<EditTournamentLogoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var savedCount = 0;
        var cut = RenderEditor(() => savedCount++);
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        savedCount.ShouldBe(0);
        _toastService.Current.ShouldBeNull();
    }
}