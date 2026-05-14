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
public sealed class SideCutCriteriaConfigurationTests
{
    private readonly IEntityType _criteriaType;

    public SideCutCriteriaConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        var groupType = context.Model.FindEntityType(typeof(SideCutCriteriaGroup))!;
        _criteriaType = groupType
            .FindNavigation(nameof(SideCutCriteriaGroup.Criteria))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to side_cut_criteria table in app schema")]
    public void Configure_ShouldMapToSideCutCriteriaTable()
    {
        _criteriaType.GetTableName().ShouldBe("side_cut_criteria");
        _criteriaType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "minimum_age is nullable")]
    public void Configure_ShouldConfigureMinimumAgeNullable()
    {
        var property = _criteriaType.FindProperty(nameof(SideCutCriteria.MinimumAge))!;

        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "maximum_age is nullable")]
    public void Configure_ShouldConfigureMaximumAgeNullable()
    {
        var property = _criteriaType.FindProperty(nameof(SideCutCriteria.MaximumAge))!;

        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "gender_requirement is varchar(1), nullable")]
    public void Configure_ShouldConfigureGenderRequirementColumn()
    {
        var property = _criteriaType.FindProperty(nameof(SideCutCriteria.GenderRequirement))!;

        property.GetMaxLength().ShouldBe(1);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "side_cut_criteria_group_id has an index")]
    public void Configure_ShouldConfigureGroupIdIndex()
    {
        var index = _criteriaType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == SideCutCriteriaGroupConfiguration.ForeignKey));

        index.ShouldNotBeNull();
    }
}