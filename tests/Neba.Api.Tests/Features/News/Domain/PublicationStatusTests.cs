using Neba.Api.Features.News.Domain;

namespace Neba.Api.Tests.Features.News.Domain;

public sealed class PublicationStatusTests
{
    [Fact(DisplayName = "PublicationStatus should have 2 defined values")]
    public void PublicationStatus_ShouldHaveTwoDefinedValues()
    {
        // Arrange & Act
        var values = PublicationStatus.List;

        // Assert
        values.Count.ShouldBe(2);
    }

    [Theory(DisplayName = "PublicationStatus should have correct name and value for all values")]
    [InlineData("Draft", 0, TestDisplayName = "Draft status maps to 0")]
    [InlineData("Published", 1, TestDisplayName = "Published status maps to 1")]
    public void PublicationStatus_ShouldHaveCorrectNameAndValue(string expectedName, int expectedValue)
    {
        // Arrange & Act
        var publicationStatus = PublicationStatus.List.FirstOrDefault(status => status.Name == expectedName);

        // Assert
        publicationStatus.ShouldNotBeNull();
        publicationStatus.Value.ShouldBe(expectedValue);
    }
}