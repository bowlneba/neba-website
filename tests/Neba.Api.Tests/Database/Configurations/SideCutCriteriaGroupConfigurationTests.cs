using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Tournaments")]
public sealed class SideCutCriteriaGroupConfigurationTests
{
    private readonly IEntityType _groupType;
    private readonly IEntityType _criteriaType;

    public SideCutCriteriaGroupConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _groupType = context.Model.FindEntityType(typeof(SideCutCriteriaGroup))!;
        _criteriaType = _groupType
            .FindNavigation(nameof(SideCutCriteriaGroup.Criteria))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to side_cut_criteria_groups table in app schema")]
    public void Configure_ShouldMapToSideCutCriteriaGroupsTable()
    {
        _groupType.GetTableName().ShouldBe("side_cut_criteria_groups");
        _groupType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _groupType.FindProperty(nameof(SideCutCriteriaGroup.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _groupType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(SideCutCriteriaGroup.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "logical_operator is not nullable")]
    public void Configure_ShouldConfigureLogicalOperatorColumn()
    {
        var property = _groupType.FindProperty(nameof(SideCutCriteriaGroup.LogicalOperator))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "sort_order is not nullable")]
    public void Configure_ShouldConfigureSortOrderColumn()
    {
        var property = _groupType.FindProperty(nameof(SideCutCriteriaGroup.SortOrder))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "side_cut_id + sort_order has a unique index")]
    public void Configure_ShouldConfigureUniqueSortOrderIndex()
    {
        var index = _groupType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == SideCutConfiguration.ForeignKey) &&
                i.Properties.Any(p => p.Name == nameof(SideCutCriteriaGroup.SortOrder)));

        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
    }

    [Fact(DisplayName = "criteria navigation uses side_cut_criteria_group_id foreign key")]
    public void Configure_ShouldConfigureCriteriaForeignKey()
    {
        var fk = _criteriaType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SideCutCriteriaGroupConfiguration.ForeignKey));

        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "criteria cascade on delete")]
    public void Configure_ShouldCascadeDeleteCriteria()
    {
        var fk = _criteriaType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SideCutCriteriaGroupConfiguration.ForeignKey));

        fk!.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}