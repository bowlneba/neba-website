using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentDomainEventTests
{
    private static readonly DateTimeOffset FirstSquadTime = SquadFactory.ValidBowlingDateTime;

    private static Tournament CreateTournamentWithOneSquad()
    {
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(FirstSquadTime, maxEntries: 16, legacyId: null).IsError.ShouldBeFalse();
        tournament.ClearDomainEvents();

        return tournament;
    }

    // ── MarkResultsRecorded ───────────────────────────────────────────────────

    [Theory(DisplayName = "MarkResultsRecorded raises one TournamentResultsRecorded carrying the tournament id when the tournament is Completed or Truncated")]
    [InlineData("Completed")]
    [InlineData("Truncated")]
    public void MarkResultsRecorded_ShouldRaiseOneEvent_WhenTournamentIsFinalized(string statusName)
    {
        // Arrange
        var tournament = TournamentFactory.Create(status: TournamentStatus.FromName(statusName));

        // Act
        var result = tournament.MarkResultsRecorded();

        // Assert
        result.IsError.ShouldBeFalse();
        var domainEvent = tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentResultsRecorded>();
        domainEvent.TournamentId.ShouldBe(tournament.Id);
    }

    [Theory(DisplayName = "MarkResultsRecorded returns Tournament.NotFinalized and raises nothing when the tournament is Scheduled or Cancelled")]
    [InlineData("Scheduled")]
    [InlineData("Cancelled")]
    public void MarkResultsRecorded_ShouldReturnNotFinalizedAndRaiseNothing_WhenTournamentIsNotFinalized(string statusName)
    {
        // Arrange
        var tournament = TournamentFactory.Create(status: TournamentStatus.FromName(statusName));
        tournament.ClearDomainEvents();

        // Act
        var result = tournament.MarkResultsRecorded();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFinalized");
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "MarkResultsRecorded raises one event when called twice before the save")]
    public void MarkResultsRecorded_ShouldRaiseOneEvent_WhenCalledTwiceBeforeSave()
    {
        // Arrange
        var tournament = TournamentFactory.Create(status: TournamentStatus.Completed);
        tournament.MarkResultsRecorded();

        // Act
        var result = tournament.MarkResultsRecorded();

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentResultsRecorded>();
    }

    [Fact(DisplayName = "MarkResultsRecorded raises an event again after the events are cleared so a re-sync redelivers")]
    public void MarkResultsRecorded_ShouldRaiseEventAgain_WhenCalledAfterEventsCleared()
    {
        // Arrange
        var tournament = TournamentFactory.Create(status: TournamentStatus.Completed);
        tournament.MarkResultsRecorded();
        tournament.ClearDomainEvents();

        // Act
        var result = tournament.MarkResultsRecorded();

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentResultsRecorded>();
    }

    [Fact(DisplayName = "CompleteTournament and TruncateTournament raise no domain events")]
    public void StatusChanges_ShouldRaiseNoEvents()
    {
        // Arrange
        var completed = TournamentFactory.Create();
        var truncated = TournamentFactory.Create();

        // Act
        completed.CompleteTournament(entryCount: 100);
        truncated.TruncateTournament();

        // Assert
        completed.DomainEvents.ShouldBeEmpty();
        truncated.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "CancelTournament raises one TournamentChanged carrying the tournament id")]
    public void CancelTournament_ShouldRaiseTournamentChanged()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.CancelTournament();

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentChanged>().TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "CancelTournament raises nothing when the tournament is already finalized")]
    public void CancelTournament_ShouldRaiseNothing_WhenAlreadyFinalized()
    {
        // Arrange
        var tournament = TournamentFactory.Create(status: TournamentStatus.Completed);

        // Act
        var result = tournament.CancelTournament();

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Update raises one TournamentChanged when the start date, end date or type changes")]
    [InlineData("start")]
    [InlineData("end")]
    [InlineData("type")]
    [InlineData("all")]
    public void Update_ShouldRaiseTournamentChanged_WhenScheduleOrTypeChanges(string change)
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var changesStart = change is "start" or "all";
        var changesEnd = change is "end" or "all";
        var changesType = change is "type" or "all";
        var otherType = TournamentType.List.First(t => t != tournament.TournamentType && t != TournamentType.NonChampions);

        // Act
        var result = UpdateWith(
            tournament,
            changesStart ? tournament.StartDate.AddDays(-1) : tournament.StartDate,
            changesEnd ? tournament.EndDate.AddDays(1) : tournament.EndDate,
            changesType ? otherType : tournament.TournamentType);

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentChanged>().TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "Update raises nothing when the start date, end date and type stay the same")]
    public void Update_ShouldRaiseNothing_WhenScheduleAndTypeAreUnchanged()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = UpdateWith(tournament, tournament.StartDate, tournament.EndDate, tournament.TournamentType);

        // Assert
        result.IsError.ShouldBeFalse();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    private static ErrorOr.ErrorOr<ErrorOr.Updated> UpdateWith(
        Tournament tournament, DateOnly startDate, DateOnly endDate, TournamentType tournamentType)
        => tournament.Update(
            tournament.Name,
            tournamentType,
            startDate,
            endDate,
            tournament.SeasonId,
            tournament.StatsEligible,
            tournament.EntryFee,
            tournament.NebaAddedMoney,
            tournament.BowlingCenterId,
            tournament.ExternalRegistrationUrl,
            tournament.Logo,
            tournament.PatternLengthCategory,
            tournament.PatternRatioCategory,
            tournament.OilPatternRevealDateTime,
            previousTournament: null);

    // ── Squad events: success ─────────────────────────────────────────────────

    [Fact(DisplayName = "AddSquad raises one TournamentSquadsChanged carrying the tournament id")]
    public void AddSquad_ShouldRaiseOneSquadsChanged_WhenSuccessful()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.AddSquad(FirstSquadTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeFalse();
        var domainEvent = tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentSquadsChanged>();
        domainEvent.TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "UpdateSquad raises one TournamentSquadsChanged carrying the tournament id")]
    public void UpdateSquad_ShouldRaiseOneSquadsChanged_WhenSuccessful()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.UpdateSquad(squad.Id, FirstSquadTime.AddHours(1), maxEntries: 32);

        // Assert
        result.IsError.ShouldBeFalse();
        var domainEvent = tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentSquadsChanged>();
        domainEvent.TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "RemoveSquad raises one TournamentSquadsChanged carrying the tournament id")]
    public void RemoveSquad_ShouldRaiseOneSquadsChanged_WhenSuccessful()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.RemoveSquad(squad.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        var domainEvent = tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentSquadsChanged>();
        domainEvent.TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "A batch of squad changes raises exactly one TournamentSquadsChanged")]
    public void SquadChanges_ShouldRaiseExactlyOneEvent_WhenBatched()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        tournament.AddSquad(FirstSquadTime, maxEntries: null, legacyId: null);
        tournament.AddSquad(FirstSquadTime.AddHours(1), maxEntries: null, legacyId: null);
        tournament.AddSquad(FirstSquadTime.AddHours(2), maxEntries: null, legacyId: null);
        tournament.UpdateSquad(tournament.Squads.First().Id, FirstSquadTime.AddHours(3), maxEntries: 8);
        tournament.RemoveSquad(tournament.Squads.Last().Id);

        // Assert
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentSquadsChanged>();
    }

    [Fact(DisplayName = "ClearDomainEvents resets the squad event dedupe so a later change raises a new event")]
    public void ClearDomainEvents_ShouldLetLaterSquadChangeRaiseNewEvent()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        tournament.AddSquad(FirstSquadTime, maxEntries: null, legacyId: null);
        tournament.ClearDomainEvents();

        // Act
        tournament.AddSquad(FirstSquadTime.AddHours(1), maxEntries: null, legacyId: null);

        // Assert
        tournament.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TournamentSquadsChanged>();
    }

    // ── Squad events: failures raise nothing ──────────────────────────────────

    [Fact(DisplayName = "AddSquad raises nothing when the date is out of range")]
    public void AddSquad_ShouldRaiseNothing_WhenDateIsOutOfRange()
    {
        // Arrange
        var tournament = TournamentFactory.Create();
        var outOfRange = TournamentFactory.ValidEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.AddSquad(outOfRange, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddSquad raises nothing when another squad already bowls at that time")]
    public void AddSquad_ShouldRaiseNothing_WhenDateTimeAlreadyUsed()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();

        // Act
        var result = tournament.AddSquad(FirstSquadTime, maxEntries: null, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddSquad raises nothing when max entries is not positive")]
    public void AddSquad_ShouldRaiseNothing_WhenMaxEntriesIsNotPositive()
    {
        // Arrange
        var tournament = TournamentFactory.Create();

        // Act
        var result = tournament.AddSquad(FirstSquadTime, maxEntries: 0, legacyId: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "UpdateSquad raises nothing when the squad is not found")]
    public void UpdateSquad_ShouldRaiseNothing_WhenSquadNotFound()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();

        // Act
        var result = tournament.UpdateSquad(SquadId.New(), FirstSquadTime.AddHours(1), maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "UpdateSquad raises nothing when the new date is out of range")]
    public void UpdateSquad_ShouldRaiseNothing_WhenDateIsOutOfRange()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();
        var squad = tournament.Squads.Single();
        var outOfRange = TournamentFactory.ValidEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Act
        var result = tournament.UpdateSquad(squad.Id, outOfRange, maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "UpdateSquad raises nothing when the new time collides with another squad")]
    public void UpdateSquad_ShouldRaiseNothing_WhenDateTimeCollidesWithAnotherSquad()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();
        tournament.AddSquad(FirstSquadTime.AddHours(2), maxEntries: null, legacyId: null);
        tournament.ClearDomainEvents();
        var secondSquad = tournament.Squads.Single(s => s.BowlingDateTimeUtc == FirstSquadTime.AddHours(2));

        // Act
        var result = tournament.UpdateSquad(secondSquad.Id, FirstSquadTime, maxEntries: null);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Theory(DisplayName = "UpdateSquad raises nothing when max entries is zero or negative")]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateSquad_ShouldRaiseNothing_WhenMaxEntriesIsNotPositive(int maxEntries)
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();
        var squad = tournament.Squads.Single();

        // Act
        var result = tournament.UpdateSquad(squad.Id, FirstSquadTime, maxEntries);

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "RemoveSquad raises nothing when the squad is not found")]
    public void RemoveSquad_ShouldRaiseNothing_WhenSquadNotFound()
    {
        // Arrange
        var tournament = CreateTournamentWithOneSquad();

        // Act
        var result = tournament.RemoveSquad(SquadId.New());

        // Assert
        result.IsError.ShouldBeTrue();
        tournament.DomainEvents.ShouldBeEmpty();
    }
}