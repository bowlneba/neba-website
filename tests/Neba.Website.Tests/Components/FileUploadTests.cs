using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.FileUpload")]
public sealed class FileUploadTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _moduleInterop;

    public FileUploadTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _moduleInterop = _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js");
        _moduleInterop.Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        _ctx.Services.AddSingleton<ILogger<FileUpload>>(NullLogger<FileUpload>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    private static Task<ErrorOr<UploadedFileResponse>> SucceedAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => Task.FromResult<ErrorOr<UploadedFileResponse>>(new UploadedFileResponse
        {
            Container = "news",
            Path = $"uploads/{fileName}",
            FileName = fileName,
            ContentType = contentType,
            SizeInBytes = 10,
            Url = new Uri($"https://storage.example.com/news/uploads/{fileName}")
        });

    private static Task<ErrorOr<UploadedFileResponse>> FailAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => Task.FromResult<ErrorOr<UploadedFileResponse>>(Error.Validation("File.Invalid", "Invalid file type."));

    private IRenderedComponent<FileUpload> Render(
        Func<Stream, string, string, IProgress<int>, CancellationToken, Task<ErrorOr<UploadedFileResponse>>> uploadDelegate,
        int maxFiles = 1,
        long? maxFileSizeBytes = null,
        EventCallback<UploadedFileResponse>? onFileUploaded = null,
        EventCallback<UploadedFileResponse>? onFileRemoved = null)
        => _ctx.Render<FileUpload>(parameters =>
        {
            parameters.Add(p => p.OnUploadRequestedAsync, uploadDelegate);
            parameters.Add(p => p.MaxFiles, maxFiles);

            if (maxFileSizeBytes.HasValue)
            {
                parameters.Add(p => p.MaxFileSizeBytes, maxFileSizeBytes.Value);
            }

            if (onFileUploaded.HasValue)
            {
                parameters.Add(p => p.OnFileUploaded, onFileUploaded.Value);
            }

            if (onFileRemoved.HasValue)
            {
                parameters.Add(p => p.OnFileRemoved, onFileRemoved.Value);
            }
        });

    [Fact(DisplayName = "Should render a drop zone div with the neba-file-upload id prefix")]
    public void Render_ShouldRenderDropZoneDiv_WithNebaFileUploadIdPrefix()
    {
        // Act
        var cut = Render(SucceedAsync);

        // Assert
        cut.Find("div[id^='neba-file-upload-']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should call initialize JS function on first render")]
    public void OnAfterRender_ShouldCallInitialize_WhenFirstRender()
    {
        // Act
        Render(SucceedAsync);

        // Assert
        _moduleInterop.VerifyInvoke("initialize");
    }

    [Fact(DisplayName = "Should invoke the upload delegate with the selected file's name and content type")]
    public async Task HandleFilesSelected_ShouldInvokeUploadDelegate_WithFileNameAndContentType()
    {
        // Arrange
        string? capturedFileName = null;
        string? capturedContentType = null;

        Task<ErrorOr<UploadedFileResponse>> CapturingUpload(Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        {
            capturedFileName = fileName;
            capturedContentType = contentType;
            return SucceedAsync(stream, fileName, contentType, progress, ct);
        }

        var cut = Render(CapturingUpload);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));

        // Assert
        capturedFileName.ShouldBe("header.png");
        capturedContentType.ShouldBe("image/png");
    }

    [Fact(DisplayName = "Should mark the file Uploaded and raise OnFileUploaded when the upload succeeds")]
    public async Task HandleFilesSelected_ShouldMarkUploadedAndRaiseCallback_WhenUploadSucceeds()
    {
        // Arrange
        UploadedFileResponse? received = null;
        var cut = Render(
            SucceedAsync,
            onFileUploaded: EventCallback.Factory.Create<UploadedFileResponse>(this, r => received = r));
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find(".neba-file-upload-item-status--success").TextContent.ShouldBe("Uploaded"));
        received.ShouldNotBeNull();
        received.Path.ShouldBe("uploads/header.png");
    }

    [Fact(DisplayName = "Should mark the file Failed with the error description when the upload returns an error")]
    public async Task HandleFilesSelected_ShouldMarkFailedWithErrorDescription_WhenUploadReturnsError()
    {
        // Arrange
        var cut = Render(FailAsync);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".neba-file-upload-item-status--error").TextContent.ShouldBe("Invalid file type."));
    }

    [Fact(DisplayName = "Should apply the failed name class to the file name when the upload returns an error")]
    public async Task HandleFilesSelected_ShouldApplyFailedNameClass_WhenUploadReturnsError()
    {
        // Arrange
        var cut = Render(FailAsync);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".neba-file-upload-item-name").ClassList.ShouldContain("neba-file-upload-item-name--failed"));
    }

    [Fact(DisplayName = "Should not apply the failed name class to the file name when the upload succeeds")]
    public async Task HandleFilesSelected_ShouldNotApplyFailedNameClass_WhenUploadSucceeds()
    {
        // Arrange
        var cut = Render(SucceedAsync);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "header.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".neba-file-upload-item-name").ClassList.ShouldNotContain("neba-file-upload-item-name--failed"));
    }

    [Fact(DisplayName = "Should mark the file Failed with a size message when the file exceeds MaxFileSizeBytes")]
    public async Task HandleFilesSelected_ShouldMarkFailedWithSizeMessage_WhenFileExceedsMaxFileSizeBytes()
    {
        // Arrange
        var cut = Render(SucceedAsync, maxFileSizeBytes: 100);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary(new byte[1000], "too-big.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".neba-file-upload-item-status--error").TextContent.ShouldContain("exceeds the maximum allowed size"));
    }

    [Fact(DisplayName = "Should replace the previous file when a new file is selected in single-file mode")]
    public async Task HandleFilesSelected_ShouldReplacePreviousFile_WhenMaxFilesIsOne()
    {
        // Arrange
        var cut = Render(SucceedAsync, maxFiles: 1);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "first.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item").Count.ShouldBe(1));

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([2], "second.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var items = cut.FindAll(".neba-file-upload-item");
            items.Count.ShouldBe(1);
            items[0].QuerySelector(".neba-file-upload-item-name")!.TextContent.ShouldBe("second.png");
        });
    }

    [Fact(DisplayName = "Should track each file independently when multiple files are selected in multi-file mode")]
    public async Task HandleFilesSelected_ShouldTrackEachFileIndependently_WhenMaxFilesAllowsMultiple()
    {
        // Arrange
        var cut = Render(SucceedAsync, maxFiles: 5);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "one.png", contentType: "image/png"),
            InputFileContent.CreateFromBinary([2], "two.pdf", contentType: "application/pdf")));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var names = cut.FindAll(".neba-file-upload-item-name").Select(e => e.TextContent).ToArray();
            names.ShouldBe(["one.png", "two.pdf"], ignoreOrder: true);
        });
    }

    [Fact(DisplayName = "Should cap the tracked files at the remaining slots when the selection exceeds MaxFiles")]
    public async Task HandleFilesSelected_ShouldCapAtRemainingSlots_WhenSelectionExceedsMaxFiles()
    {
        // Arrange
        var cut = Render(SucceedAsync, maxFiles: 2);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "one.png", contentType: "image/png"),
            InputFileContent.CreateFromBinary([2], "two.png", contentType: "image/png"),
            InputFileContent.CreateFromBinary([3], "three.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item").Count.ShouldBe(2));
    }

    [Fact(DisplayName = "Should request preview URLs from JS and render a thumbnail for image files")]
    public async Task HandleFilesSelected_ShouldRenderThumbnail_WhenJsReturnsAPreviewUrl()
    {
        // Arrange
        _moduleInterop.Setup<string?[]>("getPreviewUrls", _ => true).SetResult(["blob:preview-url"]);

        var cut = Render(SucceedAsync);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("img.neba-file-upload-thumbnail").GetAttribute("src").ShouldBe("blob:preview-url"));
    }

    [Fact(DisplayName = "Should render a generic icon when JS returns no preview URL")]
    public async Task HandleFilesSelected_ShouldRenderGenericIcon_WhenNoPreviewUrl()
    {
        // Arrange
        var cut = Render(SucceedAsync);
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "document.pdf", contentType: "application/pdf")));

        // Assert
        await cut.WaitForAssertionAsync(() => cut.FindAll("img.neba-file-upload-thumbnail").Count.ShouldBe(0));
    }

    [Fact(DisplayName = "Should cancel the upload and remove the file from the list when Remove is clicked")]
    public async Task RemoveFile_ShouldCancelUploadAndRemoveFromList_WhenClicked()
    {
        // Arrange
        var uploadStarted = false;
        var tcs = new TaskCompletionSource<ErrorOr<UploadedFileResponse>>();
        CancellationToken capturedToken = default;

#pragma warning disable VSTHRD003
        Task<ErrorOr<UploadedFileResponse>> PendingUpload(Stream _, string __, string ___, IProgress<int> ____, CancellationToken ct)
        {
            capturedToken = ct;
            uploadStarted = true;
            return tcs.Task;
        }
#pragma warning restore VSTHRD003

        var cut = Render(PendingUpload);
        var input = cut.FindComponent<InputFile>();

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForStateAsync(() => uploadStarted);

        // Act
        var removeButton = cut.Find("button.neba-file-upload-remove");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        capturedToken.IsCancellationRequested.ShouldBeTrue();
        cut.FindAll(".neba-file-upload-item").Count.ShouldBe(0);

        // Cleanup: let the orphaned upload task complete so it doesn't outlive the test.
        tcs.TrySetResult(Error.Failure());
    }

    [Fact(DisplayName = "Should raise OnFileRemoved with the uploaded response when a successfully uploaded file is removed")]
    public async Task RemoveFile_ShouldRaiseOnFileRemovedWithUploadedResponse_WhenFileWasUploaded()
    {
        // Arrange
        UploadedFileResponse? removed = null;
        var cut = Render(
            SucceedAsync,
            onFileRemoved: EventCallback.Factory.Create<UploadedFileResponse>(this, r => removed = r));
        var input = cut.FindComponent<InputFile>();

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.Find(".neba-file-upload-item-status--success").ShouldNotBeNull());

        // Act
        var removeButton = cut.Find("button.neba-file-upload-remove");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        removed.ShouldNotBeNull();
        removed.Path.ShouldBe("uploads/header.png");
    }

    [Fact(DisplayName = "Should not raise OnFileRemoved when a file that failed to upload is removed")]
    public async Task RemoveFile_ShouldNotRaiseOnFileRemoved_WhenFileFailedToUpload()
    {
        // Arrange
        var onFileRemovedCalled = false;
        var cut = Render(
            FailAsync,
            onFileRemoved: EventCallback.Factory.Create<UploadedFileResponse>(this, _ => onFileRemovedCalled = true));
        var input = cut.FindComponent<InputFile>();

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.Find(".neba-file-upload-item-status--error").ShouldNotBeNull());

        // Act
        var removeButton = cut.Find("button.neba-file-upload-remove");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        onFileRemovedCalled.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should call revokePreviewUrl on JS when a file with a thumbnail is removed")]
    public async Task RemoveFile_ShouldCallRevokePreviewUrl_WhenFileHadAPreview()
    {
        // Arrange
        _moduleInterop.Setup<string?[]>("getPreviewUrls", _ => true).SetResult(["blob:preview-url"]);

        var cut = Render(SucceedAsync);
        var input = cut.FindComponent<InputFile>();

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.Find("img.neba-file-upload-thumbnail").ShouldNotBeNull());

        // Act
        var removeButton = cut.Find("button.neba-file-upload-remove");
        await cut.InvokeAsync(() => removeButton.Click());

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("revokePreviewUrl");
        invocation.Arguments[0].ShouldBe("blob:preview-url");
    }

    [Fact(DisplayName = "Should raise OnBusyChanged(true) once upload starts and OnBusyChanged(false) once it completes")]
    public async Task OnBusyChanged_ShouldToggle_AsUploadStartsAndCompletes()
    {
        // Arrange
        var busyStates = new List<bool>();
        var tcs = new TaskCompletionSource<ErrorOr<UploadedFileResponse>>();

#pragma warning disable VSTHRD003
        Task<ErrorOr<UploadedFileResponse>> PendingUpload(Stream _, string __, string ___, IProgress<int> ____, CancellationToken _____)
            => tcs.Task;
#pragma warning restore VSTHRD003

        var cut = _ctx.Render<FileUpload>(parameters => parameters
            .Add(p => p.OnUploadRequestedAsync, PendingUpload)
            .Add(p => p.OnBusyChanged, EventCallback.Factory.Create<bool>(this, b => busyStates.Add(b))));
        var input = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForStateAsync(() => busyStates.Contains(true));

        tcs.SetResult(new UploadedFileResponse
        {
            Container = "news",
            Path = "uploads/header.png",
            FileName = "header.png",
            ContentType = "image/png",
            SizeInBytes = 3,
            Url = new Uri("https://storage.example.com/news/uploads/header.png")
        });

        // Assert
        await cut.WaitForAssertionAsync(() => busyStates.ShouldContain(false));
        busyStates.ShouldBe([true, false]);
    }

    [Fact(DisplayName = "Should call dispose JS function when component is disposed")]
    public async Task DisposeAsync_ShouldCallDisposeJs_WhenDisposed()
    {
        // Arrange
        var cut = Render(SucceedAsync);

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        _moduleInterop.VerifyInvoke("dispose", 1);
    }

    [Fact(DisplayName = "Should cancel pending uploads when the component is disposed")]
    public async Task DisposeAsync_ShouldCancelPendingUploads_WhenDisposed()
    {
        // Arrange
        var uploadStarted = false;
        var tcs = new TaskCompletionSource<ErrorOr<UploadedFileResponse>>();
        CancellationToken capturedToken = default;

#pragma warning disable VSTHRD003
        Task<ErrorOr<UploadedFileResponse>> PendingUpload(Stream _, string __, string ___, IProgress<int> ____, CancellationToken ct)
        {
            capturedToken = ct;
            uploadStarted = true;
            return tcs.Task;
        }
#pragma warning restore VSTHRD003

        var cut = Render(PendingUpload);
        var input = cut.FindComponent<InputFile>();

        await cut.InvokeAsync(() => input.UploadFiles(
            InputFileContent.CreateFromBinary([1], "header.png", contentType: "image/png")));
        await cut.WaitForStateAsync(() => uploadStarted);

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert
        capturedToken.IsCancellationRequested.ShouldBeTrue();

        // Cleanup: let the orphaned upload task complete so it doesn't outlive the test.
        tcs.TrySetResult(Error.Failure());
    }

    [Fact(DisplayName = "Should not throw when DisposeAsync encounters a JSDisconnectedException on dispose")]
    public async Task DisposeAsync_ShouldNotThrow_WhenJSDisconnectedExceptionOccurs()
    {
        // Arrange
        _moduleInterop.SetupVoid("dispose", _ => true).SetException(new JSDisconnectedException("Disconnected"));

        var cut = Render(SucceedAsync);

        // Act
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        // Assert — if the exception was not swallowed, InvokeAsync would have thrown before this assertion
        cut.Instance.ShouldNotBeNull();
    }
}