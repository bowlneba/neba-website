using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.HallOfFame;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFamePhotoEditor")]
public sealed class HallOfFamePhotoEditorTests : IDisposable
{
    private const string InductionId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private static readonly Uri CurrentPhoto = new("https://storage.example.com/bowlneba-public/hall-of-fame/photo/current.png");

    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _hallOfFameApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();

    public HallOfFamePhotoEditorTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_hallOfFameApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    private IRenderedComponent<HallOfFamePhotoEditor> RenderEditor(Action onSaved, Uri? currentPhoto = null)
        => _ctx.Render<HallOfFamePhotoEditor>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.InductionId, InductionId)
            .Add(x => x.BowlerName, "Jane Smith")
            .Add(x => x.CurrentPhotoUrl, currentPhoto ?? CurrentPhoto)
            .Add(x => x.CurrentFocalX, 31)
            .Add(x => x.CurrentFocalY, 36)
            .Add(x => x.CurrentZoom, 150)
            .Add(x => x.OnClose, EventCallback.Empty)
            .Add(x => x.OnSaved, EventCallback.Factory.Create(this, onSaved)));

    [Fact(DisplayName = "Should send a null photo and no framing, notify the host, and show a toast when the photo is removed and saved")]
    public async Task Save_ShouldSendNullPhotoAndNotify_WhenPhotoRemoved()
    {
        // Arrange
        EditHallOfFamePhotoRequest? captured = null;
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _hallOfFameApi
            .Setup(x => x.EditHallOfFamePhotoAsync(InductionId, It.IsAny<EditHallOfFamePhotoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditHallOfFamePhotoRequest, CancellationToken>((_, request, _) => captured = request)
            .ReturnsAsync(response);

        var savedCount = 0;
        var cut = RenderEditor(() => savedCount++);
        await cut.Find(".ehp-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(InductionId);
        captured.Input.Photo.ShouldBeNull();
        captured.Input.FocalX.ShouldBeNull();
        captured.Input.FocalY.ShouldBeNull();
        captured.Input.Zoom.ShouldBeNull();
        savedCount.ShouldBe(1);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Photo Saved");
    }

    [Fact(DisplayName = "Should send a null photo with the new framing when only the framing changes")]
    public async Task Save_ShouldSendFramingOnly_WhenFramingChanges()
    {
        // Arrange
        EditHallOfFamePhotoRequest? captured = null;
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _hallOfFameApi
            .Setup(x => x.EditHallOfFamePhotoAsync(InductionId, It.IsAny<EditHallOfFamePhotoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditHallOfFamePhotoRequest, CancellationToken>((_, request, _) => captured = request)
            .ReturnsAsync(response);

        var cut = RenderEditor(() => { });
        var picker = cut.FindComponent<FocalPointPicker>();
        await cut.InvokeAsync(() => picker.Instance.NotifyChanged(60, 40, 200));

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Input.Photo.ShouldBeNull();
        captured.Input.FocalX.ShouldBe(60);
        captured.Input.FocalY.ShouldBe(40);
        captured.Input.Zoom.ShouldBe(200);
    }

    [Fact(DisplayName = "Should upload the chosen file, then send it with the default framing when saved")]
    public async Task Save_ShouldUploadThenSendPhoto_WhenPhotoUploaded()
    {
        // Arrange
        using var uploadResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = UploadedFileResponseFactory.Create(
                container: "bowlneba-public",
                path: "hall-of-fame/photo/e2e.png",
                contentType: "image/png",
                sizeInBytes: 2048)
        };
        _hallOfFameApi
            .Setup(x => x.UploadHallOfFamePhotoAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResponse);

        EditHallOfFamePhotoRequest? captured = null;
        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _hallOfFameApi
            .Setup(x => x.EditHallOfFamePhotoAsync(InductionId, It.IsAny<EditHallOfFamePhotoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditHallOfFamePhotoRequest, CancellationToken>((_, request, _) => captured = request)
            .ReturnsAsync(editResponse);

        var cut = RenderEditor(() => { });
        var input = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "photo.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Input.Photo.ShouldNotBeNull().Path.ShouldBe("hall-of-fame/photo/e2e.png");
        captured.Input.FocalX.ShouldBe(50);
        captured.Input.FocalY.ShouldBe(50);
        captured.Input.Zoom.ShouldBe(100);
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
        _hallOfFameApi
            .Setup(x => x.EditHallOfFamePhotoAsync(InductionId, It.IsAny<EditHallOfFamePhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var savedCount = 0;
        var cut = RenderEditor(() => savedCount++);
        await cut.Find(".ehp-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        savedCount.ShouldBe(0);
        _toastService.Current.ShouldBeNull();
    }
}