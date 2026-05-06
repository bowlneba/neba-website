using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Configurations;

[UnitTest]
[Component("Tournaments")]
public sealed class SideCutConfigurationTests
{
    private readonly IEntityType _sideCutType;
    private readonly IEntityType _criteriaGroupType;

    public SideCutConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _sideCutType = context.Model.FindEntityType(typeof(SideCut))!;
        _criteriaGroupType = _sideCutType
            .FindNavigation(nameof(SideCut.CriteriaGroups))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to side_cuts table in app schema")]
    public void Configure_ShouldMapToSideCutsTable()
    {
        _sideCutType.GetTableName().ShouldBe("side_cuts");
        _sideCutType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _sideCutType.FindProperty(nameof(SideCut.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _sideCutType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(SideCut.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "name is varchar(31), not nullable")]
    public void Configure_ShouldConfigureNameColumn()
    {
        var property = _sideCutType.FindProperty(nameof(SideCut.Name))!;

        property.GetMaxLength().ShouldBe(31);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "color_indicator is not nullable and has a value converter")]
    public void Configure_ShouldConfigureColorIndicatorColumn()
    {
        var property = _sideCutType.FindProperty(nameof(SideCut.Indicator))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("color_indicator");
        property.IsNullable.ShouldBeFalse();
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Fact(DisplayName = "logical_operator is not nullable")]
    public void Configure_ShouldConfigureLogicalOperatorColumn()
    {
        var property = _sideCutType.FindProperty(nameof(SideCut.LogicalOperator))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "active is not nullable")]
    public void Configure_ShouldConfigureActiveColumn()
    {
        var property = _sideCutType.FindProperty(nameof(SideCut.Active))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "criteria groups navigation uses side_cut_id foreign key")]
    public void Configure_ShouldConfigureCriteriaGroupsForeignKey()
    {
        var fk = _criteriaGroupType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SideCutConfiguration.ForeignKey));

        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "criteria groups cascade on delete")]
    public void Configure_ShouldCascadeDeleteCriteriaGroups()
    {
        var fk = _criteriaGroupType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SideCutConfiguration.ForeignKey));

        fk!.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}
