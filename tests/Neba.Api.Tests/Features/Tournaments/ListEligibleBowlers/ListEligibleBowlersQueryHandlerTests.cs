using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListEligibleBowlersQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string LowerBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";
    private const string HigherBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2F";

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2010, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private ListEligibleBowlersQueryHandler CreateHandler() => new(_dbContext);

    private static ListEligibleBowlersQuery Query(Tournament tournament) => new() { Id = tournament.Id };

    private async Task<Season> SeedSeasonAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: start, endDate: end);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    private async Task<Bowler> SeedBowlerAsync(
        CancellationToken ct,
        Name? name = null,
        string? usbcId = null,
        BowlerId? id = null)
    {
        var bowler = BowlerFactory.Create(id: id, name: name, usbcId: usbcId);
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        return bowler;
    }

    // The tournament under test: Scheduled and an Invitational unless the test says otherwise.
    private async Task<Tournament> SeedInvitationalAsync(
        CancellationToken ct,
        Season? season = null,
        TournamentStatus? status = null,
        TournamentType? type = null)
    {
        var target = season ?? _season;
        var tournament = TournamentFactory.Create(
            tournamentType: type ?? TournamentType.Invitational,
            startDate: target.StartDate.AddDays(300),
            endDate: target.StartDate.AddDays(301),
            seasonId: target.Id,
            status: status);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    // One finalized tournament in the season holding a result for each participant.
    private async Task<Tournament> SeedResultTournamentAsync(
        Season season,
        int index,
        IEnumerable<Bowler> participants,
        CancellationToken ct,
        TournamentType? type = null,
        TournamentStatus? status = null,
        bool? statsEligible = null,
        DateOnly? startDate = null)
    {
        var date = startDate ?? season.StartDate.AddDays(index);
        var tournament = TournamentFactory.Create(
            tournamentType: type ?? TournamentType.Singles,
            startDate: date,
            endDate: date,
            seasonId: season.Id,
            statsEligible: statsEligible,
            status: status ?? TournamentStatus.Completed,
            results: [.. participants.Select((bowler, place) =>
                TournamentResultFactory.Create(bowlerId: bowler.Id, place: place + 1))]);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    // Gives each bowler the stated number of results in the season.
    private async Task SeedParticipationAsync(
        Season season,
        CancellationToken ct,
        params (Bowler Bowler, int Tournaments)[] entries)
    {
        var tournamentCount = entries.Max(entry => entry.Tournaments);
        for (var index = 0; index < tournamentCount; index++)
        {
            var participants = entries.Where(entry => entry.Tournaments > index).Select(entry => entry.Bowler).ToArray();
            await SeedResultTournamentAsync(season, index, participants, ct);
        }
    }

    private static string[] Names(ErrorOr<EligibleBowlersDto> result)
        => [.. result.Value.Bowlers.Select(bowler => bowler.Name)];

    public static TheoryData<string> NonInvitationalTypeNames()
        => [.. TournamentType.List.Where(type => type != TournamentType.Invitational).Select(type => type.Name)];

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await CreateHandler().HandleAsync(new ListEligibleBowlersQuery { Id = TournamentId.New() }, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Theory(DisplayName = "HandleAsync includes a bowler only with at least the threshold number of results in the season")]
    [InlineData(Tournament.InvitationalEligibilityThreshold - 1, false, TestDisplayName = "One below the threshold is excluded")]
    [InlineData(Tournament.InvitationalEligibilityThreshold, true, TestDisplayName = "At the threshold is included")]
    [InlineData(Tournament.InvitationalEligibilityThreshold + 1, true, TestDisplayName = "Above the threshold is included")]
    public async Task HandleAsync_ShouldApplyThreshold_ToResultCount(int resultCount, bool expectedIncluded)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Dana", lastName: "Jones"));
        await SeedParticipationAsync(_season, ct, (bowler, resultCount));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Names(result).Contains("Dana Jones").ShouldBe(expectedIncluded);
    }

    [Fact(DisplayName = "HandleAsync lists a bowler with five results once")]
    public async Task HandleAsync_ShouldListBowlerOnce_WhenBowlerHasFiveResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 5));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync does not count results from another season")]
    public async Task HandleAsync_ShouldNotCountResults_FromAnotherSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(otherSeason, ct, (bowler, 5));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync does not add results across seasons: two here and two elsewhere stay excluded")]
    public async Task HandleAsync_ShouldNotCombineResults_AcrossSeasons()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 2));
        await SeedParticipationAsync(otherSeason, ct, (bowler, 2));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync matches the season by SeasonId, not by tournament date")]
    public async Task HandleAsync_ShouldMatchSeasonBySeasonId_NotByDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedSeasonAsync(new DateOnly(2005, 1, 1), new DateOnly(2009, 12, 31), ct);
        var invitational = await SeedInvitationalAsync(ct);
        var included = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "In", lastName: "Season"));
        var excluded = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Date", lastName: "Only"));

        // Three tournaments of this season, dated inside the other season's range: they still count.
        for (var index = 0; index < 3; index++)
        {
            await SeedResultTournamentAsync(_season, index, [included], ct,
                startDate: new DateOnly(2006, 1, 1).AddDays(index));
        }

        // Two tournaments of this season plus one of the other season dated inside this season's range: two count.
        await SeedResultTournamentAsync(_season, 10, [excluded], ct, startDate: new DateOnly(2006, 2, 1));
        await SeedResultTournamentAsync(_season, 11, [excluded], ct, startDate: new DateOnly(2006, 2, 2));
        await SeedResultTournamentAsync(otherSeason, 12, [excluded], ct, startDate: new DateOnly(2020, 1, 1));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        Names(result).ShouldBe(["In Season"]);
    }

    [Fact(DisplayName = "HandleAsync counts results from Truncated tournaments")]
    public async Task HandleAsync_ShouldCountTruncatedTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedResultTournamentAsync(_season, 0, [bowler], ct, status: TournamentStatus.Truncated);
        await SeedResultTournamentAsync(_season, 1, [bowler], ct, status: TournamentStatus.Truncated);
        await SeedResultTournamentAsync(_season, 2, [bowler], ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync counts results from tournaments that are not stats eligible")]
    public async Task HandleAsync_ShouldCountNonStatsEligibleTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        for (var index = 0; index < Tournament.InvitationalEligibilityThreshold; index++)
        {
            await SeedResultTournamentAsync(_season, index, [bowler], ct, statsEligible: false);
        }

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync counts a result from every tournament type")]
    public async Task HandleAsync_ShouldCountEveryTournamentType()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var everyType = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Every", lastName: "Type"));
        var index = 0;
        foreach (var type in TournamentType.List)
        {
            await SeedResultTournamentAsync(_season, index++, [everyType], ct, type: type);
        }

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Every Type"]);
    }

    [Theory(DisplayName = "HandleAsync returns an empty list when the Invitational is not Scheduled, even with qualifiers")]
    [InlineData("Completed", TestDisplayName = "Completed")]
    [InlineData("Truncated", TestDisplayName = "Truncated")]
    [InlineData("Cancelled", TestDisplayName = "Cancelled")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenInvitationalIsNotScheduled(string statusName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct, status: TournamentStatus.FromName(statusName));
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 4));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldBeNull();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Theory(DisplayName = "HandleAsync returns an empty list for a Scheduled tournament that is not an Invitational, even with qualifiers")]
    [MemberData(nameof(NonInvitationalTypeNames))]
    public async Task HandleAsync_ShouldReturnEmpty_WhenTournamentIsNotAnInvitational(string typeName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedInvitationalAsync(ct, type: TournamentType.FromName(typeName));
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 4));

        // Act
        var result = await CreateHandler().HandleAsync(Query(tournament), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldBeNull();
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns a null USBC ID for a bowler without one")]
    public async Task HandleAsync_ShouldReturnNullUsbcId_WhenBowlerHasNone()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var withId = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Ann", lastName: "Adams"), usbcId: "4821-09317");
        var withoutId = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bob", lastName: "Baker"));
        await SeedParticipationAsync(_season, ct, (withId, 3), (withoutId, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.Select(bowler => bowler.UsbcId).ShouldBe(["4821-09317", null]);
    }

    [Fact(DisplayName = "HandleAsync returns an empty, non-null list when nobody qualifies")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNobodyQualifies()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var bowler = await SeedBowlerAsync(ct);
        await SeedParticipationAsync(_season, ct, (bowler, 2));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Requirements.ShouldNotBeNull();
        result.Value.Requirements.MinTournamentsCount.ShouldBe(Tournament.InvitationalEligibilityThreshold);
        result.Value.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync sorts by last name then first name regardless of insert order")]
    public async Task HandleAsync_ShouldSortByLastNameThenFirstName_WhenInsertedOutOfOrder()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var zed = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Zed", lastName: "Young"));
        var carl = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Carl", lastName: "Adams"));
        var amy = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Amy", lastName: "Young"));
        var bea = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bea", lastName: "Adams"));
        await SeedParticipationAsync(_season, ct, (zed, 3), (carl, 3), (amy, 3), (bea, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Bea Adams", "Carl Adams", "Amy Young", "Zed Young"]);
    }

    [Fact(DisplayName = "HandleAsync sorts last names ignoring case and treating punctuation by ordinal value")]
    public async Task HandleAsync_ShouldSortOrdinalIgnoreCase_WithMixedCaseAndPunctuation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var obrien = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "Obrien"));
        var apostrophe = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "O'Brien"));
        var lower = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "mcDonald"));
        var upper = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Pat", lastName: "Nash"));
        await SeedParticipationAsync(_season, ct, (obrien, 3), (apostrophe, 3), (lower, 3), (upper, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert: ordinal ignore-case, so "mcdonald" < "nash" < "o'brien" < "obrien" (' sorts before 'b')
        Names(result).ShouldBe(["Pat mcDonald", "Pat Nash", "Pat O'Brien", "Pat Obrien"]);
    }

    [Fact(DisplayName = "HandleAsync sorts by legal first name while displaying the nickname")]
    public async Task HandleAsync_ShouldSortByFirstName_WhenBowlerHasNickname()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        var nicknamed = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Alan", lastName: "Young", nickname: "Zed"));
        var plain = await SeedBowlerAsync(ct, NameFactory.Create(firstName: "Bill", lastName: "Young"));
        await SeedParticipationAsync(_season, ct, (plain, 3), (nicknamed, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        Names(result).ShouldBe(["Zed Young", "Bill Young"]);
    }

    [Fact(DisplayName = "HandleAsync breaks a tie between identical names by bowler ID")]
    public async Task HandleAsync_ShouldSortByBowlerId_WhenNamesAreIdentical()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var invitational = await SeedInvitationalAsync(ct);
        // EF owns each Name by its bowler, so the two bowlers need separate instances.
        var higher = await SeedBowlerAsync(
            ct, NameFactory.Create(firstName: "Sam", lastName: "Lee"), usbcId: "2222-22222", id: new BowlerId(HigherBowlerId));
        var lower = await SeedBowlerAsync(
            ct, NameFactory.Create(firstName: "Sam", lastName: "Lee"), usbcId: "1111-11111", id: new BowlerId(LowerBowlerId));
        await SeedParticipationAsync(_season, ct, (higher, 3), (lower, 3));

        // Act
        var result = await CreateHandler().HandleAsync(Query(invitational), ct);

        // Assert
        result.Value.Bowlers.Select(bowler => bowler.UsbcId).ShouldBe(["1111-11111", "2222-22222"]);
    }
}
