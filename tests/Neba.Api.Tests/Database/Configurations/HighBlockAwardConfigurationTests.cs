using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Seasons")]
public sealed class HighBlockAwardConfigurationTests
{
    private readonly IEntityType _awardType;

    public HighBlockAwardConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _awardType = context.Model.FindEntityType(typeof(HighBlockAward))!;
    }

    [Fact(DisplayName = "maps to high_block_awards table in app schema")]
    public void Configure_ShouldMapToHighBlockAwardsTable()
    {
        // Act & Assert
        _awardType.GetTableName().ShouldBe("high_block_awards");
        _awardType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        // Act
        var property = _awardType.FindProperty(nameof(HighBlockAward.Id))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        // Act
        var alternateKey = _awardType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(HighBlockAward.Id)));

        // Assert
        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "season_id shadow foreign key is not nullable")]
    public void Configure_ShouldConfigureSeasonIdShadowProperty()
    {
        // Act
        var property = _awardType.FindProperty(SeasonConfiguration.ForeignKeyName)!;

        // Assert
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "season foreign key uses cascade delete")]
    public void Configure_ShouldConfigureSeasonForeignKeyWithCascadeDelete()
    {
        // Act
        var foreignKey = _awardType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == SeasonConfiguration.ForeignKeyName));

        // Assert
        foreignKey.ShouldNotBeNull();
        foreignKey!.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact(DisplayName = "bowler_id is char(26), not nullable")]
    public void Configure_ShouldConfigureBowlerIdColumn()
    {
        // Act
        var property = _awardType.FindProperty(nameof(HighBlockAward.BowlerId))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(BowlerConfiguration.ForeignKeyName);
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "bowler_id foreign key targets Bowler.Id")]
    public void Configure_ShouldConfigureBowlerForeignKey()
    {
        // Act
        var foreignKey = _awardType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(HighBlockAward.BowlerId)));

        // Assert
        foreignKey.ShouldNotBeNull();
        foreignKey!.PrincipalEntityType.ClrType.ShouldBe(typeof(Bowler));
        foreignKey.PrincipalKey.Properties.Select(p => p.Name).ShouldContain(nameof(Bowler.Id));
    }

    [Fact(DisplayName = "score is not nullable")]
    public void Configure_ShouldConfigureScoreColumn()
    {
        // Act
        var property = _awardType.FindProperty(nameof(HighBlockAward.BlockScore))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("score");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "season_id has an index")]
    public void Configure_ShouldConfigureSeasonIdIndex()
    {
        // Act
        var index = _awardType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == SeasonConfiguration.ForeignKeyName));

        // Assert
        index.ShouldNotBeNull();
    }
}
