using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentValidationServiceTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns SeasonNotFound when season does not exist")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnSeasonNotFound_WhenSeasonMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var missingSeasonId = SeasonId.New();
        var tournament = TournamentFactory.Create(seasonId: missingSeasonId);

        var service = new TournamentValidationService(_dbContext);

        // Act
        var result = await service.IsTournamentValidForSeasonAsync(tournament, missingSeasonId, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Season.NotFound");
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns Success when tournament dates are within season dates")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnSuccess_WhenDatesAreValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 3, 1),
            endDate: new DateOnly(2025, 3, 2),
            seasonId: season.Id);

        var service = new TournamentValidationService(_dbContext);

        // Act
        var result = await service.IsTournamentValidForSeasonAsync(tournament, season.Id, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns error when tournament starts before season")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnError_WhenStartDateBeforeSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2024, 12, 31),
            endDate: new DateOnly(2025, 1, 2),
            seasonId: season.Id);

        var service = new TournamentValidationService(_dbContext);

        // Act
        var result = await service.IsTournamentValidForSeasonAsync(tournament, season.Id, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidDatesForSeason");
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns error when tournament ends after season")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnError_WhenEndDateAfterSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 12, 30),
            endDate: new DateOnly(2026, 1, 1),
            seasonId: season.Id);

        var service = new TournamentValidationService(_dbContext);

        // Act
        var result = await service.IsTournamentValidForSeasonAsync(tournament, season.Id, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidDatesForSeason");
    }
}