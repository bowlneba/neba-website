using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentSponsorAmount;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class EditTournamentSponsorAmountCommandHandlerTests(AppDbContextFixture fixture)
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

    private EditTournamentSponsorAmountCommandHandler CreateHandler()
        => new(_dbContext, _serviceProvider.GetRequiredService<IFusionCache>());

    private static EditTournamentSponsorAmountCommand ValidCommand(
        TournamentId tournamentId,
        SponsorId sponsorId,
        decimal? sponsorshipAmount = null)
        => new()
        {
            TournamentId = tournamentId,
            SponsorId = sponsorId,
            SponsorshipAmount = sponsorshipAmount ?? 750m
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

    private async Task<Sponsor> SeedAttachedSponsorAsync(Tournament tournament, CancellationToken ct)
    {
        var sponsor = SponsorFactory.Create();
        await _dbContext.Sponsors.AddAsync(sponsor, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.AddSponsor(sponsor.Id, titleSponsor: true, sponsorshipAmount: 500m).IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);
        return sponsor;
    }

    [Fact(DisplayName = "HandleAsync returns a not found error when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(ValidCommand(TournamentId.New(), SponsorId.New()), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the sponsor is not attached")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenSponsorIsNotAttached()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(ValidCommand(tournament.Id, SponsorId.New()), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Tournament.SponsorNotAttached");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error and keeps the amount when the amount is negative")]
    public async Task HandleAsync_ShouldReturnValidationErrorAndKeepAmount_WhenAmountIsNegative()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedAttachedSponsorAsync(tournament, ct);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(ValidCommand(tournament.Id, sponsor.Id, sponsorshipAmount: -1m), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TournamentSponsor.NegativeSponsorshipAmount");
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .Include(t => t.Sponsors)
            .SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Sponsors.Single().SponsorshipAmount.ShouldBe(500m);
    }

    [Fact(DisplayName = "HandleAsync persists the new amount and leaves the title sponsor flag alone")]
    public async Task HandleAsync_ShouldPersistNewAmount_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedAttachedSponsorAsync(tournament, ct);
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(ValidCommand(tournament.Id, sponsor.Id, sponsorshipAmount: 1234.56m), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .Include(t => t.Sponsors)
            .SingleAsync(t => t.Id == tournament.Id, ct);
        var persistedSponsor = persisted.Sponsors.Single();
        persistedSponsor.SponsorshipAmount.ShouldBe(1234.56m);
        persistedSponsor.TitleSponsor.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament and season cache tags when the command is valid")]
    public async Task HandleAsync_ShouldInvalidateCacheTags_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var sponsor = await SeedAttachedSponsorAsync(tournament, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var detailKey = $"neba:tournaments:{tournament.Id}:detail";
        var listKey = $"neba:tournaments:{tournament.SeasonId}:list";

        await cache.GetOrSetAsync(detailKey, _ => Task.FromResult("cached-detail"), tags: [$"neba:tournaments:{tournament.Id}"], token: ct);
        await cache.GetOrSetAsync(listKey, _ => Task.FromResult("cached-list"), tags: [$"neba:tournaments:{tournament.SeasonId}"], token: ct);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(ValidCommand(tournament.Id, sponsor.Id), ct);

        // Assert — a stale cached value would be returned instead of invoking the factory
        (await cache.GetOrSetAsync(detailKey, _ => Task.FromResult("fresh-detail"), token: ct)).ShouldBe("fresh-detail");
        (await cache.GetOrSetAsync(listKey, _ => Task.FromResult("fresh-list"), token: ct)).ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache when the edit fails")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenEditFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var detailKey = $"neba:tournaments:{tournament.Id}:detail";

        await cache.GetOrSetAsync(detailKey, _ => Task.FromResult("cached-detail"), tags: [$"neba:tournaments:{tournament.Id}"], token: ct);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(ValidCommand(tournament.Id, SponsorId.New()), ct);

        // Assert
        (await cache.GetOrSetAsync(detailKey, _ => Task.FromResult("fresh-detail"), token: ct)).ShouldBe("cached-detail");
    }
}