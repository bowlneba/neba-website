using Neba.Api.Seasons.ListTournamentsInSeason;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Seasons.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonRequestValidatorTests
{
    private readonly ListTournamentsInSeasonRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when SeasonId is a valid 26-character ULID")]
    public void Validate_ShouldSucceed_WhenSeasonIdIsValid()
    {
        var result = _validator.Validate(new ListTournamentsInSeasonRequest { SeasonId = "01000000000000000000000001" });

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with SeasonIdRequired error when SeasonId is null")]
    public void Validate_ShouldFail_WhenSeasonIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new ListTournamentsInSeasonRequest { SeasonId = null });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ListTournamentsInSeasonRequest.SeasonId)
            && e.ErrorCode == "ListTournamentsInSeasonRequest.SeasonIdRequired"
            && e.ErrorMessage == "Season ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with SeasonIdRequired error when SeasonId is empty or whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenSeasonIdIsEmptyOrWhitespace(string seasonId)
    {
        var result = _validator.Validate(new ListTournamentsInSeasonRequest { SeasonId = seasonId });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ListTournamentsInSeasonRequest.SeasonId)
            && e.ErrorCode == "ListTournamentsInSeasonRequest.SeasonIdRequired"
            && e.ErrorMessage == "Season ID is required.");
    }

    [Theory(DisplayName = "Validate should fail with SeasonIdInvalidLength error when SeasonId is not 26 characters")]
    [InlineData("SHORT")]
    [InlineData("01000000000000000000000001EXTRA")]
    public void Validate_ShouldFail_WhenSeasonIdIsNotCorrectLength(string seasonId)
    {
        var result = _validator.Validate(new ListTournamentsInSeasonRequest { SeasonId = seasonId });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(ListTournamentsInSeasonRequest.SeasonId)
            && e.ErrorCode == "ListTournamentsInSeasonRequest.SeasonIdInvalidLength"
            && e.ErrorMessage == "Season ID must be a 26-character ULID.");
    }
}
