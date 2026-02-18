using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Pages;

namespace Neba.Website.Tests.Pages;

[UnitTest]
[Component("Website.Pages.Error")]
public sealed class ErrorTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ErrorTests()
    {
        _ctx = new BunitContext();

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should display default error message when no code is provided")]
    public void Render_ShouldShowDefaultError_WhenNoCodeProvided()
    {
        // Arrange & Act
        var cut = _ctx.Render<Error>();

        // Assert
        cut.Markup.ShouldContain("Something Went Wrong");
        cut.Markup.ShouldContain("An error occurred while processing your request");
    }

    [Theory(DisplayName = "Should display correct error title for error code")]
    [InlineData(429, "Too Many Requests", TestDisplayName = "429 - Too Many Requests")]
    [InlineData(500, "Server Error", TestDisplayName = "500 - Server Error")]
    [InlineData(502, "Bad Gateway", TestDisplayName = "502 - Bad Gateway")]
    [InlineData(503, "Service Unavailable", TestDisplayName = "503 - Service Unavailable")]
    [InlineData(504, "Gateway Timeout", TestDisplayName = "504 - Gateway Timeout")]
    public void ErrorTitle_ShouldReturnCorrectTitle_WhenCodeProvided(int code, string expectedTitle)
    {
        // Arrange
        _ctx.Services.AddRouting();
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = _ctx.Render<Error>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("code", code));

        // Assert
        cut.Markup.ShouldContain(expectedTitle);
    }

    [Theory(DisplayName = "Should display correct error description for error code")]
    [InlineData(429, "You've made too many requests", TestDisplayName = "429 - Rate limit message")]
    [InlineData(500, "An unexpected error occurred on our server", TestDisplayName = "500 - Server error message")]
    [InlineData(502, "We're having trouble connecting to our services", TestDisplayName = "502 - Bad gateway message")]
    [InlineData(503, "The service is temporarily unavailable", TestDisplayName = "503 - Service unavailable message")]
    [InlineData(504, "The request took too long to process", TestDisplayName = "504 - Timeout message")]
    public void ErrorDescription_ShouldReturnCorrectDescription_WhenCodeProvided(int code, string expectedDescription)
    {
        // Arrange
        _ctx.Services.AddRouting();
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = _ctx.Render<Error>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("code", code));

        // Assert
        cut.Markup.ShouldContain(expectedDescription);
    }

    [Fact(DisplayName = "Should display error code when provided")]
    public void Render_ShouldDisplayCode_WhenCodeProvided()
    {
        // Arrange
        _ctx.Services.AddRouting();
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = _ctx.Render<Error>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("code", 500));

        // Assert
        cut.Markup.ShouldContain(">500<");
    }

    [Fact(DisplayName = "Should not display error code section when code is null")]
    public void Render_ShouldNotDisplayCode_WhenCodeIsNull()
    {
        // Arrange & Act
        var cut = _ctx.Render<Error>();

        // Assert - the large error code div should not be present
        cut.Markup.ShouldNotContain("text-8xl");
    }

    [Fact(DisplayName = "Should display request ID when HttpContext is available")]
    public void Render_ShouldShowRequestId_WhenHttpContextAvailable()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
        mockHttpContext.Setup(x => x.TraceIdentifier).Returns("test-trace-id-123");

        // Act
        var cut = _ctx.Render<Error>(parameters => parameters
            .Add(p => p.HttpContext, mockHttpContext.Object));

        // Assert
        cut.Markup.ShouldContain("Request ID:");
        cut.Markup.ShouldContain("test-trace-id-123");
    }

    [Fact(DisplayName = "Should not display request ID section when no trace identifier available")]
    public void Render_ShouldNotShowRequestId_WhenNoTraceIdentifier()
    {
        // Arrange & Act
        var cut = _ctx.Render<Error>();

        // Assert
        cut.Markup.ShouldNotContain("Request ID:");
    }

    [Fact(DisplayName = "Should render Return Home link")]
    public void Render_ShouldContainHomeLink_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<Error>();

        // Assert
        var homeLink = cut.Find("a[href='/']");
        homeLink.TextContent.ShouldContain("Return Home");
    }

    [Fact(DisplayName = "Should render Go Back button")]
    public void Render_ShouldContainBackButton_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<Error>();

        // Assert
        var backButton = cut.Find("button[onclick='history.back()']");
        backButton.TextContent.ShouldContain("Go Back");
    }

    [Fact(DisplayName = "Should render page title with error title")]
    public void Render_ShouldContainPageTitle_WhenRendered()
    {
        // Arrange
        _ctx.Services.AddRouting();
        var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = _ctx.Render<Error>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("code", 500));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Server Error"));

        // Assert
        cut.Markup.ShouldContain("Server Error");
    }
}