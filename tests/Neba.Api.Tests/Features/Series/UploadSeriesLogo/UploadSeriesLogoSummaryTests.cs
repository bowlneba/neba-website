using Neba.Api.Features.Series.UploadSeriesLogo;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.UploadSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class UploadSeriesLogoSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permissions")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new UploadSeriesLogoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.CreateSeries");
        summary.Description.ShouldContain("Series.EditSeriesLogo");
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new UploadSeriesLogoSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([200, 400, 401, 403]);
    }
}
