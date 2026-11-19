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
    : IClassFixture<AppDbContextFixture>
{
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
}