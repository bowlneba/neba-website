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
public sealed class SeriesSurvivorOutcomeConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private IEntityType OutcomeEntity()
    {
        using var context = fixture.CreateDbContext();
        return context.Model.FindEntityType(typeof(SurvivorOutcome))!;
    }

    [Fact(DisplayName = "SurvivorOutcome should map to series_survivor_outcomes with its columns required or optional as configured")]
    public void SurvivorOutcome_ShouldMapTableAndColumns()
    {
        // Arrange
        var entity = OutcomeEntity();

        // Act
        var bowler = entity.FindProperty(nameof(SurvivorOutcome.BowlerId))!;
        var blockTotal = entity.FindProperty(nameof(SurvivorOutcome.BlockTotal))!;
        var advanced = entity.FindProperty(nameof(SurvivorOutcome.Advanced))!;
        var stageId = entity.FindProperty(SeriesStageConfiguration.ForeignKeyName)!;

        // Assert
        entity.GetTableName().ShouldBe("series_survivor_outcomes");
        entity.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
        bowler.GetColumnName().ShouldBe("bowler_id");
        bowler.IsNullable.ShouldBeFalse();
        blockTotal.GetColumnName().ShouldBe("block_total");
        blockTotal.IsNullable.ShouldBeTrue();
        advanced.IsNullable.ShouldBeFalse();
        stageId.GetColumnName().ShouldBe("series_stage_id");
        stageId.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "SurvivorOutcome should have the composite primary key (series_stage_id, bowler_id)")]
    public void SurvivorOutcome_ShouldHaveCompositePrimaryKey()
    {
        // Arrange
        var entity = OutcomeEntity();

        // Act
        var keyNames = entity.FindPrimaryKey()!.Properties.Select(p => p.Name).ToList();

        // Assert
        keyNames.ShouldBe([SeriesStageConfiguration.ForeignKeyName, nameof(SurvivorOutcome.BowlerId)]);
    }

    [Fact(DisplayName = "SurvivorOutcome foreign keys should cascade from the Stage and restrict the bowler")]
    public void SurvivorOutcome_ShouldConfigureForeignKeyDeleteBehaviors()
    {
        // Arrange
        var entity = OutcomeEntity();

        // Act
        var stage = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeriesStage));
        var bowler = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Bowler));

        // Assert
        stage.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        bowler.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact(DisplayName = "A survivor outcome should round-trip with Advanced false and a null block total")]
    public async Task RoundTrip_ShouldPersistAdvancedFalseAndNullBlockTotal()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded, blockTotal: "NULL", advanced: "FALSE");

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking()
            .Include(s => s.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        var outcome = loaded.Stages.Single(stage => stage.Kind == StageKind.Survivor).SurvivorOutcomes.Single();
        outcome.BowlerId.ShouldBe(seeded.Bowler.Id);
        outcome.BlockTotal.ShouldBeNull();
        outcome.Advanced.ShouldBeFalse();
    }

    [Fact(DisplayName = "A negative block total should be rejected")]
    public async Task Insert_ShouldReject_WhenBlockTotalIsNegative()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded, blockTotal: "-1"));

        // Assert
        sqlState.ShouldBe("23514");
    }

    [Fact(DisplayName = "A bowler should have one outcome per Stage")]
    public async Task Insert_ShouldReject_WhenBowlerAlreadyHasOutcomeOnTheStage()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded));

        // Assert
        sqlState.ShouldBe("23505");
    }

    [Fact(DisplayName = "Deleting a bowler with a survivor outcome should be blocked")]
    public async Task DeleteBowler_ShouldBeBlocked_WhenBowlerHasOutcome()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.bowlers"));

        // Assert
        sqlState.ShouldBe("23503");
    }

    [Fact(DisplayName = "Deleting a Stage should delete its survivor outcomes")]
    public async Task DeleteStage_ShouldCascadeToOutcomes()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertSurvivorOutcomeAsync(fixture, seeded);

        // Act
        await SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.series_stages WHERE kind = 2");

        // Assert
        (await SeriesAdvancementTestData.CountAsync(fixture, "series_survivor_outcomes")).ShouldBe(0);
    }
}
