using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesLogoEditorTests : IDisposable
{
    private const string SeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";

    private static readonly Uri CurrentLogo = new("https://storage.example.com/bowlneba-public/series/logo/current.png");

    private readonly BunitContext _ctx = new();
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();

    public SeriesLogoEditorTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_seriesApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    private IRenderedComponent<SeriesLogoEditor> RenderEditor(Action onSaved)
        => _ctx.Render<SeriesLogoEditor>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.SeriesId, SeriesId)
            .Add(x => x.SeriesName, "2025 Vincent Hall Memorial Series")
            .Add(x => x.CurrentLogoUrl, CurrentLogo)
            .Add(x => x.OnClose, EventCallback.Empty)
            .Add(x => x.OnSaved, EventCallback.Factory.Create(this, onSaved)));

    [Fact(DisplayName = "Should send a null logo, notify the host and show a toast when the current logo is removed and saved")]
    public async Task Save_ShouldSendNullLogoAndNotify_WhenLogoRemoved()
    {
        // Arrange
        EditSeriesLogoRequest? captured = null;
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent,
        };
        _seriesApi
            .Setup(x => x.EditSeriesLogoAsync(SeriesId, It.IsAny<EditSeriesLogoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditSeriesLogoRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(response);

        var saved = 0;
        var cut = RenderEditor(() => saved++);
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(SeriesId);
        captured.Input.Logo.ShouldBeNull();
        saved.ShouldBe(1);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Logo Saved");
    }

    [Fact(DisplayName = "Should upload the chosen file, then map it to the Series logo input when saved")]
    public async Task Save_ShouldUploadThenMapLogo_WhenLogoUploaded()
    {
        // Arrange
        using var uploadResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = UploadedFileResponseFactory.Create(
                container: "bowlneba-public",
                path: "series/logo/new.png",
                contentType: "image/png",
                sizeInBytes: 4096),
        };
        _seriesApi
            .Setup(x => x.UploadSeriesLogoAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        EditSeriesLogoRequest? captured = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent,
        };
        _seriesApi
            .Setup(x => x.EditSeriesLogoAsync(SeriesId, It.IsAny<EditSeriesLogoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditSeriesLogoRequest, CancellationToken>((_, r, _) => captured = r)
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
        logo.Path.ShouldBe("series/logo/new.png");
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
            StatusCode = System.Net.HttpStatusCode.Conflict,
        };
        _seriesApi
            .Setup(x => x.EditSeriesLogoAsync(SeriesId, It.IsAny<EditSeriesLogoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var saved = 0;
        var cut = RenderEditor(() => saved++);
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        saved.ShouldBe(0);
        _toastService.Current.ShouldBeNull();
    }
}