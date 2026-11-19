using Neba.Api.Features.Series.DeleteSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.DeleteSeries;

[UnitTest]
[Component("Series")]
public sealed class DeleteSeriesRequestValidatorTests
{
    private readonly DeleteSeriesRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should pass for a 26-character id")]
    public async Task Validate_ShouldPass_ForValidId()
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(new DeleteSeriesRequest { Id = new string('0', 26) }, TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail for an empty or wrong-length id")]
    [InlineData("", "DeleteSeriesRequest.IdRequired")]
    [InlineData("short", "DeleteSeriesRequest.IdInvalidLength")]
    public async Task Validate_ShouldFail_ForBadId(string id, string code)
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(new DeleteSeriesRequest { Id = id }, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == code);
    }
}