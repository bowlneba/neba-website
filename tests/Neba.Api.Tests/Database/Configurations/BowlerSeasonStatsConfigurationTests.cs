using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Stats;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Stats")]
public sealed class BowlerSeasonStatsConfigurationTests
{
    private readonly IEntityType _statsType;

    public BowlerSeasonStatsConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _statsType = context.Model.FindEntityType(typeof(BowlerSeasonStats))!;
    }

    [Fact(DisplayName = "maps to bowler_season_stats table in app schema")]
    public void Configure_ShouldMapToBowlerSeasonStatsTable()
    {
        _statsType.GetTableName().ShouldBe("bowler_season_stats");
        _statsType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "composite primary key is (SeasonId, BowlerId)")]
    public void Configure_ShouldConfigureCompositePrimaryKey()
    {
        var pkPropertyNames = _statsType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ToList();

        pkPropertyNames.ShouldContain(nameof(BowlerSeasonStats.SeasonId));
        pkPropertyNames.ShouldContain(nameof(BowlerSeasonStats.BowlerId));
        pkPropertyNames.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "season_id is char(26), not nullable")]
    public void Configure_ShouldConfigureSeasonIdColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.SeasonId))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(SeasonConfiguration.ForeignKeyName);
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "season foreign key targets Season.Id with restrict delete")]
    public void Configure_ShouldConfigureSeasonForeignKey()
    {
        var foreignKey = _statsType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(BowlerSeasonStats.SeasonId)));

        foreignKey.ShouldNotBeNull();
        foreignKey!.PrincipalEntityType.ClrType.ShouldBe(typeof(Season));
        foreignKey.PrincipalKey.Properties.Select(p => p.Name).ShouldContain(nameof(Season.Id));
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact(DisplayName = "bowler_id is char(26), not nullable")]
    public void Configure_ShouldConfigureBowlerIdColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.BowlerId))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(BowlerConfiguration.ForeignKeyName);
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "bowler foreign key targets Bowler.Id with restrict delete")]
    public void Configure_ShouldConfigureBowlerForeignKey()
    {
        var foreignKey = _statsType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(BowlerSeasonStats.BowlerId)));

        foreignKey.ShouldNotBeNull();
        foreignKey!.PrincipalEntityType.ClrType.ShouldBe(typeof(Bowler));
        foreignKey.PrincipalKey.Properties.Select(p => p.Name).ShouldContain(nameof(Bowler.Id));
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact(DisplayName = "bowler_id has an index")]
    public void Configure_ShouldConfigureBowlerIdIndex()
    {
        var index = _statsType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(BowlerSeasonStats.BowlerId)));

        index.ShouldNotBeNull();
    }

    [Fact(DisplayName = "classification flags are not nullable")]
    public void Configure_ShouldConfigureClassificationFlags()
    {
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsMember))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsRookie))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsSenior))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsSuperSenior))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsWoman))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.IsYouth))!.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "eligible_tournaments maps to tournaments column, not nullable")]
    public void Configure_ShouldConfigureEligibleTournamentsColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.EligibleTournaments))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("tournaments");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "eligible_entries maps to entries column, not nullable")]
    public void Configure_ShouldConfigureEligibleEntriesColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.EligibleEntries))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("entries");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "participation counts are not nullable")]
    public void Configure_ShouldConfigureParticipationCounts()
    {
        _statsType.FindProperty(nameof(BowlerSeasonStats.TotalTournaments))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.TotalEntries))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.Cashes))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.Finals))!.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "performance stats are not nullable")]
    public void Configure_ShouldConfigurePerformanceStats()
    {
        _statsType.FindProperty(nameof(BowlerSeasonStats.TotalGames))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.TotalPinfall))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.QualifyingHighGame))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.HighBlock))!.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "field_average is decimal(5,2), not nullable")]
    public void Configure_ShouldConfigureFieldAverageColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.FieldAverage))!;

        property.GetPrecision().ShouldBe(5);
        property.GetScale().ShouldBe(2);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "high_finish is nullable")]
    public void Configure_ShouldConfigureHighFinishColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.HighFinish))!;

        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "average_finish is decimal(3,1), nullable")]
    public void Configure_ShouldConfigureAverageFinishColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.AverageFinish))!;

        property.GetPrecision().ShouldBe(3);
        property.GetScale().ShouldBe(1);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "match play stats are not nullable")]
    public void Configure_ShouldConfigureMatchPlayStats()
    {
        _statsType.FindProperty(nameof(BowlerSeasonStats.MatchPlayWins))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.MatchPlayLosses))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.MatchPlayGames))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.MatchPlayPinfall))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.MatchPlayHighGame))!.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "points fields are not nullable")]
    public void Configure_ShouldConfigurePointsFields()
    {
        _statsType.FindProperty(nameof(BowlerSeasonStats.BowlerOfTheYearPoints))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.SeniorOfTheYearPoints))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.SuperSeniorOfTheYearPoints))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.WomanOfTheYearPoints))!.IsNullable.ShouldBeFalse();
        _statsType.FindProperty(nameof(BowlerSeasonStats.YouthOfTheYearPoints))!.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "financial fields are decimal(7,2), not nullable")]
    public void Configure_ShouldConfigureFinancialFields()
    {
        var winnings = _statsType.FindProperty(nameof(BowlerSeasonStats.TournamentWinnings))!;
        winnings.GetPrecision().ShouldBe(7);
        winnings.GetScale().ShouldBe(2);
        winnings.IsNullable.ShouldBeFalse();

        var cupEarnings = _statsType.FindProperty(nameof(BowlerSeasonStats.CupEarnings))!;
        cupEarnings.GetPrecision().ShouldBe(7);
        cupEarnings.GetScale().ShouldBe(2);
        cupEarnings.IsNullable.ShouldBeFalse();

        var credits = _statsType.FindProperty(nameof(BowlerSeasonStats.Credits))!;
        credits.GetPrecision().ShouldBe(7);
        credits.GetScale().ShouldBe(2);
        credits.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "last_updated_utc maps to last_updated_utc column, not nullable")]
    public void Configure_ShouldConfigureLastUpdatedUtcColumn()
    {
        var property = _statsType.FindProperty(nameof(BowlerSeasonStats.LastUpdatedUtc))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("last_updated_utc");
        property.IsNullable.ShouldBeFalse();
    }
}