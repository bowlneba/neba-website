using Bunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Pages;

namespace Neba.Website.Tests.Pages;

/// <summary>
/// Minimal tests for Counter page - this page is a demo/example and will be removed.
/// Tests are included for SonarQube code coverage requirements.
/// </summary>
[UnitTest]
[Component("Website.Pages.Counter")]
public sealed class CounterTests : IDisposable
{
    private readonly BunitContext _ctx;

    public CounterTests()
    {
        _ctx = new BunitContext();

        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
        _ctx.Services.AddSingleton(mockWebHostEnvironment.Object);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render with initial count of zero")]
    public void Render_ShouldShowZero_WhenInitiallyRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<Counter>();

        // Assert
        cut.Markup.ShouldContain("Current count:");
        cut.Markup.ShouldContain(">0<");
    }

    [Fact(DisplayName = "Should increment count when button is clicked")]
    public void IncrementCount_ShouldIncreaseByOne_WhenButtonClicked()
    {
        // Arrange
        var cut = _ctx.Render<Counter>();

        // Act - find the "Click me" button (first primary button)
        var incrementButton = cut.Find("button.neba-btn-primary");
        incrementButton.Click();

        // Assert
        cut.Markup.ShouldContain(">1<");
    }

    [Fact(DisplayName = "Should increment count multiple times")]
    public void IncrementCount_ShouldAccumulate_WhenButtonClickedMultipleTimes()
    {
        // Arrange
        var cut = _ctx.Render<Counter>();

        // Act
        var incrementButton = cut.Find("button.neba-btn-primary");
        incrementButton.Click();
        incrementButton.Click();
        incrementButton.Click();

        // Assert
        cut.Markup.ShouldContain(">3<");
    }

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldContainPageTitle_WhenRendered()
    {
        // Arrange & Act
        var cut = _ctx.Render<Counter>();

        // Assert
        cut.Markup.ShouldContain("Counter");
    }
}