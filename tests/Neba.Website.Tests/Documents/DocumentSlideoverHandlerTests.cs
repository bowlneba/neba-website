using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.Documents.GetDocument;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.DocumentSlideoverHandler")]
public sealed class DocumentSlideoverHandlerTests
{
    private readonly Mock<IDocumentsApi> _mockDocumentsApi;
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService;
    private readonly ApiExecutor _apiExecutor;

    public DocumentSlideoverHandlerTests()
    {
        _mockDocumentsApi = new Mock<IDocumentsApi>(MockBehavior.Strict);
        _mockClientTimeZoneService = new Mock<IClientTimeZoneService>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _apiExecutor = new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance);
    }

    [Theory(DisplayName = "GetDocumentNameFromRoute should map known routes to document names")]
    [InlineData("bylaws", "bylaws")]
    [InlineData("tournaments/rules", "tournament-rules")]
    public void GetDocumentNameFromRoute_ShouldReturnDocumentName_ForKnownRoutes(
        string route, string expectedDocumentName)
    {
        // Act
        var result = DocumentSlideoverHandler.GetDocumentNameFromRoute(route);

        // Assert
        result.ShouldBe(expectedDocumentName);
    }

    [Theory(DisplayName = "GetDocumentNameFromRoute should return null for unknown routes")]
    [InlineData("unknown/route")]
    [InlineData("")]
    [InlineData("about/bylaws")]
    public void GetDocumentNameFromRoute_ShouldReturnNull_ForUnknownRoutes(string route)
    {
        // Act
        var result = DocumentSlideoverHandler.GetDocumentNameFromRoute(route);

        // Assert
        result.ShouldBeNull();
    }

    [Theory(DisplayName = "GetDocumentTitle should return display title for known document names")]
    [InlineData("bylaws", "Bylaws")]
    [InlineData("tournament-rules", "Tournament Rules")]
    public void GetDocumentTitle_ShouldReturnDisplayTitle_ForKnownDocumentNames(
        string documentName, string expectedTitle)
    {
        // Act
        var result = DocumentSlideoverHandler.GetDocumentTitle(documentName);

        // Assert
        result.ShouldBe(expectedTitle);
    }

    [Fact(DisplayName = "GetDocumentTitle should title-case unknown document names")]
    public void GetDocumentTitle_ShouldTitleCase_UnknownDocumentNames()
    {
        // Act
        var result = DocumentSlideoverHandler.GetDocumentTitle("officer-handbook");

        // Assert
        result.ShouldBe("Officer Handbook");
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should set content when API call succeeds")]
    public async Task HandleLinkClickedAsync_ShouldSetContent_WhenApiCallSucceeds()
    {
        // Arrange
        const string html = "<h1>Bylaws Content</h1>";
        SetupSuccessResponse("bylaws", html);
        var handler = CreateHandler();
        var stateChangedCount = 0;

        // Act
        await handler.HandleLinkClickedAsync("/bylaws", () => stateChangedCount++);

        // Assert
        handler.Content.ShouldNotBeNull();
        handler.Content!.Value.Value.ShouldContain("Bylaws Content");
        handler.Title.ShouldBe("Bylaws");
        handler.IsLoading.ShouldBeFalse();
        stateChangedCount.ShouldBe(2); // Once for loading, once for result
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should set error content when API call fails")]
    public async Task HandleLinkClickedAsync_ShouldSetErrorContent_WhenApiCallFails()
    {
        // Arrange
        SetupFailureResponse("tournament-rules", System.Net.HttpStatusCode.InternalServerError);
        var handler = CreateHandler();

        // Act
        await handler.HandleLinkClickedAsync("/tournaments/rules", () => { });

        // Assert
        handler.Content.ShouldNotBeNull();
        handler.Content!.Value.Value.ShouldContain("Failed to load document");
        handler.Title.ShouldBe("Tournament Rules");
        handler.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should set not-found content for unknown routes")]
    public async Task HandleLinkClickedAsync_ShouldSetNotFoundContent_ForUnknownRoutes()
    {
        // Arrange
        var handler = CreateHandler();
        var stateChangedCount = 0;

        // Act
        await handler.HandleLinkClickedAsync("/unknown/route", () => stateChangedCount++);

        // Assert
        handler.Content.ShouldNotBeNull();
        handler.Content.Value.Value.ShouldContain("Document not found for route");
        handler.Title.ShouldBeNull();
        handler.IsLoading.ShouldBeFalse();
        stateChangedCount.ShouldBe(2); // Once for loading, once for not-found result
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should set loading state before API call")]
    public async Task HandleLinkClickedAsync_ShouldSetLoadingState_BeforeApiCall()
    {
        // Arrange
        var wasLoadingOnFirstCallback = false;
        SetupSuccessResponse("bylaws", "<p>Content</p>");
        var handler = CreateHandler();
        var callbackCount = 0;

        // Act
        await handler.HandleLinkClickedAsync("/bylaws", () =>
        {
            callbackCount++;
            if (callbackCount == 1)
            {
                wasLoadingOnFirstCallback = handler.IsLoading;
            }
        });

        // Assert
        wasLoadingOnFirstCallback.ShouldBeTrue();
        handler.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should convert LastUpdated to the viewer's local time when the API call succeeds")]
    public async Task HandleLinkClickedAsync_ShouldConvertLastUpdatedToLocalTime_WhenApiCallSucceeds()
    {
        // Arrange
        var utc = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        var local = new DateTimeOffset(2026, 3, 1, 7, 0, 0, TimeSpan.FromHours(-5));
        SetupSuccessResponse("bylaws", "<p>Content</p>", utc);
        _mockClientTimeZoneService.Setup(s => s.ToLocalAsync(utc)).ReturnsAsync(local).Verifiable();
        var handler = CreateHandler();

        // Act
        await handler.HandleLinkClickedAsync("/bylaws", () => { });

        // Assert
        handler.LastUpdated.ShouldBe(local);
        _mockClientTimeZoneService.VerifyAll();
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should not call the client time zone service when LastUpdated is null")]
    public async Task HandleLinkClickedAsync_ShouldNotCallClientTimeZoneService_WhenLastUpdatedIsNull()
    {
        // Arrange — Strict mock with no setup: an unexpected call fails the test.
        SetupSuccessResponse("bylaws", "<p>Content</p>");
        var handler = CreateHandler();

        // Act
        await handler.HandleLinkClickedAsync("/bylaws", () => { });

        // Assert
        handler.LastUpdated.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleLinkClickedAsync should trim leading slash from pathname")]
    public async Task HandleLinkClickedAsync_ShouldTrimLeadingSlash_FromPathname()
    {
        // Arrange
        SetupSuccessResponse("bylaws", "<p>Content</p>");
        var handler = CreateHandler();

        // Act
        await handler.HandleLinkClickedAsync("/bylaws", () => { });

        // Assert
        _mockDocumentsApi.Verify(
            x => x.GetDocumentAsync("bylaws", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DocumentSlideoverHandler CreateHandler()
        => new(_apiExecutor, _mockDocumentsApi.Object, _mockClientTimeZoneService.Object);

    private void SetupSuccessResponse(string documentName, string html, DateTimeOffset? lastUpdated = null)
    {
        using var response = new StubApiResponse<GetDocumentResponse>
        {
            IsSuccessStatusCode = true,
            Content = new GetDocumentResponse { Html = html, LastUpdated = lastUpdated },
            StatusCode = System.Net.HttpStatusCode.OK
        };

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync(documentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(string documentName, System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<GetDocumentResponse>
        {
            IsSuccessStatusCode = false,
            Content = (GetDocumentResponse?)null,
            StatusCode = statusCode
        };

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync(documentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}