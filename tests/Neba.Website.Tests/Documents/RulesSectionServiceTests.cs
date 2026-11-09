using ErrorOr;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Documents;
using Neba.Api.Contracts.Documents.GetDocument;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Services;
using Neba.Website.Server.Time;
using Neba.Website.Tests.TestSupport;

using Refit.Testing;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.RulesSectionService")]
public sealed class RulesSectionServiceTests
{
    private const string Html =
        "<h3 id=\"rule-601-invitational-tournament-rules\">Rule 601- Invitational Tournament Rules</h3><p>Eligibility</p>" +
        "<h3 id=\"rule-602-masters-tournament-rules\">Rule 602 - Masters Tournament Rules</h3><p>Masters text</p>";

    private static readonly TournamentRulesAnchor Invitational = new("invitational", 601, "Invitational");

    private readonly Mock<IDocumentsApi> _mockDocumentsApi = new(MockBehavior.Strict);
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService = new(MockBehavior.Strict);
    private readonly RulesSectionService _service;

    public RulesSectionServiceTests()
    {
        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var executor = new ApiExecutor(mockStopwatch.Object, new StubNavigationManager(), NullLogger<ApiExecutor>.Instance);
        _service = new RulesSectionService(executor, _mockDocumentsApi.Object, _mockClientTimeZoneService.Object);
    }

    private void SetupDocument(bool success = true, string html = Html, DateTimeOffset? lastUpdated = null)
    {
        using var response = new StubApiResponse<GetDocumentResponse>
        {
            IsSuccessStatusCode = success,
            Content = success ? new GetDocumentResponse { Html = html, LastUpdated = lastUpdated } : null,
            StatusCode = success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError
        };

        _mockDocumentsApi
            .Setup(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    [Fact(DisplayName = "Returns only the requested section when the document has it")]
    public async Task GetSectionAsync_ShouldReturnSection_WhenDocumentHasIt()
    {
        // Arrange
        SetupDocument();

        // Act
        var result = await _service.GetSectionAsync(Invitational, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Html.ShouldContain("Eligibility");
        result.Value.Html.ShouldNotContain("Masters text");
        result.Value.LastUpdated.ShouldBeNull();
    }

    [Fact(DisplayName = "Converts the last-updated time to the viewer's time zone")]
    public async Task GetSectionAsync_ShouldConvertLastUpdated_WhenDocumentHasTimestamp()
    {
        // Arrange
        var utc = new DateTimeOffset(2026, 3, 1, 15, 0, 0, TimeSpan.Zero);
        var local = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.FromHours(-5));
        SetupDocument(lastUpdated: utc);
        _mockClientTimeZoneService.Setup(x => x.ToLocalAsync(utc)).ReturnsAsync(local);

        // Act
        var result = await _service.GetSectionAsync(Invitational, TestContext.Current.CancellationToken);

        // Assert
        result.Value.LastUpdated.ShouldBe(local);
    }

    [Fact(DisplayName = "Returns the API error when the document request fails")]
    public async Task GetSectionAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        SetupDocument(success: false);

        // Act
        var result = await _service.GetSectionAsync(Invitational, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldNotBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "Returns NotFound when the document has no such section")]
    public async Task GetSectionAsync_ShouldReturnNotFound_WhenSectionMissing()
    {
        // Arrange
        SetupDocument(html: "<h3 id=\"rule-100-general\">Rule 100 - General</h3><p>General text</p>");

        // Act
        var result = await _service.GetSectionAsync(Invitational, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("RulesSection.NotFound");
    }
}
