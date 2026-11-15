using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentPreviousTournamentTests
{
    private static ErrorOr.ErrorOr<Tournament> CreateWith(
        PreviousTournamentFacts? previous,
        TournamentType? type = null,
        DateOnly? startDate = null)
        => Tournament.Create(
            TournamentFactory.ValidName,
            type ?? TournamentType.Doubles,
            startDate ?? new DateOnly(2026, 3, 7),
            (startDate ?? new DateOnly(2026, 3, 7)).AddDays(1),
            SeasonId.New(),
            statsEligible: true,
            entryFee: 100m,
            previousTournament: previous);

    [Fact(DisplayName = "Create stores the previous tournament's ID when the previous tournament is valid")]
    public void Create_ShouldStorePreviousTournamentId_WhenValid()
    {
        // Arrange
        var previous = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Doubles, endDate: new DateOnly(2025, 3, 2));

        // Act
        var result = CreateWith(previous);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PreviousTournamentId.ShouldBe(previous.Id);
    }

    [Fact(DisplayName = "Create leaves the previous tournament empty when none is given")]
    public void Create_ShouldLeavePreviousTournamentEmpty_WhenNoneGiven()
    {
        // Arrange / Act
        var result = CreateWith(previous: null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PreviousTournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns an error when the previous tournament is a different type")]
    public void Create_ShouldReturnTypeMismatch_WhenPreviousTypeDiffers()
    {
        // Arrange
        var previous = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Trios, endDate: new DateOnly(2025, 3, 2));

        // Act
        var result = CreateWith(previous, type: TournamentType.Doubles);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.PreviousTournament.TypeMismatch");
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
    }

    [Theory(DisplayName = "Create returns an error when the previous tournament does not end before this one starts")]
    [InlineData("2026-03-07", TestDisplayName = "Ends on the start date")]
    [InlineData("2026-03-08", TestDisplayName = "Ends after the start date")]
    public void Create_ShouldReturnNotBefore_WhenPreviousEndsOnOrAfterStart(string previousEnd)
    {
        // Arrange
        var previous = PreviousTournamentFactsFactory.Create(
            tournamentType: TournamentType.Doubles,
            endDate: DateOnly.Parse(previousEnd, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        var result = CreateWith(previous, type: TournamentType.Doubles, startDate: new DateOnly(2026, 3, 7));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.PreviousTournament.NotBefore");
    }

    [Fact(DisplayName = "Create accepts a previous tournament that ended the day before this one starts")]
    public void Create_ShouldSucceed_WhenPreviousEndedTheDayBefore()
    {
        // Arrange
        var previous = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Doubles, endDate: new DateOnly(2026, 3, 6));

        // Act
        var result = CreateWith(previous, type: TournamentType.Doubles, startDate: new DateOnly(2026, 3, 7));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create returns an error when a Non-Champions tournament names a previous tournament")]
    public void Create_ShouldReturnNotAllowed_WhenTournamentIsNonChampions()
    {
        // Arrange
        var previous = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.NonChampions, endDate: new DateOnly(2025, 3, 2));

        // Act
        var result = CreateWith(previous, type: TournamentType.NonChampions);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.PreviousTournament.NotAllowed");
    }

    [Fact(DisplayName = "Update returns an error when the tournament names itself as its previous tournament")]
    public void Update_ShouldReturnIsSelf_WhenPreviousIsTheSameTournament()
    {
        // Arrange
        var tournament = TournamentFactory.Create(tournamentType: TournamentType.Doubles, startDate: new DateOnly(2026, 3, 7), endDate: new DateOnly(2026, 3, 8));
        var self = PreviousTournamentFactsFactory.Create(id: tournament.Id, tournamentType: TournamentType.Doubles, endDate: new DateOnly(2025, 3, 2));

        // Act
        var result = UpdateWith(tournament, self);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.PreviousTournament.IsSelf");
    }

    [Fact(DisplayName = "Update sets and then clears the previous tournament")]
    public void Update_ShouldSetThenClearPreviousTournament()
    {
        // Arrange
        var tournament = TournamentFactory.Create(tournamentType: TournamentType.Doubles, startDate: new DateOnly(2026, 3, 7), endDate: new DateOnly(2026, 3, 8));
        var previous = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Doubles, endDate: new DateOnly(2025, 3, 2));

        // Act
        var set = UpdateWith(tournament, previous);
        var idAfterSet = tournament.PreviousTournamentId;
        var cleared = UpdateWith(tournament, null);

        // Assert
        set.IsError.ShouldBeFalse();
        idAfterSet.ShouldBe(previous.Id);
        cleared.IsError.ShouldBeFalse();
        tournament.PreviousTournamentId.ShouldBeNull();
    }

    [Fact(DisplayName = "Update keeps the old previous tournament when the new one is invalid")]
    public void Update_ShouldKeepOldPreviousTournament_WhenNewOneIsInvalid()
    {
        // Arrange
        var good = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Doubles, endDate: new DateOnly(2025, 3, 2));
        var tournament = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2026, 3, 7),
            endDate: new DateOnly(2026, 3, 8),
            previousTournament: good);
        var bad = PreviousTournamentFactsFactory.Create(tournamentType: TournamentType.Trios, endDate: new DateOnly(2025, 3, 2));

        // Act
        var result = UpdateWith(tournament, bad);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.PreviousTournamentId.ShouldBe(good.Id);
    }

    private static ErrorOr.ErrorOr<ErrorOr.Updated> UpdateWith(Tournament tournament, PreviousTournamentFacts? previous)
        => tournament.Update(
            name: tournament.Name,
            tournamentType: tournament.TournamentType,
            startDate: tournament.StartDate,
            endDate: tournament.EndDate,
            seasonId: tournament.SeasonId,
            statsEligible: tournament.StatsEligible,
            entryFee: tournament.EntryFee,
            nebaAddedMoney: tournament.NebaAddedMoney,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null,
            oilPatternRevealDateTime: null,
            previousTournament: previous);
}