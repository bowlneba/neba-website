using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.TruncateTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.TruncateTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TruncateTournamentCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private TruncateTournamentCommandHandler CreateHandler()
        => new(_dbContext, _serviceProvider.GetRequiredService<IFusionCache>());

    private async Task<Tournament> SeedTournamentAsync(CancellationToken ct, TournamentStatus? status = null)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, status: status);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new TruncateTournamentCommand { TournamentId = TournamentId.New() };

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns success and marks the tournament truncated when it is scheduled")]
    public async Task HandleAsync_ShouldReturnSuccessAndMarkTruncated_WhenTournamentIsScheduled()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = new TruncateTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var reloaded = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        reloaded.Status.ShouldBe(TournamentStatus.Truncated);
    }

    [Fact(DisplayName = "HandleAsync returns Tournament.AlreadyFinalized when the tournament is already finalized")]
    public async Task HandleAsync_ShouldReturnAlreadyFinalized_WhenTournamentIsAlreadyFinalized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, status: TournamentStatus.Cancelled);
        var handler = CreateHandler();
        var command = new TruncateTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.AlreadyFinalized");
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament and season cache tags when the tournament is truncated")]
    public async Task HandleAsync_ShouldInvalidateTournamentAndSeasonCacheTags_WhenTournamentIsTruncated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        var tournamentCacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var seasonCacheKey = $"neba:tournaments:{tournament.SeasonId}:list";

        await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [$"neba:tournaments:{tournament.Id}"],
            token: ct);
        await cache.GetOrSetAsync(
            seasonCacheKey,
            _ => Task.FromResult("cached-season-tournaments"),
            tags: [$"neba:tournaments:{tournament.SeasonId}"],
            token: ct);

        var handler = CreateHandler();
        var command = new TruncateTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var tournamentCacheAfterTruncate = await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        tournamentCacheAfterTruncate.ShouldBe("fresh-tournament");

        var seasonCacheAfterTruncate = await cache.GetOrSetAsync(
            seasonCacheKey,
            _ => Task.FromResult("fresh-season-tournaments"),
            token: ct);
        seasonCacheAfterTruncate.ShouldBe("fresh-season-tournaments");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache when the tournament is already finalized")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentIsAlreadyFinalized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, status: TournamentStatus.Cancelled);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var tournamentCacheKey = $"neba:tournaments:{tournament.Id}:detail";

        await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [$"neba:tournaments:{tournament.Id}"],
            token: ct);

        var handler = CreateHandler();
        var command = new TruncateTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing changed
        var tournamentCacheAfterTruncate = await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        tournamentCacheAfterTruncate.ShouldBe("cached-tournament");
    }
}