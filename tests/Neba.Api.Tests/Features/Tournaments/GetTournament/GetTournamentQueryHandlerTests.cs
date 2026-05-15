using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<PostgreSqlFixture>]
public sealed class GetTournamentQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "HandleAsync returns TournamentNotFound when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = TournamentId.New() },
            TestContext.Current.CancellationToken);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns tournament detail with correct fields when tournament exists")]
    public async Task HandleAsync_ShouldReturnTournamentDetail_WithCorrectFields_WhenTournamentExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(description: "2025 Season");
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles 2025",
            seasonId: season.Id,
            statsEligible: true,
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2025, 10, 4),
            endDate: new DateOnly(2025, 10, 5));
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldBe(tournament.Id);
        result.Value.Name.ShouldBe("NEBA Singles 2025");
        result.Value.Season.ShouldBe("2025 Season");
        result.Value.StatsEligible.ShouldBeTrue();
        result.Value.Winners.ShouldBeEmpty();
        result.Value.Results.ShouldBeEmpty();
        result.Value.EntryCount.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when tournament has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenTournamentHasLogo()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var logo = Neba.TestFactory.Storage.StoredFileFactory.Create(
            container: "logos",
            path: "tournaments/neba-singles.jpg");
        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/logos/tournaments/neba-singles.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("logos", "tournaments/neba-singles.jpg"))
            .Returns(expectedUri);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object);

        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id }, ct);

        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedUri);
    }
}
