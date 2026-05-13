using Neba.Domain.Sponsors;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentTests
{
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
}