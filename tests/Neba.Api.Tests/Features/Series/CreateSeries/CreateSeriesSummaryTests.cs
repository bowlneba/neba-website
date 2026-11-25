using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.CreateSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.CreateSeries;

[UnitTest]
[Component("Series")]
public sealed class CreateSeriesSummaryTests
{
    [Fact(DisplayName = "Constructor should set the Summary and a Description that names the permission")]
    public void Constructor_ShouldSetSummaryAndDescription()
    {
        // Arrange

        // Act
        var summary = new CreateSeriesSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldNotBeNullOrWhiteSpace();
        summary.Description.ShouldContain("Series.CreateSeries");
        summary.Description.ShouldContain("ordered tournaments");
    }

    [Fact(DisplayName = "Constructor should register the documented responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange

        // Act
        var summary = new CreateSeriesSummary();

        // Assert
        summary.Responses.Keys.Order().ShouldBe([201, 400, 401, 403, 409, 422]);
        summary.Responses[409].ShouldBe("A tournament is already in another Series.");
        summary.Responses[422].ShouldContain("tournament");
    }

    [Fact(DisplayName = "Constructor should give the 201 response an example with a 26-character Series id")]
    public void Constructor_ShouldGiveTheCreatedResponseAnExample()
    {
        // Arrange & Act
        var summary = new CreateSeriesSummary();

        // Assert
        var example = SummaryExamples.For(summary, 201).ShouldBeOfType<CreatedSeriesResponse>();
        example.SeriesId.Length.ShouldBe(26);
    }
}