using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.CreateTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.CreateTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class CreateTournamentRequestValidatorTests
{
    private readonly CreateTournamentRequestValidator _validator = new();

    [Fact(DisplayName = "Validate should succeed when all fields are valid")]
    public void Validate_ShouldSucceed_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create() };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Validate should fail with NameRequired when Name is empty")]
    public void Validate_ShouldFailWithNameRequired_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(name: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.NameRequired");
    }

    [Fact(DisplayName = "Validate should fail with NameTooLong when Name exceeds 127 characters")]
    public void Validate_ShouldFailWithNameTooLong_WhenNameExceeds127Characters()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(name: new string('a', 128)) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.NameTooLong");
    }

    [Fact(DisplayName = "Validate should fail with TournamentTypeRequired when TournamentType is empty")]
    public void Validate_ShouldFailWithTournamentTypeRequired_WhenTournamentTypeIsEmpty()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(tournamentType: string.Empty) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.TournamentTypeRequired");
    }

    [Fact(DisplayName = "Validate should fail with TournamentTypeInvalid when TournamentType is unknown")]
    public void Validate_ShouldFailWithTournamentTypeInvalid_WhenTournamentTypeIsUnknown()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(tournamentType: "NotARealType") };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.TournamentTypeInvalid");
    }

    [Fact(DisplayName = "Validate should fail with EndDateBeforeStartDate when EndDate precedes StartDate")]
    public void Validate_ShouldFailWithEndDateBeforeStartDate_WhenEndDatePrecedesStartDate()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(
                startDate: new DateOnly(2025, 10, 5),
                endDate: new DateOnly(2025, 10, 4))
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.EndDateBeforeStartDate");
    }

    [Fact(DisplayName = "Validate should fail with EntryFeeInvalid when EntryFee is negative")]
    public void Validate_ShouldFailWithEntryFeeInvalid_WhenEntryFeeIsNegative()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(entryFee: -1m) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.EntryFeeInvalid");
    }

    [Fact(DisplayName = "Validate should fail with NebaAddedMoneyInvalid when NebaAddedMoney is negative")]
    public void Validate_ShouldFailWithNebaAddedMoneyInvalid_WhenNebaAddedMoneyIsNegative()
    {
        // Arrange
        var request = new CreateTournamentRequest { Tournament = TournamentInputFactory.Create(nebaAddedMoney: -1m) };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.NebaAddedMoneyInvalid");
    }

    [Fact(DisplayName = "Validate should fail with ExternalRegistrationUrlInvalid when the URL is relative")]
    public void Validate_ShouldFailWithExternalRegistrationUrlInvalid_WhenUrlIsRelative()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(externalRegistrationUrl: new Uri("/register", UriKind.Relative))
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.ExternalRegistrationUrlInvalid");
    }

    [Fact(DisplayName = "Validate should succeed when ExternalRegistrationUrl is not supplied")]
    public void Validate_ShouldSucceed_WhenExternalRegistrationUrlIsNotSupplied()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(externalRegistrationUrl: null)
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should fail with PatternLengthCategoryInvalid when the category name is unknown")]
    public void Validate_ShouldFailWithPatternLengthCategoryInvalid_WhenCategoryNameIsUnknown()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(patternLengthCategory: "NotARealCategory")
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.PatternLengthCategoryInvalid");
    }

    [Fact(DisplayName = "Validate should fail with PatternRatioCategoryInvalid when the category name is unknown")]
    public void Validate_ShouldFailWithPatternRatioCategoryInvalid_WhenCategoryNameIsUnknown()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(patternRatioCategory: "NotARealCategory")
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.PatternRatioCategoryInvalid");
    }

    [Fact(DisplayName = "Validate should fail with OilPatternAndManualCategoriesConflict when both an oil pattern and manual categories are supplied")]
    public void Validate_ShouldFailWithOilPatternAndManualCategoriesConflict_WhenBothSupplied()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(
                oilPatternId: "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
                patternLengthCategory: "Long")
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == "CreateTournamentRequest.OilPatternAndManualCategoriesConflict");
    }

    [Fact(DisplayName = "Validate should succeed when only an oil pattern is supplied")]
    public void Validate_ShouldSucceed_WhenOnlyOilPatternIsSupplied()
    {
        // Arrange
        var request = new CreateTournamentRequest
        {
            Tournament = TournamentInputFactory.Create(oilPatternId: "01J7ZK8X6ZQJ8V3F8N9T9C9R2E")
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}