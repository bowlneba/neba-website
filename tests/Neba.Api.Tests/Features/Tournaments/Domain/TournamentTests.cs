using ErrorOr;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentTests
{
    [Fact(DisplayName = "Create returns success when all inputs are valid")]
    public void Create_ShouldReturnSuccess_WhenInputsAreValid()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            seasonId,
            statsEligible: true,
            entryFee: 100m);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create assigns a new Id and the provided required properties")]
    public void Create_ShouldAssignIdAndRequiredProperties_WhenInputsAreValid()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            seasonId,
            statsEligible: true,
            entryFee: 100m);

        // Assert
        var tournament = result.Value;
        tournament.Id.ShouldNotBe(default);
        tournament.Name.ShouldBe(TournamentFactory.ValidName);
        tournament.TournamentType.ShouldBe(TournamentFactory.ValidTournamentType);
        tournament.StartDate.ShouldBe(TournamentFactory.ValidStartDate);
        tournament.EndDate.ShouldBe(TournamentFactory.ValidEndDate);
        tournament.SeasonId.ShouldBe(seasonId);
        tournament.StatsEligible.ShouldBeTrue();
        tournament.EntryFee.ShouldBe(100m);
    }

    [Fact(DisplayName = "Create assigns the optional properties when provided")]
    public void Create_ShouldAssignOptionalProperties_WhenProvided()
    {
        // Arrange
        var bowlingCenterId = CertificationNumberFactory.Create();
        var externalRegistrationUrl = new Uri("https://example.com/register");

        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            bowlingCenterId: bowlingCenterId,
            externalRegistrationUrl: externalRegistrationUrl,
            patternLengthCategory: PatternLengthCategory.LongPattern,
            patternRatioCategory: PatternRatioCategory.Recreation);

        // Assert
        var tournament = result.Value;
        tournament.BowlingCenterId.ShouldBe(bowlingCenterId);
        tournament.ExternalRegistrationUrl.ShouldBe(externalRegistrationUrl);
        tournament.PatternLengthCategory.ShouldBe(PatternLengthCategory.LongPattern);
        tournament.PatternRatioCategory.ShouldBe(PatternRatioCategory.Recreation);
    }

#nullable disable
    [Theory(DisplayName = "Create returns Tournament.Name.Required when name is null, empty, or whitespace")]
    [InlineData(null, TestDisplayName = "name is null")]
    [InlineData("", TestDisplayName = "name is empty")]
    [InlineData("   ", TestDisplayName = "name is whitespace")]
    public void Create_ShouldReturnError_WhenNameIsNullOrWhiteSpace(string name)
    {
        // Act
        var result = Tournament.Create(
            name,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Name.Required");
    }
#nullable enable

    [Fact(DisplayName = "Create returns Tournament.EndDateBeforeStartDate when end date is before start date")]
    public void Create_ShouldReturnError_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = new DateOnly(2025, 10, 5);
        var endDate = new DateOnly(2025, 10, 4);

        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            startDate,
            endDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.EndDateBeforeStartDate");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata["StartDate"].ShouldBe("2025-10-05");
        result.FirstError.Metadata["EndDate"].ShouldBe("2025-10-04");
    }

    [Fact(DisplayName = "Create returns success when start date equals end date")]
    public void Create_ShouldReturnSuccess_WhenStartDateEqualsEndDate()
    {
        // Arrange
        var date = TournamentFactory.ValidStartDate;

        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            date,
            date,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create returns Tournament.InvalidEntryFee when entry fee is negative")]
    public void Create_ShouldReturnError_WhenEntryFeeIsNegative()
    {
        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: -1m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidEntryFee");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata["EntryFee"].ShouldBe(-1m);
    }

    [Fact(DisplayName = "Create returns success when entry fee is zero")]
    public void Create_ShouldReturnSuccess_WhenEntryFeeIsZero()
    {
        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 0m);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create returns Tournament.InvalidNebaAddedMoney when NEBA added money is negative")]
    public void Create_ShouldReturnError_WhenNebaAddedMoneyIsNegative()
    {
        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: -1m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidNebaAddedMoney");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata["NebaAddedMoney"].ShouldBe(-1m);
    }

    [Fact(DisplayName = "Create assigns NEBA added money when provided")]
    public void Create_ShouldAssignNebaAddedMoney_WhenProvided()
    {
        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: 500m);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.NebaAddedMoney.ShouldBe(500m);
    }

    [Fact(DisplayName = "Create defaults NEBA added money to zero when not provided")]
    public void Create_ShouldDefaultNebaAddedMoneyToZero_WhenNotProvided()
    {
        // Act
        var result = Tournament.Create(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.NebaAddedMoney.ShouldBe(0m);
    }

    [Fact(DisplayName = "Update returns success and reassigns fields when all inputs are valid")]
    public void Update_ShouldReturnSuccessAndReassignFields_WhenInputsAreValid()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var newSeasonId = SeasonId.New();
        var bowlingCenterId = CertificationNumberFactory.Create();
        var externalRegistrationUrl = new Uri("https://example.com/register");
        var newStartDate = new DateOnly(2026, 3, 1);
        var newEndDate = new DateOnly(2026, 3, 2);

        // Act
        var result = tournament.Update(
            name: "Updated Tournament Name",
            tournamentType: TournamentType.Doubles,
            startDate: newStartDate,
            endDate: newEndDate,
            seasonId: newSeasonId,
            statsEligible: false,
            entryFee: 250m,
            nebaAddedMoney: 1000m,
            bowlingCenterId: bowlingCenterId,
            externalRegistrationUrl: externalRegistrationUrl,
            logo: null,
            patternLengthCategory: PatternLengthCategory.LongPattern,
            patternRatioCategory: PatternRatioCategory.Sport,
            oilPatternRevealDateTime: null);

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.Name.ShouldBe("Updated Tournament Name");
        tournament.TournamentType.ShouldBe(TournamentType.Doubles);
        tournament.StartDate.ShouldBe(newStartDate);
        tournament.EndDate.ShouldBe(newEndDate);
        tournament.SeasonId.ShouldBe(newSeasonId);
        tournament.StatsEligible.ShouldBeFalse();
        tournament.EntryFee.ShouldBe(250m);
        tournament.NebaAddedMoney.ShouldBe(1000m);
        tournament.BowlingCenterId.ShouldBe(bowlingCenterId);
        tournament.ExternalRegistrationUrl.ShouldBe(externalRegistrationUrl);
        tournament.PatternLengthCategory.ShouldBe(PatternLengthCategory.LongPattern);
        tournament.PatternRatioCategory.ShouldBe(PatternRatioCategory.Sport);
    }

#nullable disable
    [Theory(DisplayName = "Update returns Tournament.Name.Required when name is null, empty, or whitespace")]
    [InlineData(null, TestDisplayName = "name is null")]
    [InlineData("", TestDisplayName = "name is empty")]
    [InlineData("   ", TestDisplayName = "name is whitespace")]
    public void Update_ShouldReturnError_WhenNameIsNullOrWhiteSpace(string name)
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.Update(
            name,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: 0m,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Name.Required");
    }
#nullable enable

    [Fact(DisplayName = "Update returns Tournament.EndDateBeforeStartDate when end date is before start date")]
    public void Update_ShouldReturnError_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var startDate = new DateOnly(2025, 10, 5);
        var endDate = new DateOnly(2025, 10, 4);

        // Act
        var result = tournament.Update(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            startDate,
            endDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: 0m,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.EndDateBeforeStartDate");
    }

    [Fact(DisplayName = "Update returns Tournament.InvalidEntryFee when entry fee is negative")]
    public void Update_ShouldReturnError_WhenEntryFeeIsNegative()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.Update(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: -1m,
            nebaAddedMoney: 0m,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidEntryFee");
    }

    [Fact(DisplayName = "Update returns Tournament.InvalidNebaAddedMoney when NEBA added money is negative")]
    public void Update_ShouldReturnError_WhenNebaAddedMoneyIsNegative()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.Update(
            TournamentFactory.ValidName,
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: -1m,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidNebaAddedMoney");
    }

    [Fact(DisplayName = "Update does not modify the sponsors collection")]
    public void Update_ShouldNotModifySponsorsCollection()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Act
        tournament.Update(
            "Updated Name",
            TournamentFactory.ValidTournamentType,
            TournamentFactory.ValidStartDate,
            TournamentFactory.ValidEndDate,
            tournament.SeasonId,
            statsEligible: true,
            entryFee: 100m,
            nebaAddedMoney: 0m,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null);

        // Assert
        tournament.Sponsors.ShouldContain(s => s.SponsorId == sponsorId);
    }

    [Fact(DisplayName = "AddSponsor returns success when sponsor is new")]
    public void AddSponsor_ShouldReturnSuccess_WhenSponsorIsNew()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();

        // Act
        var result = tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddSponsor adds sponsor to the collection when sponsor is new")]
    public void AddSponsor_ShouldAddSponsorToCollection_WhenSponsorIsNew()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();

        // Act
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Assert
        tournament.Sponsors.ShouldContain(s => s.SponsorId == sponsorId);
    }

    [Fact(DisplayName = "AddSponsor persists title sponsor flag and amount on the added sponsor")]
    public void AddSponsor_ShouldPersistSponsorDetails_WhenSponsorIsAdded()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();

        // Act
        tournament.AddSponsor(sponsorId, titleSponsor: true, sponsorshipAmount: 1000m);

        // Assert
        var added = tournament.Sponsors.Single(s => s.SponsorId == sponsorId);
        added.TitleSponsor.ShouldBeTrue();
        added.SponsorshipAmount.ShouldBe(1000m);
    }

    [Fact(DisplayName = "AddSponsor returns Tournament.SponsorAlreadyAdded when sponsor is already in the tournament")]
    public void AddSponsor_ShouldReturnError_WhenSponsorIsAlreadyAdded()
    {
        // Arrange
        var sponsorId = SponsorId.New();
        var tournament = TournamentFactory.Create();
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 250m);

        // Act
        var result = tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 250m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorAlreadyAdded");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("SponsorId");
        result.FirstError.Metadata["SponsorId"].ShouldBe(sponsorId.ToString());
    }

    [Fact(DisplayName = "AddSponsor returns Tournament.TitleSponsorAlreadyAdded with existing title sponsor ID when a second title sponsor is added")]
    public void AddSponsor_ShouldReturnError_WhenTitleSponsorAlreadyExists()
    {
        // Arrange
        var existingTitleSponsorId = SponsorId.New();
        var tournament = TournamentFactory.Create();
        tournament.AddSponsor(existingTitleSponsorId, titleSponsor: true, sponsorshipAmount: 2000m);

        var newSponsorId = SponsorId.New();

        // Act
        var result = tournament.AddSponsor(newSponsorId, titleSponsor: true, sponsorshipAmount: 1500m);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.TitleSponsorAlreadyAdded");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("TitleSponsorId");
        result.FirstError.Metadata["TitleSponsorId"].ShouldBe(existingTitleSponsorId.ToString());
    }

    [Fact(DisplayName = "AddSponsor returns success when non-title sponsor is added alongside an existing title sponsor")]
    public void AddSponsor_ShouldReturnSuccess_WhenNonTitleSponsorAddedWithExistingTitleSponsor()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSponsor(SponsorId.New(), titleSponsor: true, sponsorshipAmount: 2000m);
        var regularSponsorId = SponsorId.New();

        // Act
        var result = tournament.AddSponsor(regularSponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.Sponsors.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "AddSponsor allows multiple non-title sponsors")]
    public void AddSponsor_ShouldAllowMultipleNonTitleSponsors()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        tournament.AddSponsor(SponsorId.New(), titleSponsor: false, sponsorshipAmount: 100m);
        tournament.AddSponsor(SponsorId.New(), titleSponsor: false, sponsorshipAmount: 200m);
        var result = tournament.AddSponsor(SponsorId.New(), titleSponsor: false, sponsorshipAmount: 300m);

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.Sponsors.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "AddSponsor returns TournamentSponsor.NegativeSponsorshipAmount when sponsorship amount is negative")]
    public void AddSponsor_ShouldReturnError_WhenSponsorshipAmountIsNegative()
    {
        var tournament = TournamentFactory.Create();

        var result = tournament.AddSponsor(SponsorId.New(), titleSponsor: false, sponsorshipAmount: -1m);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentSponsor.NegativeSponsorshipAmount");
    }

    [Fact(DisplayName = "RemoveSponsor returns Deleted when sponsor is attached")]
    public void RemoveSponsor_ShouldReturnDeleted_WhenSponsorIsAttached()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Act
        var result = tournament.RemoveSponsor(sponsorId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Deleted);
    }

    [Fact(DisplayName = "RemoveSponsor removes sponsor from the collection when sponsor is attached")]
    public void RemoveSponsor_ShouldRemoveSponsorFromCollection_WhenSponsorIsAttached()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);

        // Act
        tournament.RemoveSponsor(sponsorId);

        // Assert
        tournament.Sponsors.ShouldNotContain(s => s.SponsorId == sponsorId);
    }

    [Fact(DisplayName = "RemoveSponsor only removes the specified sponsor, leaving others intact")]
    public void RemoveSponsor_ShouldOnlyRemoveSpecifiedSponsor_WhenMultipleSponsorsAttached()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorIdToRemove = SponsorId.New();
        var sponsorIdToKeep = SponsorId.New();
        tournament.AddSponsor(sponsorIdToRemove, titleSponsor: false, sponsorshipAmount: 500m);
        tournament.AddSponsor(sponsorIdToKeep, titleSponsor: false, sponsorshipAmount: 750m);

        // Act
        tournament.RemoveSponsor(sponsorIdToRemove);

        // Assert
        tournament.Sponsors.Count.ShouldBe(1);
        tournament.Sponsors.ShouldContain(s => s.SponsorId == sponsorIdToKeep);
    }

    [Fact(DisplayName = "RemoveSponsor returns Tournament.SponsorNotAttached when sponsor was never added")]
    public void RemoveSponsor_ShouldReturnError_WhenSponsorWasNeverAdded()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();

        // Act
        var result = tournament.RemoveSponsor(sponsorId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorNotAttached");
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("SponsorId");
        result.FirstError.Metadata["SponsorId"].ShouldBe(sponsorId.ToString());
    }

    [Fact(DisplayName = "RemoveSponsor returns Tournament.SponsorNotAttached when sponsor was already removed")]
    public void RemoveSponsor_ShouldReturnError_WhenSponsorWasAlreadyRemoved()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var sponsorId = SponsorId.New();
        tournament.AddSponsor(sponsorId, titleSponsor: false, sponsorshipAmount: 500m);
        tournament.RemoveSponsor(sponsorId);

        // Act
        var result = tournament.RemoveSponsor(sponsorId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorNotAttached");
    }

    [Fact(DisplayName = "AddOilPattern returns Success when oil pattern is new")]
    public void AddOilPattern_ShouldReturnSuccess_WhenOilPatternIsNew()
    {
        var tournament = TournamentFactory.Create();

        var result = tournament.AddOilPattern(OilPatternId.New(), TournamentRound.Qualifying);

        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddOilPattern adds oil pattern to collection when pattern is new")]
    public void AddOilPattern_ShouldAddToCollection_WhenOilPatternIsNew()
    {
        var tournament = TournamentFactory.Create();
        var oilPatternId = OilPatternId.New();

        tournament.AddOilPattern(oilPatternId, TournamentRound.Qualifying);

        tournament.OilPatterns.ShouldContain(op => op.OilPatternId == oilPatternId);
    }

    [Fact(DisplayName = "AddOilPattern adds specified rounds to the new oil pattern")]
    public void AddOilPattern_ShouldAddSpecifiedRounds_WhenOilPatternIsNew()
    {
        var tournament = TournamentFactory.Create();
        var oilPatternId = OilPatternId.New();

        tournament.AddOilPattern(oilPatternId, TournamentRound.Qualifying, TournamentRound.MatchPlay);

        var added = tournament.OilPatterns.Single(op => op.OilPatternId == oilPatternId);
        added.TournamentRounds.ShouldContain(TournamentRound.Qualifying);
        added.TournamentRounds.ShouldContain(TournamentRound.MatchPlay);
    }

    [Fact(DisplayName = "AddOilPattern returns Success and adds round to existing pattern without creating a duplicate")]
    public void AddOilPattern_ShouldAddRoundToExistingPattern_WhenPatternAlreadyExists()
    {
        var tournament = TournamentFactory.Create();
        var oilPatternId = OilPatternId.New();
        tournament.AddOilPattern(oilPatternId, TournamentRound.Qualifying);

        var result = tournament.AddOilPattern(oilPatternId, TournamentRound.MatchPlay);

        result.IsError.ShouldBeFalse();
        tournament.OilPatterns.Count.ShouldBe(1);
        tournament.OilPatterns.Single().TournamentRounds.ShouldContain(TournamentRound.MatchPlay);
    }

    [Fact(DisplayName = "AddOilPattern returns TournamentOilPattern.RoundAlreadyAssociated when round is already on the existing pattern")]
    public void AddOilPattern_ShouldReturnError_WhenRoundAlreadyAssociatedWithExistingPattern()
    {
        var tournament = TournamentFactory.Create();
        var oilPatternId = OilPatternId.New();
        tournament.AddOilPattern(oilPatternId, TournamentRound.Qualifying);

        var result = tournament.AddOilPattern(oilPatternId, TournamentRound.Qualifying);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentOilPattern.RoundAlreadyAssociated");
    }

    [Fact(DisplayName = "AddOilPattern returns TournamentOilPattern.NoRoundsSpecified when no rounds are provided")]
    public void AddOilPattern_ShouldReturnError_WhenNoRoundsSpecified()
    {
        var tournament = TournamentFactory.Create();

        var result = tournament.AddOilPattern(OilPatternId.New());

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentOilPattern.NoRoundsSpecified");
    }

    [Fact(DisplayName = "AddSquad returns success when the bowling date/time is within range and unused")]
    public void AddSquad_ShouldReturnSuccess_WhenDateTimeIsValid()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = TournamentFactory.ValidStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: 32, legacyId: 42);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddSquad adds the squad to the collection with the provided details")]
    public void AddSquad_ShouldAddSquadToCollection_WhenDateTimeIsValid()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = TournamentFactory.ValidStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        tournament.AddSquad(bowlingDateTime, maxEntries: 32, legacyId: 42);

        // Assert
        var squad = tournament.Squads.Single(s => s.BowlingDateTimeUtc == bowlingDateTime);
        squad.MaxEntries.ShouldBe(32);
        squad.LegacyId.ShouldBe(42);
    }

    [Fact(DisplayName = "AddSquad returns Tournament.Squad.DateOutOfRange when the date is before the tournament start date")]
    public void AddSquad_ShouldReturnError_WhenDateIsBeforeStartDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = TournamentFactory.ValidStartDate.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateOutOfRange");
    }

    [Fact(DisplayName = "AddSquad returns Tournament.Squad.DateOutOfRange when the date is after the tournament end date")]
    public void AddSquad_ShouldReturnError_WhenDateIsAfterEndDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = TournamentFactory.ValidEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateOutOfRange");
    }

    [Fact(DisplayName = "AddSquad returns success when the UTC date is within range even though the offset's local date is before the start date")]
    public void AddSquad_ShouldReturnSuccess_WhenUtcDateIsWithinRange_ButOffsetLocalDateIsBeforeStartDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        // Oct 3 22:00 -04:00 == Oct 4 02:00 UTC: local date (Oct 3) is before ValidStartDate (Oct 4),
        // but the UTC date (Oct 4) is on the start date.
        var bowlingDateTime = new DateTimeOffset(2025, 10, 3, 22, 0, 0, TimeSpan.FromHours(-4));

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddSquad returns Tournament.Squad.DateOutOfRange when the UTC date is after the end date even though the offset's local date is on the end date")]
    public void AddSquad_ShouldReturnError_WhenUtcDateIsAfterEndDate_ButOffsetLocalDateIsOnEndDate()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        // Oct 5 21:00 -04:00 == Oct 6 01:00 UTC: local date (Oct 5) is on ValidEndDate (Oct 5),
        // but the UTC date (Oct 6) is after the end date.
        var bowlingDateTime = new DateTimeOffset(2025, 10, 5, 21, 0, 0, TimeSpan.FromHours(-4));

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateOutOfRange");
    }

    [Fact(DisplayName = "AddSquad returns Tournament.Squad.DateTimeAlreadyUsed when another squad already bowls at that date/time")]
    public void AddSquad_ShouldReturnError_WhenDateTimeAlreadyUsed()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = SquadFactory.ValidBowlingDateTime;
        tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Act
        var result = tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateTimeAlreadyUsed");
    }

    [Fact(DisplayName = "AddSquad does not add a colliding squad to the collection")]
    public void AddSquad_ShouldNotAddSquadToCollection_WhenDateTimeAlreadyUsed()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var bowlingDateTime = SquadFactory.ValidBowlingDateTime;
        tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Act
        tournament.AddSquad(bowlingDateTime, maxEntries: null, legacyId: null);

        // Assert
        tournament.Squads.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddSquad returns Squad.MaxEntries.Invalid when max entries is not positive")]
    public void AddSquad_ShouldReturnError_WhenMaxEntriesIsNotPositive()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: 0, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Squad.MaxEntries.Invalid");
    }

    [Fact(DisplayName = "UpdateSquad returns Updated and reassigns fields when inputs are valid")]
    public void UpdateSquad_ShouldReturnUpdatedAndReassignFields_WhenInputsAreValid()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: 16, legacyId: null);
        var squad = tournament.Squads.Single();
        var newBowlingDateTime = TournamentFactory.ValidEndDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.UpdateSquad(squad.Id, newBowlingDateTime, maxEntries: 64);

        // Assert
        result.IsError.ShouldBeFalse();
        squad.BowlingDateTimeUtc.ShouldBe(newBowlingDateTime);
        squad.MaxEntries.ShouldBe(64);
    }

    [Fact(DisplayName = "UpdateSquad converts a non-UTC bowling date/time to UTC")]
    public void UpdateSquad_ShouldConvertBowlingDateTimeToUtc_WhenNewDateTimeHasNonZeroOffset()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        var squad = tournament.Squads.Single();
        var nonUtcBowlingDateTime = new DateTimeOffset(
            TournamentFactory.ValidStartDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(-4));

        // Act
        var result = tournament.UpdateSquad(squad.Id, nonUtcBowlingDateTime, maxEntries: null);

        // Assert
        result.IsError.ShouldBeFalse();
        squad.BowlingDateTimeUtc.Offset.ShouldBe(TimeSpan.Zero);
        squad.BowlingDateTimeUtc.ShouldBe(nonUtcBowlingDateTime.ToUniversalTime());
    }

    [Fact(DisplayName = "UpdateSquad returns Tournament.Squad.NotFound when the squad doesn't exist")]
    public void UpdateSquad_ShouldReturnError_WhenSquadNotFound()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.UpdateSquad(SquadId.New(), SquadFactory.ValidBowlingDateTime, maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.NotFound");
    }

    [Fact(DisplayName = "UpdateSquad returns Tournament.Squad.DateOutOfRange when the new date falls outside the tournament's range")]
    public void UpdateSquad_ShouldReturnError_WhenDateIsOutOfRange()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        var squad = tournament.Squads.Single();
        var outOfRangeDateTime = TournamentFactory.ValidEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.UpdateSquad(squad.Id, outOfRangeDateTime, maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateOutOfRange");
    }

    [Fact(DisplayName = "UpdateSquad returns Tournament.Squad.DateTimeAlreadyUsed when another squad already bowls at the new date/time")]
    public void UpdateSquad_ShouldReturnError_WhenDateTimeCollidesWithAnotherSquad()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var firstBowlingDateTime = SquadFactory.ValidBowlingDateTime;
        var secondBowlingDateTime = firstBowlingDateTime.AddHours(2);
        tournament.AddSquad(firstBowlingDateTime, maxEntries: null, legacyId: null);
        tournament.AddSquad(secondBowlingDateTime, maxEntries: null, legacyId: null);
        var squadToUpdate = tournament.Squads.Single(s => s.BowlingDateTimeUtc == secondBowlingDateTime);

        // Act
        var result = tournament.UpdateSquad(squadToUpdate.Id, firstBowlingDateTime, maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.DateTimeAlreadyUsed");
    }

    [Fact(DisplayName = "UpdateSquad returns Updated when the new date/time is unchanged from the squad's own current date/time")]
    public void UpdateSquad_ShouldReturnUpdated_WhenDateTimeIsUnchangedFromOwnSquad()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: 16, legacyId: null);
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.UpdateSquad(squad.Id, SquadFactory.ValidBowlingDateTime, maxEntries: 32);

        // Assert
        result.IsError.ShouldBeFalse();
        squad.MaxEntries.ShouldBe(32);
    }

    [Fact(DisplayName = "UpdateSquad returns Squad.MaxEntries.Invalid when max entries is not positive")]
    public void UpdateSquad_ShouldReturnError_WhenMaxEntriesIsNotPositive()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.UpdateSquad(squad.Id, SquadFactory.ValidBowlingDateTime, maxEntries: -1);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Squad.MaxEntries.Invalid");
    }

    [Fact(DisplayName = "RemoveSquad returns Deleted when the squad exists")]
    public void RemoveSquad_ShouldReturnDeleted_WhenSquadExists()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.RemoveSquad(squad.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(Result.Deleted);
    }

    [Fact(DisplayName = "RemoveSquad removes the squad from the collection when it exists")]
    public void RemoveSquad_ShouldRemoveSquadFromCollection_WhenSquadExists()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        var squad = tournament.Squads.Single();

        // Act
        tournament.RemoveSquad(squad.Id);

        // Assert
        tournament.Squads.ShouldNotContain(s => s.Id == squad.Id);
    }

    [Fact(DisplayName = "RemoveSquad only removes the specified squad, leaving others intact")]
    public void RemoveSquad_ShouldOnlyRemoveSpecifiedSquad_WhenMultipleSquadsExist()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null);
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime.AddHours(2), maxEntries: null, legacyId: null);
        var squadToRemove = tournament.Squads.First();
        var squadToKeep = tournament.Squads.Last();

        // Act
        tournament.RemoveSquad(squadToRemove.Id);

        // Assert
        tournament.Squads.Count.ShouldBe(1);
        tournament.Squads.ShouldContain(s => s.Id == squadToKeep.Id);
    }

    [Fact(DisplayName = "RemoveSquad returns Tournament.Squad.NotFound when the squad doesn't exist")]
    public void RemoveSquad_ShouldReturnError_WhenSquadNotFound()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.RemoveSquad(SquadId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Squad.NotFound");
    }
}