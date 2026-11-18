using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Features.Series.EditSeriesLogo;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.EditSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesLogoRequestValidatorTests
{
    private readonly EditSeriesLogoRequestValidator _validator = new();

    private static EditSeriesLogoRequest Request(string? id = null, SeriesLogoInput? logo = null, bool nullLogo = false)
        => new()
        {
            Id = id ?? new string('3', 26),
            Input = new EditSeriesLogoInput { Logo = nullLogo ? null : logo ?? SeriesLogoInputFactory.Create() },
        };

    [Theory(DisplayName = "Validate should pass for a logo or for a null logo (removal)")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validate_ShouldPass(bool nullLogo)
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(Request(nullLogo: nullLogo), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail for an empty or wrong-length id")]
    [InlineData("", "EditSeriesLogoRequest.IdRequired")]
    [InlineData("short", "EditSeriesLogoRequest.IdInvalidLength")]
    public async Task Validate_ShouldFail_ForBadId(string id, string code)
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(Request(id: id), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == code);
    }

    [Fact(DisplayName = "Validate should fail for a logo with missing fields or a non-positive size")]
    public async Task Validate_ShouldFail_ForBadLogoFields()
    {
        // Arrange
        var logo = SeriesLogoInputFactory.Create(container: "", path: "", contentType: "", sizeInBytes: 0);

        // Act
        var result = await _validator.ValidateAsync(Request(logo: logo), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.ErrorCode).ShouldBe(
            [
                "EditSeriesLogoRequest.LogoContainerRequired",
                "EditSeriesLogoRequest.LogoPathRequired",
                "EditSeriesLogoRequest.LogoContentTypeRequired",
                "EditSeriesLogoRequest.LogoSizeInvalid",
            ],
            ignoreOrder: true);
    }
}