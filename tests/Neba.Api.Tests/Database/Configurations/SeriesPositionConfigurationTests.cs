using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Database.Configurations;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesPositionConfigurationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private IEntityType PositionEntity()
    {
        using var context = fixture.CreateDbContext();
        return context.Model.FindEntityType(typeof(SeriesPosition))!;
    }

    [Fact(DisplayName = "SeriesPosition should map to series_positions with its columns required or optional as configured")]
    public void SeriesPosition_ShouldMapTableAndColumns()
    {
        // Arrange
        var entity = PositionEntity();

        // Act
        var bowler = entity.FindProperty(nameof(SeriesPosition.BowlerId))!;
        var position = entity.FindProperty(nameof(SeriesPosition.Position))!;
        var bowled = entity.FindProperty(nameof(SeriesPosition.Bowled))!;
        var finalist = entity.FindProperty(nameof(SeriesPosition.IsFinalist))!;
        var seriesId = entity.FindProperty(SeriesConfiguration.ForeignKeyName)!;

        // Assert
        entity.GetTableName().ShouldBe("series_positions");
        entity.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
        bowler.GetColumnName().ShouldBe("bowler_id");
        bowler.IsNullable.ShouldBeFalse();
        position.GetColumnName().ShouldBe("position");
        position.IsNullable.ShouldBeFalse();
        bowled.IsNullable.ShouldBeFalse();
        finalist.IsNullable.ShouldBeTrue();
        seriesId.GetColumnName().ShouldBe("series_id");
        seriesId.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesPosition should have the composite primary key (series_id, bowler_id) and a non-unique (series_id, position) index")]
    public void SeriesPosition_ShouldHaveCompositeKeyAndPositionIndex()
    {
        // Arrange
        var entity = PositionEntity();

        // Act
        var keyNames = entity.FindPrimaryKey()!.Properties.Select(p => p.Name).ToList();
        var positionIndex = entity.GetIndexes().Single(index => index.Properties.Select(p => p.Name)
            .SequenceEqual(new[] { SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.Position) }));

        // Assert
        keyNames.ShouldBe([SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.BowlerId)]);
        positionIndex.IsUnique.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesPosition foreign keys should cascade from the Series and restrict the bowler")]
    public void SeriesPosition_ShouldConfigureForeignKeyDeleteBehaviors()
    {
        // Arrange
        var entity = PositionEntity();

        // Act
        var series = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(SeriesAggregate));
        var bowler = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Bowler));

        // Assert
        series.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        bowler.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Theory(DisplayName = "A position should round-trip with Bowled and IsFinalist as stored")]
    [InlineData("FALSE", "NULL", false, null)]
    [InlineData("TRUE", "FALSE", true, false)]
    [InlineData("TRUE", "TRUE", true, true)]
    public async Task RoundTrip_ShouldPersistFlags(string bowledSql, string finalistSql, bool bowled, bool? finalist)
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded, position: "4", bowled: bowledSql, isFinalist: finalistSql);

        // Act
        await using var read = fixture.CreateDbContext();
        var loaded = await read.Series.AsNoTracking().Include(s => s.Positions).SingleAsync(TestContext.Current.CancellationToken);

        // Assert
        var position = loaded.Positions.Single();
        position.BowlerId.ShouldBe(seeded.Bowler.Id);
        position.Position.ShouldBe(4);
        position.Bowled.ShouldBe(bowled);
        position.IsFinalist.ShouldBe(finalist);
    }

    [Fact(DisplayName = "A position below one should be rejected")]
    public async Task Insert_ShouldReject_WhenPositionIsNotPositive()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded, position: "0"));

        // Assert
        sqlState.ShouldBe("23514");
    }

    [Fact(DisplayName = "A bowler should have one position per Series")]
    public async Task Insert_ShouldReject_WhenBowlerAlreadyHasPosition()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded, position: "2"));

        // Assert
        sqlState.ShouldBe("23505");
    }

    [Fact(DisplayName = "Deleting a bowler with a position should be blocked")]
    public async Task DeleteBowler_ShouldBeBlocked_WhenBowlerHasPosition()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded);

        // Act
        var sqlState = await SeriesAdvancementTestData.SqlStateOfAsync(
            () => SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.bowlers"));

        // Assert
        sqlState.ShouldBe("23503");
    }

    [Fact(DisplayName = "Deleting a Series should delete its positions")]
    public async Task DeleteSeries_ShouldCascadeToPositions()
    {
        // Arrange
        var seeded = await SeriesAdvancementTestData.SeedAsync(fixture);
        await SeriesAdvancementTestData.InsertPositionAsync(fixture, seeded);

        // Act
        await SeriesAdvancementTestData.ExecuteSqlAsync(fixture, "DELETE FROM app.series");

        // Assert
        (await SeriesAdvancementTestData.CountAsync(fixture, "series_positions")).ShouldBe(0);
    }
}
