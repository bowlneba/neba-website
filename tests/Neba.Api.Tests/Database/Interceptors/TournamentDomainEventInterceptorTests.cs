using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Database.Interceptors;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Database.Interceptors;

// Real Postgres, the real DomainEventDispatcherInterceptor and the real DomainEventPublisher over a
// mock IBackgroundJobClient: proves a tournament's squad events become Hangfire jobs through a save.
[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentDomainEventInterceptorTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _seedDb = fixture.CreateDbContext();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _seedDb.DisposeAsync();
    }

    private sealed class CountingSaveInterceptor : SaveChangesInterceptor
    {
        public int SavingCalls { get; private set; }

        public int SavedCalls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SavingCalls++;

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            SavedCalls++;

            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }

    private static DomainEventPublisher CreatePublisher(List<Job> createdJobs)
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => createdJobs.Add(job))
            .Returns("job-1");

        return new DomainEventPublisher(client.Object);
    }

    private async Task<SeasonId> SeedSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _seedDb.Seasons.AddAsync(season, ct);
        await _seedDb.SaveChangesAsync(ct);

        return season.Id;
    }

    [Fact(DisplayName = "Saving a new tournament with a squad should enqueue exactly one TournamentSquadsChanged job")]
    public async Task SaveChangesAsync_ShouldEnqueueOneSquadsChangedJob_WhenNewTournamentHasSquad()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await SeedSeasonAsync(ct);
        var tournament = TournamentFactory.Create(seasonId: seasonId, squads: [SquadFactory.Create()]);
        var createdJobs = new List<Job>();
        await using var db = fixture.CreateDbContext(new DomainEventDispatcherInterceptor(CreatePublisher(createdJobs)));
        await db.Tournaments.AddAsync(tournament, ct);

        // Act
        await db.SaveChangesAsync(ct);

        // Assert
        var job = createdJobs.ShouldHaveSingleItem();
        job.Type.ShouldBe(typeof(IDomainEventJob<TournamentSquadsChanged>));
        job.Args[0].ShouldBeOfType<TournamentSquadsChanged>().TournamentId.ShouldBe(tournament.Id);
        tournament.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Saving a stored tournament after a batch of squad changes should enqueue exactly one TournamentSquadsChanged job")]
    public async Task SaveChangesAsync_ShouldEnqueueOneSquadsChangedJob_WhenBatchOfSquadChangesIsSaved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await SeedSeasonAsync(ct);
        var stored = TournamentFactory.Create(seasonId: seasonId);
        await _seedDb.Tournaments.AddAsync(stored, ct);
        await _seedDb.SaveChangesAsync(ct);

        var createdJobs = new List<Job>();
        await using var db = fixture.CreateDbContext(new DomainEventDispatcherInterceptor(CreatePublisher(createdJobs)));
        var tournament = await db.Tournaments.Include(t => t.Squads).SingleAsync(t => t.Id == stored.Id, ct);
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime, maxEntries: null, legacyId: null).IsError.ShouldBeFalse();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime.AddHours(2), maxEntries: null, legacyId: null).IsError.ShouldBeFalse();
        tournament.AddSquad(SquadFactory.ValidBowlingDateTime.AddHours(4), maxEntries: null, legacyId: null).IsError.ShouldBeFalse();

        // Act
        await db.SaveChangesAsync(ct);

        // Assert
        var job = createdJobs.ShouldHaveSingleItem();
        job.Type.ShouldBe(typeof(IDomainEventJob<TournamentSquadsChanged>));
        job.Args[0].ShouldBeOfType<TournamentSquadsChanged>().TournamentId.ShouldBe(stored.Id);
    }

    [Fact(DisplayName = "A save with no tracked changes should still run the SavingChanges and SavedChanges interceptors")]
    public async Task SaveChangesAsync_ShouldStillRunInterceptors_WhenNothingChanged()
    {
        // Arrange - settles plan note 11: EF does call SavedChanges on a save that writes no rows, so the explicit
        // PublishAndClear in SyncTournamentResultsJob is a safety net, not the only path for a no-row re-sync.
        var ct = TestContext.Current.CancellationToken;
        var seasonId = await SeedSeasonAsync(ct);
        var stored = TournamentFactory.Create(seasonId: seasonId);
        await _seedDb.Tournaments.AddAsync(stored, ct);
        await _seedDb.SaveChangesAsync(ct);

        var counter = new CountingSaveInterceptor();
        await using var db = fixture.CreateDbContext(counter);
        await db.Tournaments.SingleAsync(t => t.Id == stored.Id, ct);

        // Act
        var rowsWritten = await db.SaveChangesAsync(ct);

        // Assert
        rowsWritten.ShouldBe(0);
        counter.SavingCalls.ShouldBe(1);
        counter.SavedCalls.ShouldBe(1);
    }
}