using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class PreviousTournamentResolverTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "ResolveAsync returns null facts when no previous tournament is given")]
    public async Task ResolveAsync_ShouldReturnNull_WhenNoPreviousTournamentGiven()
    {
        // Arrange & Act
        var result = await PreviousTournamentResolver.ResolveAsync(
            _dbContext, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "ResolveAsync returns the id, type and end date of an existing tournament")]
    public async Task ResolveAsync_ShouldReturnFacts_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(startDate: new DateOnly(2024, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        var tournament = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2025, 3, 7),
            endDate: new DateOnly(2025, 3, 8),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await PreviousTournamentResolver.ResolveAsync(_dbContext, tournament.Id, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();
        result.Value.Value.Id.ShouldBe(tournament.Id);
        result.Value.Value.TournamentType.ShouldBe(TournamentType.Doubles);
        result.Value.Value.EndDate.ShouldBe(new DateOnly(2025, 3, 8));
    }

    [Fact(DisplayName = "ResolveAsync returns a conflict error when the tournament does not exist")]
    public async Task ResolveAsync_ShouldReturnNotFoundError_WhenTournamentMissing()
    {
        // Arrange
        var missingId = TournamentId.New();

        // Act
        var result = await PreviousTournamentResolver.ResolveAsync(
            _dbContext, missingId, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.PreviousTournament.NotFound");
    }
}
