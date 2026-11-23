using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Features.Series.EditSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.EditSeries;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesRequestValidatorTests
{
    private readonly EditSeriesRequestValidator _validator = new();

    private static EditSeriesRequest Request(string? id = null, string? name = null, IReadOnlyList<SeriesStageInput>? stages = null, SeriesFinaleInput? finale = null)
        => new()
        {
            Id = id ?? new string('3', 26),
            Series = new EditSeriesInput
            {
                Name = name ?? "Series",
                Stages = stages ?? [SeriesStageInputFactory.Create(tournamentId: new string('1', 26))],
                Finale = finale,
            },
        };

    [Fact(DisplayName = "Validate should pass for a valid request")]
    public async Task Validate_ShouldPass_ForValidRequest()
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail for an empty or wrong-length id")]
    [InlineData("", "EditSeriesRequest.IdRequired")]
    [InlineData("short", "EditSeriesRequest.IdInvalidLength")]
    public async Task Validate_ShouldFail_ForBadId(string id, string code)
    {
        // Arrange & Act
        var result = await _validator.ValidateAsync(Request(id: id), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldContain(e => e.ErrorCode == code);
    }

    [Fact(DisplayName = "Validate should fail for an empty name and for a name over 127 characters")]
    public async Task Validate_ShouldFail_ForBadName()
    {
        // Arrange & Act
        var empty = await _validator.ValidateAsync(Request(name: ""), TestContext.Current.CancellationToken);
        var tooLong = await _validator.ValidateAsync(Request(name: new string('x', 128)), TestContext.Current.CancellationToken);

        // Assert
        empty.Errors.ShouldContain(e => e.ErrorCode == "EditSeriesRequest.NameRequired");
        tooLong.Errors.ShouldContain(e => e.ErrorCode == "EditSeriesRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should run the Stage and Finale validators")]
    public async Task Validate_ShouldRunNestedValidators()
    {
        // Arrange
        var request = Request(
            stages: [SeriesStageInputFactory.Create(kind: 99, tournamentId: new string('1', 26))],
            finale: new SeriesFinaleInput { FinalistCount = 4 });

        // Act
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Select(e => e.ErrorCode).ShouldContain("SeriesStageInput.KindInvalid");
        result.Errors.Select(e => e.ErrorCode).ShouldContain("SeriesFinaleInput.DateRequired");
    }
}