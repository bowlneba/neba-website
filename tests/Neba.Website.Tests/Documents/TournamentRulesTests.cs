using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.Documents.GetDocument;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Services;

using Refit;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.TournamentRules")]
public sealed class TournamentRulesTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IDocumentsApi> _mockDocumentsApi;

    public TournamentRulesTests()
    {
        _mockDocumentsApi = new Mock<IDocumentsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var apiExecutor = new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance);

        _ctx.Services.AddSingleton(_mockDocumentsApi.Object);
        _ctx.Services.AddSingleton(apiExecutor);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        // Arrange
        SetupSuccessResponse("<p>Content</p>");

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        cut.Markup.ShouldContain("NEBA Tournament Rules");
    }

    [Fact(DisplayName = "Should render page description")]
    public void Render_ShouldShowPageDescription_WhenRendered()
    {
        // Arrange
        SetupSuccessResponse("<p>Content</p>");

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        cut.Markup.ShouldContain("Official rules and regulations for NEBA tournaments");
    }

    [Fact(DisplayName = "Should call GetDocumentAsync with 'tournament-rules' on initialization")]
    public void OnInitialized_ShouldCallGetDocumentAsync_WithTournamentRulesDocumentName()
    {
        // Arrange
        SetupSuccessResponse("<h1>Tournament Rules</h1>");

        // Act
        _ctx.Render<TournamentRules>();

        // Assert
        _mockDocumentsApi.Verify(
            x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should display content when API call succeeds")]
    public void Render_ShouldShowContent_WhenApiCallSucceeds()
    {
        // Arrange
        const string html = "<h1>Rule 1</h1><p>Description of rule 1</p>";
        SetupSuccessResponse(html);

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        var document = cut.FindComponent<NebaDocument>();
        document.Instance.Content.ShouldNotBeNull();
        document.Instance.Content.Value.Value.ShouldContain("Rule 1");
        document.Instance.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should display error when API returns failure status")]
    public void Render_ShouldShowError_WhenApiReturnsFailureStatus()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        var document = cut.FindComponent<NebaDocument>();
        document.Instance.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        document.Instance.ErrorMessage.ShouldContain("Failed to load tournament rules");
        document.Instance.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should display error when API returns not found")]
    public void Render_ShouldShowError_WhenApiReturnsNotFound()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.NotFound);

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        var document = cut.FindComponent<NebaDocument>();
        document.Instance.Content.ShouldBeNull();
        document.Instance.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        document.Instance.IsLoading.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should configure NebaDocument with correct parameters")]
    public void Render_ShouldSetCorrectParameters_OnNebaDocument()
    {
        // Arrange
        SetupSuccessResponse("<h1>Tournament Rules</h1>");

        // Act
        var cut = _ctx.Render<TournamentRules>();

        // Assert
        var document = cut.FindComponent<NebaDocument>();
        document.Instance.LoadingText.ShouldBe("Loading tournament rules...");
        document.Instance.ErrorTitle.ShouldBe("Error Loading Tournament Rules");
        document.Instance.ShowTableOfContents.ShouldBeTrue();
        document.Instance.TableOfContentsTitle.ShouldBe("Contents");
        document.Instance.HeadingLevels.ShouldBe("h1, h2");
        document.Instance.DocumentId.ShouldBe("tournament-rules");
    }

    private void SetupSuccessResponse(string html)
    {
        var response = new Mock<IApiResponse<GetDocumentResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.Content).Returns(new GetDocumentResponse { Html = html });
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<GetDocumentResponse>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.Content).Returns((GetDocumentResponse?)null);
        response.Setup(r => r.StatusCode).Returns(statusCode);

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}