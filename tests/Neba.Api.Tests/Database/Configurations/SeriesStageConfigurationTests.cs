using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesStageConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private IEntityType StageEntity()
    {
        using var context = fixture.CreateDbContext();
        return context.Model.FindEntityType(typeof(SeriesStage))!;
    }

    [Fact(DisplayName = "SeriesStage should map to series_stages with its columns required or optional as configured")]
    public void SeriesStage_ShouldMapTableAndColumns()
    {
        // Arrange
        var entity = StageEntity();

        // Act
        var table = entity.GetTableName();
        var order = entity.FindProperty(nameof(SeriesStage.Order))!;
        var kind = entity.FindProperty(nameof(SeriesStage.Kind))!;
        var status = entity.FindProperty(nameof(SeriesStage.Status))!;
        var advancing = entity.FindProperty(nameof(SeriesStage.AdvancingCount))!;
        var tournament = entity.FindProperty(nameof(SeriesStage.TournamentId))!;

        // Assert
        table.ShouldBe("series_stages");
        order.GetColumnName().ShouldBe("stage_order");
        order.IsNullable.ShouldBeFalse();
        kind.IsNullable.ShouldBeFalse();
        status.IsNullable.ShouldBeFalse();
        advancing.IsNullable.ShouldBeTrue();
        tournament.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesStage should be unique per Series order and per tournament")]
    public void SeriesStage_ShouldHaveUniqueOrderAndTournamentIndexes()
    {
        // Arrange
        var entity = StageEntity();

        // Act
        var uniqueIndexes = entity.GetIndexes().Where(i => i.IsUnique).ToList();

        // Assert
        uniqueIndexes.ShouldContain(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { SeriesConfiguration.ForeignKeyName, nameof(SeriesStage.Order) }));
        uniqueIndexes.ShouldContain(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(SeriesStage.TournamentId) }));
    }

    [Fact(DisplayName = "SeriesStage tournament foreign key should block deleting the tournament")]
    public void SeriesStage_ShouldRestrictTournamentDelete()
    {
        // Arrange
        var entity = StageEntity();

        // Act
        var foreignKey = entity.GetForeignKeys().Single(fk => fk.Properties.Any(p => p.Name == nameof(SeriesStage.TournamentId)));

        // Assert
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.NoAction);
    }

    [Fact(DisplayName = "SeriesStage squad designation should map to the squad_* columns with a fixed-length squad id")]
    public void SeriesStage_ShouldMapSquadDesignationColumns()
    {
        // Arrange
        var squad = StageEntity().FindComplexProperty(nameof(SeriesStage.SquadDesignation))!.ComplexType;

        // Act
        var date = squad.FindProperty(nameof(SquadDesignation.Date))!;
        var startTime = squad.FindProperty(nameof(SquadDesignation.StartTime))!;
        var squadId = squad.FindProperty(nameof(SquadDesignation.SquadId))!;

        // Assert
        date.GetColumnName().ShouldBe("squad_date");
        startTime.GetColumnName().ShouldBe("squad_start_time");
        squadId.GetColumnName().ShouldBe("squad_id");
        squadId.GetMaxLength().ShouldBe(26);
        squadId.IsFixedLength().ShouldBe(true);
    }

    [Fact(DisplayName = "SeriesStage should map the advancement columns as configured")]
    public void SeriesStage_ShouldMapAdvancementColumns()
    {
        // Arrange
        var entity = StageEntity();

        // Act
        var isStale = entity.FindProperty(nameof(SeriesStage.IsStale))!;
        var computedAt = entity.FindProperty(nameof(SeriesStage.ComputedAt))!;
        var officialAt = entity.FindProperty(nameof(SeriesStage.OfficialAt))!;
        var slotCount = entity.FindProperty(nameof(SeriesStage.SlotCount))!;

        // Assert
        isStale.IsNullable.ShouldBeFalse();
        isStale.GetColumnName().ShouldBe("is_stale");
        isStale.GetDefaultValue().ShouldBe(false);
        computedAt.GetColumnName().ShouldBe("computed_at_utc");
        computedAt.IsNullable.ShouldBeTrue();
        officialAt.GetColumnName().ShouldBe("official_at_utc");
        officialAt.IsNullable.ShouldBeTrue();
        slotCount.GetColumnName().ShouldBe("slot_count");
        slotCount.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "SeriesStage survivor outcomes and overrides should cascade from the Stage")]
    public void SeriesStage_ShouldCascadeOutcomesAndOverrides()
    {
        // Arrange
        var entity = StageEntity();

        // Act
        var outcomes = entity.FindNavigation(nameof(SeriesStage.SurvivorOutcomes))!.ForeignKey;
        var overrides = entity.FindNavigation(nameof(SeriesStage.Overrides))!.ForeignKey;

        // Assert
        outcomes.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        overrides.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact(DisplayName = "A new Stage should save as not stale with no timestamps or slot count")]
    public async Task Save_ShouldDefaultAdvancementColumns()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        await using var read = fixture.CreateDbContext();
        var stage = await read.Set<SeriesStage>().AsNoTracking().SingleAsync(s => s.Id == seeded.QualifyingStage.Id, TestContext.Current.CancellationToken);

        // Assert
        stage.Status.ShouldBe(StageStatus.Pending);
        stage.IsStale.ShouldBeFalse();
        stage.ComputedAt.ShouldBeNull();
        stage.OfficialAt.ShouldBeNull();
        stage.SlotCount.ShouldBeNull();
    }

    [Fact(DisplayName = "A Computed and an Official Qualifying Stage with the right timestamps should be accepted")]
    public async Task StatusCheck_ShouldAccept_WhenTimestampsMatchStatus()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        await SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "UPDATE app.series_stages SET status = 1, computed_at_utc = now() WHERE kind = 1");
        await SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "UPDATE app.series_stages SET status = 2, official_at_utc = now() WHERE kind = 1");

        // Assert
        await using var read = fixture.CreateDbContext();
        var stage = await read.Set<SeriesStage>().AsNoTracking().SingleAsync(s => s.Id == seeded.QualifyingStage.Id, TestContext.Current.CancellationToken);
        stage.Status.ShouldBe(StageStatus.Official);
        stage.ComputedAt.ShouldNotBeNull();
        stage.OfficialAt.ShouldNotBeNull();
    }

    [Theory(DisplayName = "The status check should reject a Stage whose timestamps do not match its status")]
    [InlineData("status = 1")] // Computed without computed_at_utc
    [InlineData("status = 2, computed_at_utc = now()")] // Official without official_at_utc
    [InlineData("status = 1, computed_at_utc = now(), official_at_utc = now()")] // Computed with official_at_utc
    [InlineData("status = 0, computed_at_utc = now()")] // Pending with computed_at_utc
    public async Task StatusCheck_ShouldReject_WhenTimestampsDoNotMatchStatus(string assignments)
    {
        // Arrange
        await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, $"UPDATE app.series_stages SET {assignments} WHERE kind = 1"));

        // Assert
        sqlState.ShouldBe("23514");
    }

    [Fact(DisplayName = "The check constraints should accept an Official Placement Stage")]
    public async Task StatusCheck_ShouldAccept_WhenPlacementStageIsOfficial()
    {
        // Arrange
        await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(
                fixture,
                "UPDATE app.series_stages SET status = 2, computed_at_utc = now(), official_at_utc = now() WHERE kind = 3"));

        // Assert
        sqlState.ShouldBeNull();
    }

    [Fact(DisplayName = "The check constraint should reject a negative slot count")]
    public async Task SlotCountCheck_ShouldReject_WhenSlotCountIsNegative()
    {
        // Arrange
        await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "UPDATE app.series_stages SET slot_count = -1"));

        // Assert
        sqlState.ShouldBe("23514");
    }
}