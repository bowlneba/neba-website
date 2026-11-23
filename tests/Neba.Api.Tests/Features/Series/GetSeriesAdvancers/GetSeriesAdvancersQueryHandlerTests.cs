using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancers;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class GetSeriesAdvancersQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = AdvancementReadSeed.Now;

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly AppDbContext _readContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _readContext.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<SeriesAdvancersDto> ReadAsync(SeriesId seriesId)
    {
        var result = await new GetSeriesAdvancersQueryHandler(_readContext)
            .HandleAsync(new GetSeriesAdvancersQuery { SeriesId = seriesId }, TestContext.Current.CancellationToken);
        result.IsError.ShouldBeFalse();
        return result.Value;
    }

    [Fact(DisplayName = "HandleAsync should return NotFound for an unknown Series")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        // Arrange & Act
        var result = await new GetSeriesAdvancersQueryHandler(_readContext)
            .HandleAsync(new GetSeriesAdvancersQuery { SeriesId = SeriesId.New() }, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should return no Stages when every Qualifying and Survivor Stage is only Computed")]
    public async Task HandleAsync_ShouldReturnNoStages_WhenNothingIsOfficial()
    {
        // Arrange
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, TestContext.Current.CancellationToken);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should list the names of an Official Qualifying Stage by place, with no admin data")]
    public async Task HandleAsync_ShouldListQualifiers_WhenQualifyingStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var stage = dto.Stages.Single();
        stage.Order.ShouldBe(1);
        stage.Kind.ShouldBe("Qualifying");
        stage.Advancers.Select(a => a.BowlerId).Take(3).ShouldBe([scenario.Id(0), scenario.Id(1), scenario.Id(2)]);
        stage.Advancers.Select(a => a.Place).Take(3).ShouldBe([1, 2, 3]);
        stage.Advancers.ShouldAllBe(a => a.BlockTotal == null);
    }

    [Fact(DisplayName = "HandleAsync should include Backfill entries, flagged and last, in the last Qualifying Stage")]
    public async Task HandleAsync_ShouldFlagBackfillEntries_InLastQualifyingStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            QualifierResults = [(0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6), (6, 7), (7, 8)],
            NonMembers = new HashSet<int> { 6, 7 },
            BackfillPoints = [(9, 90), (8, 50)],
        });
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var advancers = dto.Stages.Single().Advancers;
        advancers.Where(a => a.IsBackfill).Select(a => a.BowlerId).ShouldBe([scenario.Id(8), scenario.Id(9)], ignoreOrder: true);
        advancers.Take(advancers.Count(a => !a.IsBackfill)).ShouldAllBe(a => !a.IsBackfill && a.Place != null);
        advancers.Where(a => a.IsBackfill).ShouldAllBe(a => a.Place == null);
    }

    [Fact(DisplayName = "HandleAsync should list only the bowlers who advanced from an Official Survivor Stage, best total first")]
    public async Task HandleAsync_ShouldListOnlyAdvancedBowlers_WhenSurvivorStageIsOfficial()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        scenario.Stage(2).MarkOfficial(Now.AddHours(3));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var survivor = dto.Stages.Single(s => s.Kind == "Survivor");
        survivor.Order.ShouldBe(2);
        survivor.Advancers.ShouldNotBeEmpty();
        survivor.Advancers.ShouldAllBe(a => a.BlockTotal != null && a.Place == null && !a.IsBackfill);
        survivor.Advancers.Select(a => a.BlockTotal!.Value).ShouldBe(survivor.Advancers.Select(a => a.BlockTotal!.Value).OrderDescending());
    }

    [Fact(DisplayName = "HandleAsync should leave out a Computed Survivor Stage even when an earlier Stage is Official")]
    public async Task HandleAsync_ShouldLeaveOutComputedSurvivorStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);
        scenario.Stage(1).MarkOfficial(Now.AddHours(2));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.Select(s => s.Kind).ShouldBe(["Qualifying"]);
    }

    [Fact(DisplayName = "HandleAsync should leave out the Placement Stage when the Series has no Finale")]
    public async Task HandleAsync_ShouldLeaveOutPlacementStage_WhenNoFinale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct);

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        dto.Stages.ShouldNotContain(s => s.Kind == "Placement");
    }

    [Fact(DisplayName = "HandleAsync should list the finalists of a computed Placement Stage when the Series has a Finale")]
    public async Task HandleAsync_ShouldListFinalists_WhenSeriesHasFinale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await AdvancementReadSeed.SeedAsync(_dbContext, ct, new ReadScenarioOptions
        {
            Finale = SeriesFinaleDefinitionFactory.Create(finalistCount: 3),
        });

        // Act
        var dto = await ReadAsync(scenario.Series.Id);

        // Assert
        var placement = dto.Stages.Single(s => s.Kind == "Placement");
        placement.Advancers.Count.ShouldBe(3);
        placement.Advancers.ShouldAllBe(a => !a.IsBackfill && a.BlockTotal == null);
    }
}
