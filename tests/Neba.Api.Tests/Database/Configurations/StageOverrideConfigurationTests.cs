using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class StageOverrideConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private IEntityType OverrideEntity()
    {
        using var context = fixture.CreateDbContext();
        return context.Model.FindEntityType(typeof(StageOverride))!;
    }

    [Fact(DisplayName = "StageOverride should map to series_stage_overrides with its columns, lengths and types as configured")]
    public void StageOverride_ShouldMapTableAndColumns()
    {
        // Arrange
        var entity = OverrideEntity();

        // Act
        var kind = entity.FindProperty(nameof(StageOverride.Kind))!;
        var reason = entity.FindProperty(nameof(StageOverride.Reason))!;
        var createdBy = entity.FindProperty(nameof(StageOverride.CreatedBy))!;
        var createdAt = entity.FindProperty(nameof(StageOverride.CreatedAt))!;
        var applied = entity.FindProperty(nameof(StageOverride.Applied))!;
        var bowler = entity.FindProperty(nameof(StageOverride.BowlerId))!;
        var stageId = entity.FindProperty(SeriesStageConfiguration.ForeignKeyName)!;

        // Assert
        entity.GetTableName().ShouldBe("series_stage_overrides");
        entity.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
        kind.IsNullable.ShouldBeFalse();
        kind.GetColumnType().ShouldBe("integer");
        reason.GetMaxLength().ShouldBe(500);
        reason.IsNullable.ShouldBeFalse();
        createdBy.GetMaxLength().ShouldBe(64);
        createdBy.IsNullable.ShouldBeFalse();
        createdAt.GetColumnName().ShouldBe("created_at_utc");
        createdAt.IsNullable.ShouldBeFalse();
        applied.IsNullable.ShouldBeFalse();
        applied.FindAnnotation("Relational:DefaultValue").ShouldBeNull();
        bowler.GetColumnName().ShouldBe("bowler_id");
        stageId.GetColumnName().ShouldBe("series_stage_id");
    }

    [Fact(DisplayName = "StageOverride should have a unique domain ID and be unique per Stage and bowler")]
    public void StageOverride_ShouldHaveAlternateKeyAndUniqueStageBowlerIndex()
    {
        // Arrange
        var entity = OverrideEntity();

        // Act
        var alternateKeys = entity.GetKeys().Where(key => !key.IsPrimaryKey()).ToList();
        var uniqueIndexes = entity.GetIndexes().Where(index => index.IsUnique).ToList();

        // Assert
        alternateKeys.ShouldContain(key => key.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(StageOverride.Id) }));
        uniqueIndexes.ShouldContain(index => index.Properties.Select(p => p.Name)
            .SequenceEqual(new[] { SeriesStageConfiguration.ForeignKeyName, nameof(StageOverride.BowlerId) }));
    }

    [Fact(DisplayName = "StageOverride foreign keys should cascade from the Stage and restrict the bowler, with no foreign key for the creator")]
    public void StageOverride_ShouldConfigureForeignKeyDeleteBehaviors()
    {
        // Arrange
        var entity = OverrideEntity();

        // Act
        var foreignKeys = entity.GetForeignKeys().ToList();
        var stage = foreignKeys.Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeriesStage));
        var bowler = foreignKeys.Single(fk => fk.PrincipalEntityType.ClrType == typeof(Bowler));

        // Assert
        stage.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        bowler.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        foreignKeys.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "An override should round-trip with Kind Clear and Applied false")]
    public async Task RoundTrip_ShouldPersistKindAndAppliedFalse()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded, applied: "FALSE", kind: 2);

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking()
            .Include(s => s.Stages).ThenInclude(stage => stage.Overrides)
            .SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        var stageOverride = loaded.Stages.Single(stage => stage.Kind == StageKind.Qualifying).Overrides.Single();
        stageOverride.BowlerId.ShouldBe(seeded.Bowler.Id);
        stageOverride.Kind.ShouldBe(OverrideKind.Clear);
        stageOverride.Reason.ShouldBe("reason");
        stageOverride.CreatedBy.ShouldBe("admin");
        stageOverride.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        stageOverride.Applied.ShouldBeFalse();
    }

    [Fact(DisplayName = "An override should round-trip with Applied true")]
    public async Task RoundTrip_ShouldPersistAppliedTrue()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded, applied: "TRUE");

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking()
            .Include(s => s.Stages).ThenInclude(stage => stage.Overrides)
            .SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.Stages.SelectMany(stage => stage.Overrides).Single().Applied.ShouldBeTrue();
    }

    [Fact(DisplayName = "A bowler should have one override per Stage")]
    public async Task Insert_ShouldReject_WhenBowlerAlreadyHasOverrideOnTheStage()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded, kind: 2));

        // Assert
        sqlState.ShouldBe("23505");
    }

    [Theory(DisplayName = "A reason over 500 characters or a creator over 64 characters should be rejected")]
    [InlineData("reason", 501)]
    [InlineData("created_by", 65)]
    public async Task Update_ShouldReject_WhenTextIsTooLong(string column, int length)
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, $"UPDATE app.series_stage_overrides SET {column} = repeat('x', {length})"));

        // Assert
        sqlState.ShouldBe("22001");
    }

    [Fact(DisplayName = "Deleting a bowler with an override should be blocked")]
    public async Task DeleteBowler_ShouldBeBlocked_WhenBowlerHasOverride()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.bowlers"));

        // Assert
        sqlState.ShouldBe("23503");
    }

    [Fact(DisplayName = "Deleting a Stage should delete its overrides")]
    public async Task DeleteStage_ShouldCascadeToOverrides()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertOverrideAsync(fixture, seeded);

        // Act
        await SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.series_stages WHERE kind = 1");

        // Assert
        (await SeriesAdvancementTestData.CountAsync(fixture, "series_stage_overrides")).ShouldBe(0);
    }
}
