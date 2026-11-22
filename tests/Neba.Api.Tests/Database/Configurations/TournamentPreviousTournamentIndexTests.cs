using EntityFramework.Exceptions.Common;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentPreviousTournamentIndexTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2020, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private Tournament Create(int year, Tournament? previous = null)
        => TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(year, 3, 7),
            endDate: new DateOnly(year, 3, 8),
            seasonId: _season.Id,
            previousTournament: previous is null
                ? null
                : PreviousTournamentFactsFactory.Create(previous.Id, TournamentType.Doubles, previous.EndDate));

    [Fact(DisplayName = "Saving two tournaments that share a previous tournament throws a unique constraint violation the resolver recognizes")]
    public async Task Save_ShouldThrow_WhenTwoTournamentsShareAPreviousTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previous = Create(2024);
        await _dbContext.Tournaments.AddAsync(previous, ct);
        await _dbContext.Tournaments.AddAsync(Create(2025, previous), ct);
        await _dbContext.SaveChangesAsync(ct);
        await _dbContext.Tournaments.AddAsync(Create(2026, previous), ct);

        // Act
        var exception = await Should.ThrowAsync<UniqueConstraintException>(() => _dbContext.SaveChangesAsync(ct));

        // Assert
        PreviousTournamentResolver.IsPreviousTournamentViolation(exception).ShouldBeTrue();
    }

    [Fact(DisplayName = "Saving several tournaments with no previous tournament succeeds")]
    public async Task Save_ShouldSucceed_WhenSeveralTournamentsHaveNoPreviousTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _dbContext.Tournaments.AddRangeAsync([Create(2024), Create(2025), Create(2026)], ct);

        // Act
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        (await _dbContext.Tournaments.CountAsync(ct)).ShouldBe(3);
    }

    [Fact(DisplayName = "IsPreviousTournamentViolation is false for a unique constraint violation on another property")]
    public async Task IsPreviousTournamentViolation_ShouldBeFalse_ForAnotherConstraint()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var duplicate = SeasonFactory.Create(id: _season.Id, startDate: new DateOnly(2040, 1, 1), endDate: new DateOnly(2040, 12, 31));
        await using var other = fixture.CreateDbContext();
        await other.Seasons.AddAsync(duplicate, ct);
        var exception = await Should.ThrowAsync<UniqueConstraintException>(() => other.SaveChangesAsync(ct));

        // Act
        var result = PreviousTournamentResolver.IsPreviousTournamentViolation(exception);

        // Assert
        result.ShouldBeFalse();
    }
}
