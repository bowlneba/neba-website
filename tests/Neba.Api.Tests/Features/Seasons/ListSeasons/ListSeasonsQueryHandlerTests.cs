using Neba.Api.Database;
using Neba.Api.Features.Seasons.ListSeasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Features.Seasons.ListSeasons;

[IntegrationTest]
[Component("Seasons")]
[Collection<PostgreSqlFixture>]
public sealed class ListSeasonsQueryHandlerTests(PostgreSqlFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns empty collection when no seasons exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoSeasonsExist()
    {
        var handler = new ListSeasonsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(
            new ListSeasonsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns all seasons with correct fields")]
    public async Task HandleAsync_ShouldReturnAllSeasons_WithCorrectFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            description: "2025 Season",
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListSeasonsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListSeasonsQuery(), ct);

        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Id.ShouldBe(season.Id);
        dto.Description.ShouldBe("2025 Season");
        dto.StartDate.ShouldBe(new DateOnly(2025, 1, 1));
        dto.EndDate.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Fact(DisplayName = "HandleAsync returns seasons ordered by start date descending")]
    public async Task HandleAsync_ShouldReturnSeasonsOrderedByStartDateDescending()
    {
        var ct = TestContext.Current.CancellationToken;
        var seasonA = SeasonFactory.Create(startDate: new DateOnly(2023, 1, 1), endDate: new DateOnly(2023, 12, 31));
        var seasonB = SeasonFactory.Create(startDate: new DateOnly(2025, 1, 1), endDate: new DateOnly(2025, 12, 31));
        var seasonC = SeasonFactory.Create(startDate: new DateOnly(2024, 1, 1), endDate: new DateOnly(2024, 12, 31));
        await _dbContext.Seasons.AddRangeAsync([seasonA, seasonB, seasonC], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListSeasonsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListSeasonsQuery(), ct);

        var list = result.ToList();
        list.Count.ShouldBe(3);
        list[0].StartDate.ShouldBe(new DateOnly(2025, 1, 1));
        list[1].StartDate.ShouldBe(new DateOnly(2024, 1, 1));
        list[2].StartDate.ShouldBe(new DateOnly(2023, 1, 1));
    }
}
