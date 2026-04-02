using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Domain.Bowlers;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Configurations;

[UnitTest]
[Component("Bowlers")]
public sealed class BowlerConfigurationTests
{
    private readonly IEntityType _bowlerType;
    private readonly IEntityType _nameType;

    public BowlerConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _bowlerType = context.Model.FindEntityType(typeof(Bowler))!;
        _nameType = _bowlerType.FindNavigation(nameof(Bowler.Name))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to bowlers table in app schema")]
    public void Configure_ShouldMapToBowlersTable()
    {
        _bowlerType.GetTableName().ShouldBe("bowlers");
        _bowlerType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is character(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _bowlerType.FindProperty(nameof(Bowler.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _bowlerType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Bowler.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "first_name is varchar(31), not nullable")]
    public void Configure_ShouldConfigureFirstNameColumn()
    {
        var property = _nameType.FindProperty(nameof(Name.FirstName))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("first_name");
        property.GetMaxLength().ShouldBe(31);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "middle_name is varchar(31), nullable")]
    public void Configure_ShouldConfigureMiddleNameColumn()
    {
        var property = _nameType.FindProperty(nameof(Name.MiddleName))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("middle_name");
        property.GetMaxLength().ShouldBe(31);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "last_name is varchar(63), not nullable")]
    public void Configure_ShouldConfigureLastNameColumn()
    {
        var property = _nameType.FindProperty(nameof(Name.LastName))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("last_name");
        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "suffix is varchar(7), nullable")]
    public void Configure_ShouldConfigureSuffixColumn()
    {
        var property = _nameType.FindProperty(nameof(Name.Suffix))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("suffix");
        property.GetMaxLength().ShouldBe(7);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "nickname is varchar(31), nullable")]
    public void Configure_ShouldConfigureNicknameColumn()
    {
        var property = _nameType.FindProperty(nameof(Name.Nickname))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("nickname");
        property.GetMaxLength().ShouldBe(31);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "Name has composite index on last_name, first_name")]
    public void Configure_ShouldConfigureNameCompositeIndex()
    {
        var index = _nameType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Any(p => p.Name == nameof(Name.LastName)) &&
                i.Properties.Any(p => p.Name == nameof(Name.FirstName)));

        index.ShouldNotBeNull();
    }

    [Fact(DisplayName = "website_id is never generated")]
    public void Configure_ShouldConfigureWebsiteIdValueGeneratedNever()
    {
        var property = _bowlerType.FindProperty(nameof(Bowler.WebsiteId))!;

        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "website_id has a unique index with nulls distinct")]
    public void Configure_ShouldConfigureWebsiteIdUniqueIndex()
    {
        var index = _bowlerType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Bowler.WebsiteId)));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
        index.FindAnnotation("Npgsql:NullsDistinct")?.Value.ShouldBe(true);
    }

    [Fact(DisplayName = "legacy_id is never generated")]
    public void Configure_ShouldConfigureLegacyIdValueGeneratedNever()
    {
        var property = _bowlerType.FindProperty(nameof(Bowler.LegacyId))!;

        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "legacy_id has a unique index with nulls distinct")]
    public void Configure_ShouldConfigureLegacyIdUniqueIndex()
    {
        var index = _bowlerType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Bowler.LegacyId)));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
        index.FindAnnotation("Npgsql:NullsDistinct")?.Value.ShouldBe(true);
    }
}