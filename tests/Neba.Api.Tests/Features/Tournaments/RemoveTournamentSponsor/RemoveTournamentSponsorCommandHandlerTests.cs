using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.RemoveTournamentSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.RemoveTournamentSponsor;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class RemoveTournamentSponsorCommandHandlerTests(AppDbContextFixture fixture)
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

    private RemoveTournamentSponsorCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return new RemoveTournamentSponsorCommandHandler(_dbContext, cache);
    }

    private async Task<Tournament> SeedTournamentAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    private async Task<Sponsor> SeedSponsorAsync(CancellationToken ct)
    {
        var sponsor = SponsorFactory.Create();
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);
        return sponsor;
    }

    [Fact(DisplayName = "HandleAsync returns a not found error when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = TournamentId.New(), SponsorId = SponsorId.New() };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the sponsor is not attached")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenSponsorNotAttached()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = tournament.Id, SponsorId = SponsorId.New() };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorNotAttached");
    }

    [Fact(DisplayName = "HandleAsync removes the sponsor when it is attached")]
    public async Task HandleAsync_ShouldRemoveSponsor_WhenAttached()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        tournament.AddSponsor(sponsor.Id, titleSponsor: false, sponsorshipAmount: 500m);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = tournament.Id, SponsorId = sponsor.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Sponsors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament cache tag when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateTournamentCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        tournament.AddSponsor(sponsor.Id, titleSponsor: false, sponsorshipAmount: 500m);
        await _dbContext.SaveChangesAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var cacheTag = $"neba:tournaments:{tournament.Id}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = tournament.Id, SponsorId = sponsor.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var cachedAfterRemove = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        cachedAfterRemove.ShouldBe("fresh-tournament");
    }

    [Fact(DisplayName = "HandleAsync invalidates the season cache tag when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateSeasonCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        tournament.AddSponsor(sponsor.Id, titleSponsor: false, sponsorshipAmount: 500m);
        await _dbContext.SaveChangesAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.SeasonId}:list";
        var cacheTag = $"neba:tournaments:{tournament.SeasonId}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-season-tournaments"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = tournament.Id, SponsorId = sponsor.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var cachedAfterRemove = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-season-tournaments"),
            token: ct);
        cachedAfterRemove.ShouldBe("fresh-season-tournaments");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache tag when the sponsor is not attached")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenSponsorNotAttached()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var cacheTag = $"neba:tournaments:{tournament.Id}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = new RemoveTournamentSponsorCommand { TournamentId = tournament.Id, SponsorId = SponsorId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was removed
        var cachedAfterRemove = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        cachedAfterRemove.ShouldBe("cached-tournament");
    }
}
