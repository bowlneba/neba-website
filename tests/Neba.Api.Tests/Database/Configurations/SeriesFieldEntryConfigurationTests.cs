using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesFieldEntryConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private IEntityType EntryEntity()
    {
        using var context = fixture.CreateDbContext();
        return context.Model.FindEntityType(typeof(SeriesFieldEntry))!;
    }

    [Fact(DisplayName = "SeriesFieldEntry should map to series_field_entries with its columns required or optional as configured")]
    public void SeriesFieldEntry_ShouldMapTableAndColumns()
    {
        // Arrange
        var entity = EntryEntity();

        // Act
        var table = entity.GetTableName();
        var domainId = entity.FindProperty(nameof(SeriesFieldEntry.Id))!;
        var bowler = entity.FindProperty(nameof(SeriesFieldEntry.BowlerId))!;
        var source = entity.FindProperty(nameof(SeriesFieldEntry.SourceStageId))!;
        var place = entity.FindProperty(nameof(SeriesFieldEntry.Place))!;
        var points = entity.FindProperty(nameof(SeriesFieldEntry.BackfillPoints))!;
        var viaOverride = entity.FindProperty(nameof(SeriesFieldEntry.ViaOverride))!;
        var seriesId = entity.FindProperty(SeriesConfiguration.ForeignKeyName)!;

        // Assert
        table.ShouldBe("series_field_entries");
        entity.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
        domainId.GetColumnName().ShouldBe("domain_id");
        bowler.GetColumnName().ShouldBe("bowler_id");
        bowler.IsNullable.ShouldBeFalse();
        source.GetColumnName().ShouldBe("source_stage_id");
        source.IsNullable.ShouldBeTrue();
        source.GetMaxLength().ShouldBe(26);
        source.IsFixedLength().ShouldBe(true);
        place.IsNullable.ShouldBeTrue();
        points.IsNullable.ShouldBeTrue();
        viaOverride.IsNullable.ShouldBeFalse();
        viaOverride.FindAnnotation("Relational:DefaultValue").ShouldBeNull();
        seriesId.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesFieldEntry should have a unique domain ID and be unique per Series and bowler")]
    public void SeriesFieldEntry_ShouldHaveAlternateKeyAndUniqueSeriesBowlerIndex()
    {
        // Arrange
        var entity = EntryEntity();

        // Act
        var alternateKeys = entity.GetKeys().Where(key => !key.IsPrimaryKey()).ToList();
        var uniqueIndexes = entity.GetIndexes().Where(index => index.IsUnique).ToList();

        // Assert
        alternateKeys.ShouldContain(key => key.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(SeriesFieldEntry.Id) }));
        uniqueIndexes.ShouldContain(index => index.Properties.Select(p => p.Name)
            .SequenceEqual(new[] { SeriesConfiguration.ForeignKeyName, nameof(SeriesFieldEntry.BowlerId) }));
    }

    [Fact(DisplayName = "SeriesFieldEntry foreign keys should cascade from the Series, restrict the bowler and not act on the source Stage")]
    public void SeriesFieldEntry_ShouldConfigureForeignKeyDeleteBehaviors()
    {
        // Arrange
        var entity = EntryEntity();

        // Act
        var series = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeriesAggregate));
        var bowler = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Bowler));
        var stage = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeriesStage));

        // Assert
        series.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        bowler.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        stage.DeleteBehavior.ShouldBe(DeleteBehavior.NoAction);
    }

    [Fact(DisplayName = "A Stage entry and a Backfill entry should round-trip with ViaOverride false")]
    public async Task RoundTrip_ShouldPersistStageAndBackfillEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        var backfillBowler = BowlerFactory.Create();
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Bowlers.Add(backfillBowler);
            await setup.SaveChangesAsync(ct);
        }

        await SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded, place: "3");
        await SeriesAdvancementTestData.InsertFieldEntryAsync(
            fixture, seeded, backfillBowler, backfill: true, place: "NULL", backfillPoints: "250");

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().Include(s => s.FieldEntries).SingleAsync(ct);

        // Assert
        var stageEntry = loaded.FieldEntries.Single(entry => entry.BowlerId == seeded.Bowler.Id);
        stageEntry.SourceStageId.ShouldBe(seeded.QualifyingStage.Id);
        stageEntry.Place.ShouldBe(3);
        stageEntry.BackfillPoints.ShouldBeNull();
        stageEntry.ViaOverride.ShouldBeFalse();

        var backfillEntry = loaded.FieldEntries.Single(entry => entry.BowlerId == backfillBowler.Id);
        backfillEntry.SourceStageId.ShouldBeNull();
        backfillEntry.Place.ShouldBeNull();
        backfillEntry.BackfillPoints.ShouldBe(250);
        backfillEntry.ViaOverride.ShouldBeFalse();
    }

    [Fact(DisplayName = "An override entry should round-trip with ViaOverride true")]
    public async Task RoundTrip_ShouldPersistViaOverrideTrue()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded, viaOverride: "TRUE");

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().Include(s => s.FieldEntries).SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        loaded.FieldEntries.Single().ViaOverride.ShouldBeTrue();
    }

    [Theory(DisplayName = "The check constraints should reject a malformed field entry")]
    [InlineData(false, "1", "10", "FALSE")] // Stage entry with points
    [InlineData(false, "NULL", "NULL", "FALSE")] // Stage entry with no place
    [InlineData(true, "NULL", "NULL", "FALSE")] // Backfill entry with no points
    [InlineData(true, "2", "10", "FALSE")] // Backfill entry with a place
    [InlineData(true, "NULL", "10", "TRUE")] // Backfill entry via override
    [InlineData(false, "0", "NULL", "FALSE")] // Place not positive
    [InlineData(true, "NULL", "-1", "FALSE")] // Negative points
    public async Task Insert_ShouldReject_WhenCheckConstraintFails(bool backfill, string place, string points, string viaOverride)
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded, backfill: backfill, place: place, backfillPoints: points, viaOverride: viaOverride));

        // Assert
        sqlState.ShouldBe("23514");
    }

    [Fact(DisplayName = "A bowler should not enter the same Series Field twice")]
    public async Task Insert_ShouldReject_WhenBowlerIsAlreadyInTheField()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded, backfill: true, place: "NULL", backfillPoints: "5"));

        // Assert
        sqlState.ShouldBe("23505");
    }

    [Fact(DisplayName = "Deleting a bowler in the Series Field should be blocked")]
    public async Task DeleteBowler_ShouldBeBlocked_WhenBowlerHasFieldEntry()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.bowlers"));

        // Assert
        sqlState.ShouldBe("23503");
    }

    [Fact(DisplayName = "Deleting a Stage that sourced a field entry should be blocked")]
    public async Task DeleteStage_ShouldBeBlocked_WhenStageSourcedAnEntry()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertFieldEntryAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.series_stages WHERE kind = 1"));

        // Assert
        sqlState.ShouldBe("23503");
    }
}