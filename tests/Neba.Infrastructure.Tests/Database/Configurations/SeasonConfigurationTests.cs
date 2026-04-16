using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Domain.Seasons;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Configurations;

[UnitTest]
[Component("Seasons")]
public sealed class SeasonConfigurationTests
{
    private readonly IEntityType _seasonType;

    public SeasonConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _seasonType = context.Model.FindEntityType(typeof(Season))!;
    }

    [Fact(DisplayName = "maps to seasons table in app schema")]
    public void Configure_ShouldMapToSeasonsTable()
    {
        _seasonType.GetTableName().ShouldBe("seasons");
        _seasonType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _seasonType.FindProperty(nameof(Season.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _seasonType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Season.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "description is varchar(31), not nullable")]
    public void Configure_ShouldConfigureDescriptionColumn()
    {
        var property = _seasonType.FindProperty(nameof(Season.Description))!;

        property.GetMaxLength().ShouldBe(31);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "start_date is not nullable")]
    public void Configure_ShouldConfigureStartDateColumn()
    {
        var property = _seasonType.FindProperty(nameof(Season.StartDate))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "end_date is not nullable")]
    public void Configure_ShouldConfigureEndDateColumn()
    {
        var property = _seasonType.FindProperty(nameof(Season.EndDate))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "complete is not nullable")]
    public void Configure_ShouldConfigureCompleteColumn()
    {
        var property = _seasonType.FindProperty(nameof(Season.Complete))!;

        property.IsNullable.ShouldBeFalse();
    }
}