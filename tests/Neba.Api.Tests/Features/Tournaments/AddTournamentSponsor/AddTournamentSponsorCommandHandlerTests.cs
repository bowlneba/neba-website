using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.AddTournamentSponsor;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.AddTournamentSponsor;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class AddTournamentSponsorCommandHandlerTests(AppDbContextFixture fixture)
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

    private AddTournamentSponsorCommandHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        return new AddTournamentSponsorCommandHandler(_dbContext, cache);
    }

    private static AddTournamentSponsorCommand ValidCommand(
        TournamentId tournamentId,
        SponsorId sponsorId,
        bool? titleSponsor = null,
        decimal? sponsorshipAmount = null)
        => new()
        {
            TournamentId = tournamentId,
            SponsorId = sponsorId,
            TitleSponsor = titleSponsor ?? false,
            SponsorshipAmount = sponsorshipAmount ?? 500m
        };

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
        var sponsor = await SeedSponsorAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(TournamentId.New(), sponsor.Id);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the sponsor does not exist")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSponsorDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, SponsorId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorNotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the sponsor was already added")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenSponsorAlreadyAdded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id);
        (await handler.HandleAsync(command, ct)).IsError.ShouldBeFalse();

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.SponsorAlreadyAdded");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the sponsorship amount is negative")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSponsorshipAmountIsNegative()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id, sponsorshipAmount: -1m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentSponsor.NegativeSponsorshipAmount");
    }

    [Fact(DisplayName = "HandleAsync does not persist a sponsor when adding it fails")]
    public async Task HandleAsync_ShouldNotPersistSponsor_WhenAddingFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id, sponsorshipAmount: -1m);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var persisted = await _dbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Sponsors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync persists the sponsor when the command is valid")]
    public async Task HandleAsync_ShouldPersistSponsor_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id, titleSponsor: true, sponsorshipAmount: 1234.56m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleAsync(t => t.Id == tournament.Id, ct);
        var persistedSponsor = persisted.Sponsors.Single(s => s.SponsorId == sponsor.Id);
        persistedSponsor.TitleSponsor.ShouldBeTrue();
        persistedSponsor.SponsorshipAmount.ShouldBe(1234.56m);
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament cache tag when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateTournamentCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var cacheTag = $"neba:tournaments:{tournament.Id}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var cachedAfterAdd = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        cachedAfterAdd.ShouldBe("fresh-tournament");
    }

    [Fact(DisplayName = "HandleAsync invalidates the season cache tag when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateSeasonCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.SeasonId}:list";
        var cacheTag = $"neba:tournaments:{tournament.SeasonId}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-season-tournaments"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var cachedAfterAdd = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-season-tournaments"),
            token: ct);
        cachedAfterAdd.ShouldBe("fresh-season-tournaments");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache tag when adding the sponsor fails")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenAddingSponsorFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedSponsorAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var cacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var cacheTag = $"neba:tournaments:{tournament.Id}";

        await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, sponsor.Id, sponsorshipAmount: -1m);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was persisted
        var cachedAfterAdd = await cache.GetOrSetAsync(
            cacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        cachedAfterAdd.ShouldBe("cached-tournament");
    }
}
