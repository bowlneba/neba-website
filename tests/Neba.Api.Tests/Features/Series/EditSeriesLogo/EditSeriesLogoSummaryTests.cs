using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Features.Series.EditSeriesLogo;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.EditSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesLogoSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange & Act
        var summary = new EditSeriesLogoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.EditSeriesLogo");
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new EditSeriesLogoSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([204, 400, 401, 403, 404, 422]);
    }

    [Fact(DisplayName = "Constructor should give the request an example with a logo")]
    public void Constructor_ShouldGiveTheRequestAnExample()
    {
        // Arrange & Act
        var summary = new EditSeriesLogoSummary();

        // Assert
        var example = summary.RequestExamples.Single().Value.ShouldBeOfType<EditSeriesLogoRequest>();
        example.Id.Length.ShouldBe(26);
        example.Input.Logo.ShouldNotBeNull();
        example.Input.Logo.ContentType.ShouldBe("image/png");
    }
}
