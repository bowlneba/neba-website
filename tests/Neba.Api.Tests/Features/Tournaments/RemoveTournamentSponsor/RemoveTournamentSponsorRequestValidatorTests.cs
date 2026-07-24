using Neba.Api.Features.Tournaments.RemoveTournamentSponsor;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.RemoveTournamentSponsor;

[UnitTest]
[Component("Tournaments")]
public sealed class RemoveTournamentSponsorRequestValidatorTests
{
    private const string ValidTournamentId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private readonly RemoveTournamentSponsorRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        var result = _validator.Validate(new RemoveTournamentSponsorRequest
        {
            TournamentId = ValidTournamentId,
            SponsorId = ValidSponsorId
        });

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with TournamentIdRequired error when TournamentId is null")]
    public void Validate_ShouldFail_WhenTournamentIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new RemoveTournamentSponsorRequest
        {
            TournamentId = null,
            SponsorId = ValidSponsorId
        });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RemoveTournamentSponsorRequest.TournamentId)
            && e.ErrorCode == "RemoveTournamentSponsorRequest.TournamentIdRequired");
    }

    [Fact(DisplayName = "Validate should fail with TournamentIdInvalidLength error when TournamentId is not 26 characters")]
    public void Validate_ShouldFail_WhenTournamentIdIsNotCorrectLength()
    {
        var result = _validator.Validate(new RemoveTournamentSponsorRequest
        {
            TournamentId = "SHORT",
            SponsorId = ValidSponsorId
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RemoveTournamentSponsorRequest.TournamentId)
            && e.ErrorCode == "RemoveTournamentSponsorRequest.TournamentIdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdRequired error when SponsorId is null")]
    public void Validate_ShouldFail_WhenSponsorIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new RemoveTournamentSponsorRequest
        {
            TournamentId = ValidTournamentId,
            SponsorId = null
        });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RemoveTournamentSponsorRequest.SponsorId)
            && e.ErrorCode == "RemoveTournamentSponsorRequest.SponsorIdRequired");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdInvalidLength error when SponsorId is not 26 characters")]
    public void Validate_ShouldFail_WhenSponsorIdIsNotCorrectLength()
    {
        var result = _validator.Validate(new RemoveTournamentSponsorRequest
        {
            TournamentId = ValidTournamentId,
            SponsorId = "SHORT"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(RemoveTournamentSponsorRequest.SponsorId)
            && e.ErrorCode == "RemoveTournamentSponsorRequest.SponsorIdInvalidLength");
    }
}