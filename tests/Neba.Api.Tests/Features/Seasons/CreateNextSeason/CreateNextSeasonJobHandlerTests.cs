using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.CreateNextSeason;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Features.Seasons.CreateNextSeason;

[IntegrationTest]
[Component("Seasons")]
[Collection<AppDbContextFixture>]
public sealed class CreateNextSeasonJobHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 2, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly FakeLogger<CreateNextSeasonJobHandler> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private CreateNextSeasonJobHandler CreateHandler()
        => new(_dbContext, _timeProvider, _logger);

    [Fact(DisplayName = "Should create next year's season when it does not yet exist")]
    public async Task ExecuteAsync_ShouldCreateNextSeason_WhenItDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CreateNextSeasonJob(), ct);

        // Assert
        var season = await _dbContext.Seasons.AsNoTracking()
            .SingleAsync(s => s.EndDate == new DateOnly(2027, 12, 31), ct);

        season.Description.ShouldBe("2027 Season");
        season.StartDate.ShouldBe(new DateOnly(2027, 1, 1));
        season.EndDate.ShouldBe(new DateOnly(2027, 12, 31));
        season.Complete.ShouldBeFalse();

        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l => l.Level == LogLevel.Information && l.Message.Contains("Created 2027 season"));
    }

    [Fact(DisplayName = "Should not create a season and should log when next year's season already exists")]
    public async Task ExecuteAsync_ShouldNotCreateSeason_WhenNextSeasonAlreadyExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existing = SeasonFactory.Create(
            description: "2027 Season",
            startDate: new DateOnly(2027, 1, 1),
            endDate: new DateOnly(2027, 12, 31));
        await _dbContext.Seasons.AddAsync(existing, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        await handler.ExecuteAsync(new CreateNextSeasonJob(), ct);

        // Assert
        var seasonCount = await _dbContext.Seasons.AsNoTracking()
            .CountAsync(s => s.EndDate == new DateOnly(2027, 12, 31), ct);
        seasonCount.ShouldBe(1);

        var logs = _logger.Collector.GetSnapshot();
        logs.ShouldContain(l => l.Level == LogLevel.Information && l.Message.Contains("already exists"));
    }
}
