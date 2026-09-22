using Neba.Api.Features.Tournaments.CancelTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CancelTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class CancelTournamentRequestValidatorTests
{
    private const string ValidId = "01000000000000000000000001";

    private readonly CancelTournamentRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        var result = _validator.Validate(new CancelTournamentRequest { Id = ValidId });

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired error when Id is null")]
    public void Validate_ShouldFail_WhenIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new CancelTournamentRequest { Id = null });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(CancelTournamentRequest.Id)
            && e.ErrorCode == "CancelTournamentRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength error when Id is not 26 characters")]
    public void Validate_ShouldFail_WhenIdIsNotCorrectLength()
    {
        var result = _validator.Validate(new CancelTournamentRequest { Id = "SHORT" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(CancelTournamentRequest.Id)
            && e.ErrorCode == "CancelTournamentRequest.IdInvalidLength");
    }
}
