using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.Api.Features.Tournaments.AddTournamentSponsor;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.AddTournamentSponsor;

[UnitTest]
[Component("Tournaments")]
public sealed class AddTournamentSponsorRequestValidatorTests
{
    private const string ValidId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private readonly AddTournamentSponsorRequestValidator _validator = new();

    private static AddTournamentSponsorRequest CreateRequest(
        string? id = null,
        string? sponsorId = null,
        decimal? sponsorshipAmount = null)
        => new()
        {
            Id = id ?? ValidId,
            Sponsor = new AddTournamentSponsorInput
            {
                SponsorId = sponsorId ?? ValidSponsorId,
                TitleSponsor = false,
                SponsorshipAmount = sponsorshipAmount ?? 100m
            }
        };

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        var result = _validator.Validate(CreateRequest());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired error when Id is null")]
    public void Validate_ShouldFail_WhenIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new AddTournamentSponsorRequest
        {
            Id = null,
            Sponsor = new AddTournamentSponsorInput
            {
                SponsorId = ValidSponsorId,
                TitleSponsor = false,
                SponsorshipAmount = 100m
            }
        });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Id"
            && e.ErrorCode == "AddTournamentSponsorRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength error when Id is not 26 characters")]
    public void Validate_ShouldFail_WhenIdIsNotCorrectLength()
    {
        var result = _validator.Validate(CreateRequest(id: "SHORT"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Id"
            && e.ErrorCode == "AddTournamentSponsorRequest.IdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdRequired error when SponsorId is null")]
    public void Validate_ShouldFail_WhenSponsorIdIsNull()
    {
#nullable disable
        var result = _validator.Validate(new AddTournamentSponsorRequest
        {
            Id = ValidId,
            Sponsor = new AddTournamentSponsorInput
            {
                SponsorId = null,
                TitleSponsor = false,
                SponsorshipAmount = 100m
            }
        });
#nullable enable

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor.SponsorId"
            && e.ErrorCode == "AddTournamentSponsorRequest.SponsorIdRequired");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdInvalidLength error when SponsorId is not 26 characters")]
    public void Validate_ShouldFail_WhenSponsorIdIsNotCorrectLength()
    {
        var result = _validator.Validate(CreateRequest(sponsorId: "SHORT"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor.SponsorId"
            && e.ErrorCode == "AddTournamentSponsorRequest.SponsorIdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with SponsorshipAmountInvalid error when SponsorshipAmount is negative")]
    public void Validate_ShouldFail_WhenSponsorshipAmountIsNegative()
    {
        var result = _validator.Validate(CreateRequest(sponsorshipAmount: -1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor.SponsorshipAmount"
            && e.ErrorCode == "AddTournamentSponsorRequest.SponsorshipAmountInvalid");
    }

    [Fact(DisplayName = "Validate should succeed when SponsorshipAmount is zero")]
    public void Validate_ShouldSucceed_WhenSponsorshipAmountIsZero()
    {
        var result = _validator.Validate(CreateRequest(sponsorshipAmount: 0m));

        result.IsValid.ShouldBeTrue();
    }
}