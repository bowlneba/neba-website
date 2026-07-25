using Neba.Api.Features.Tournaments.DeleteTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.DeleteTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class DeleteTournamentRequestValidatorTests
{
    private const string ValidId = "01000000000000000000000001";

    private readonly DeleteTournamentRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        var result = _validator.Validate(new DeleteTournamentRequest { Id = ValidId });

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired error when Id is null")]
    public void Validate_ShouldFail_WhenIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new DeleteTournamentRequest { Id = null });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeleteTournamentRequest.Id)
            && e.ErrorCode == "DeleteTournamentRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength error when Id is not 26 characters")]
    public void Validate_ShouldFail_WhenIdIsNotCorrectLength()
    {
        var result = _validator.Validate(new DeleteTournamentRequest { Id = "SHORT" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(DeleteTournamentRequest.Id)
            && e.ErrorCode == "DeleteTournamentRequest.IdInvalidLength");
    }
}
