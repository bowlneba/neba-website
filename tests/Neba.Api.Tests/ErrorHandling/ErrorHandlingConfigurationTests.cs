using Neba.Api.ErrorHandling;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ErrorHandling;

[UnitTest]
[Component("ErrorHandling")]
public sealed class ErrorHandlingConfigurationTests
{
    [Theory(DisplayName = "TitleFor should return the fixed title for each status the API sends and the HTTP reason phrase for others")]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(409, "Conflict")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(503, "Service Unavailable")]
    public void TitleFor_ShouldReturnFixedTitle(int status, string expected)
    {
        // Arrange & Act
        var title = ErrorHandlingConfiguration.TitleFor(status);

        // Assert
        title.ShouldBe(expected);
    }

    [Fact(DisplayName = "TitleFor should return a generic title for a status with no reason phrase")]
    public void TitleFor_ShouldReturnGenericTitle_WhenStatusHasNoReasonPhrase()
    {
        // Arrange & Act
        var title = ErrorHandlingConfiguration.TitleFor(799);

        // Assert
        title.ShouldBe("Error");
    }
}