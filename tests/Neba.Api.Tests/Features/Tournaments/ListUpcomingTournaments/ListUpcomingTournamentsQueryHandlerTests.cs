using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Features.Tournaments.ListUpcomingTournaments;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListUpcomingTournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListUpcomingTournamentsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private static ListUpcomingTournamentsQuery Query(bool authenticated = true, bool management = true)
        => new() { CallerIsAuthenticated = authenticated, CallerHasTournamentManagementPermission = management };

    private ListUpcomingTournamentsQueryHandler CreateHandler(
        Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>> seasonHandler)
        => new(_dbContext, seasonHandler.Object, new FakeTimeProvider(Now));

    private static Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>> StrictSeasonHandler()
        => new(MockBehavior.Strict);

    private async Task<Season> AddSeasonAsync(int year, CancellationToken ct)
    {
        var season = SeasonFactory.Create(
            description: $"{year} Season",
            startDate: new DateOnly(year, 1, 1),
            endDate: new DateOnly(year, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    [Fact(DisplayName = "HandleAsync returns an empty collection when every season has ended")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenEverySeasonHasEnded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await AddSeasonAsync(2025, ct);
        var seasonHandler = StrictSeasonHandler();
        var handler = CreateHandler(seasonHandler);

        // Act
        var result = await handler.HandleAsync(Query(), ct);

        // Assert
        result.ShouldBeEmpty();
        seasonHandler.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "HandleAsync returns tournaments from the current season and later seasons ordered by start date")]
    public async Task HandleAsync_ShouldReturnCurrentAndLaterSeasonTournaments_OrderedByStartDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var past = await AddSeasonAsync(2025, ct);
        var current = await AddSeasonAsync(2026, ct);
        var next = await AddSeasonAsync(2027, ct);

        var december = SeasonTournamentDtoFactory.Create(name: "December", startDate: new DateOnly(2026, 12, 12), endDate: new DateOnly(2026, 12, 12));
        var january = SeasonTournamentDtoFactory.Create(name: "January", startDate: new DateOnly(2027, 1, 16), endDate: new DateOnly(2027, 1, 17));

        var seasonHandler = StrictSeasonHandler();
        seasonHandler
            .Setup(h => h.HandleAsync(It.Is<ListTournamentsInSeasonQuery>(q => q.SeasonId == current.Id), ct))
            .ReturnsAsync([december]);
        seasonHandler
            .Setup(h => h.HandleAsync(It.Is<ListTournamentsInSeasonQuery>(q => q.SeasonId == next.Id), ct))
            .ReturnsAsync([january]);
        var handler = CreateHandler(seasonHandler);

        // Act
        var result = await handler.HandleAsync(Query(), ct);

        // Assert
        result.Select(t => t.Name).ShouldBe(["December", "January"]);
        seasonHandler.Verify(h => h.HandleAsync(It.Is<ListTournamentsInSeasonQuery>(q => q.SeasonId == past.Id), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync drops tournaments that ended before yesterday and keeps ones that end today")]
    public async Task HandleAsync_ShouldDropEndedTournaments_AndKeepOnesEndingToday()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var current = await AddSeasonAsync(2026, ct);

        var ended = SeasonTournamentDtoFactory.Create(name: "Ended", startDate: new DateOnly(2026, 11, 8), endDate: new DateOnly(2026, 11, 9));
        var endsToday = SeasonTournamentDtoFactory.Create(name: "Ends Today", startDate: new DateOnly(2026, 11, 12), endDate: new DateOnly(2026, 11, 13));
        var later = SeasonTournamentDtoFactory.Create(name: "Later", startDate: new DateOnly(2026, 12, 12), endDate: new DateOnly(2026, 12, 12));

        var seasonHandler = StrictSeasonHandler();
        seasonHandler
            .Setup(h => h.HandleAsync(It.Is<ListTournamentsInSeasonQuery>(q => q.SeasonId == current.Id), ct))
            .ReturnsAsync([later, ended, endsToday]);
        var handler = CreateHandler(seasonHandler);

        // Act
        var result = await handler.HandleAsync(Query(), ct);

        // Assert
        result.Select(t => t.Name).ShouldBe(["Ends Today", "Later"]);
    }

    [Theory(DisplayName = "HandleAsync passes the caller scope to the season query")]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task HandleAsync_ShouldPassCallerScopeToSeasonQuery(bool authenticated, bool management)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var current = await AddSeasonAsync(2026, ct);

        var seasonHandler = StrictSeasonHandler();
        seasonHandler
            .Setup(h => h.HandleAsync(
                It.Is<ListTournamentsInSeasonQuery>(q =>
                    q.SeasonId == current.Id
                    && q.CallerIsAuthenticated == authenticated
                    && q.CallerHasTournamentManagementPermission == management),
                ct))
            .ReturnsAsync([]);
        var handler = CreateHandler(seasonHandler);

        // Act
        var result = await handler.HandleAsync(Query(authenticated, management), ct);

        // Assert
        result.ShouldBeEmpty();
        seasonHandler.VerifyAll();
    }
}