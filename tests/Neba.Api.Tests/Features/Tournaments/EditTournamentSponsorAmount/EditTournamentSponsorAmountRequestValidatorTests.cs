using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
using Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentSponsorAmount;

[UnitTest]
[Component("Tournaments")]
public sealed class EditTournamentSponsorAmountRequestValidatorTests
{
    private const string ValidId = "01000000000000000000000001";
    private const string ValidSponsorId = "01000000000000000000000002";

    private readonly EditTournamentSponsorAmountRequestValidator _validator = new();

    private static EditTournamentSponsorAmountRequest CreateRequest(
        string? id = null,
        string? sponsorId = null,
        decimal? sponsorshipAmount = null)
        => new()
        {
            Id = id ?? ValidId,
            SponsorId = sponsorId ?? ValidSponsorId,
            Sponsor = new EditTournamentSponsorAmountInput
            {
                SponsorshipAmount = sponsorshipAmount ?? 100m
            }
        };

    [Fact(DisplayName = "Validate should succeed when the request is valid")]
    public void Validate_ShouldSucceed_WhenRequestIsValid()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with IdRequired error when Id is null")]
    public void Validate_ShouldFail_WhenIdIsNull()
    {
#nullable disable
        // Arrange
        var request = new EditTournamentSponsorAmountRequest
        {
            Id = null,
            SponsorId = ValidSponsorId,
            Sponsor = new EditTournamentSponsorAmountInput
            {
                SponsorshipAmount = 100m
            }
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Id"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength error when Id is not 26 characters")]
    public void Validate_ShouldFail_WhenIdIsNotCorrectLength()
    {
        // Arrange
        var request = CreateRequest(id: "SHORT");

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Id"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.IdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdRequired error when SponsorId is null")]
    public void Validate_ShouldFail_WhenSponsorIdIsNull()
    {
#nullable disable
        // Arrange
        var request = new EditTournamentSponsorAmountRequest
        {
            Id = ValidId,
            SponsorId = null,
            Sponsor = new EditTournamentSponsorAmountInput
            {
                SponsorshipAmount = 100m
            }
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "SponsorId"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.SponsorIdRequired");
    }

    [Fact(DisplayName = "Validate should fail with SponsorIdInvalidLength error when SponsorId is not 26 characters")]
    public void Validate_ShouldFail_WhenSponsorIdIsNotCorrectLength()
    {
        // Arrange
        var request = CreateRequest(sponsorId: "SHORT");

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "SponsorId"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.SponsorIdInvalidLength");
    }

    [Fact(DisplayName = "Validate should fail with SponsorshipAmountInvalid error when SponsorshipAmount is negative")]
    public void Validate_ShouldFail_WhenSponsorshipAmountIsNegative()
    {
        // Arrange
        var request = CreateRequest(sponsorshipAmount: -1m);

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor.SponsorshipAmount"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.SponsorshipAmountInvalid");
    }

    [Fact(DisplayName = "Validate should succeed when SponsorshipAmount is zero")]
    public void Validate_ShouldSucceed_WhenSponsorshipAmountIsZero()
    {
        // Arrange
        var request = CreateRequest(sponsorshipAmount: 0m);

        // Act
        var result = _validator.Validate(request);

        // Assert

        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with SponsorshipAmountInvalid error when SponsorshipAmount exceeds the maximum or has more than two decimals")]
    [InlineData(100_000)]
    [InlineData(250_000)]
    [InlineData(10.123)]
    public void Validate_ShouldFail_WhenSponsorshipAmountIsOutOfRange(double amount)
    {
        // Arrange
        var request = CreateRequest(sponsorshipAmount: (decimal)amount);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor.SponsorshipAmount"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.SponsorshipAmountInvalid");
    }

    [Fact(DisplayName = "Validate should succeed when SponsorshipAmount is the maximum")]
    public void Validate_ShouldSucceed_WhenSponsorshipAmountIsMaximum()
    {
        // Arrange
        var request = CreateRequest(sponsorshipAmount: 99_999.99m);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with SponsorRequired error when Sponsor is null")]
    public void Validate_ShouldFail_WhenSponsorIsNull()
    {
#nullable disable
        // Arrange
        var request = new EditTournamentSponsorAmountRequest
        {
            Id = ValidId,
            SponsorId = ValidSponsorId,
            Sponsor = null
        };
#nullable enable

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == "Sponsor"
            && e.ErrorCode == "EditTournamentSponsorAmountRequest.SponsorRequired");
    }
}
