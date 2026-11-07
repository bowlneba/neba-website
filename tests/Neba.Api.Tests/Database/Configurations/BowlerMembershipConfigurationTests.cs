using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Memberships")]
public sealed class BowlerMembershipConfigurationTests
{
    private readonly IEntityType _membershipType;

    public BowlerMembershipConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _membershipType = context.Model.FindEntityType(typeof(BowlerMembership))!;
    }

    [Fact(DisplayName = "maps to bowler_memberships table in app schema")]
    public void Configure_ShouldMapToBowlerMembershipsTable()
    {
        // Act & Assert
        _membershipType.GetTableName().ShouldBe("bowler_memberships");
        _membershipType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        // Act
        var property = _membershipType.FindProperty(nameof(BowlerMembership.Id))!;

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
        var alternateKey = _membershipType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(BowlerMembership.Id)));

        // Assert
        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "legacy_id is nullable, value generated never")]
    public void Configure_ShouldConfigureLegacyIdColumn()
    {
        // Act
        var property = _membershipType.FindProperty(nameof(BowlerMembership.LegacyId))!;

        // Assert
        property.IsNullable.ShouldBeTrue();
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "legacy_id has a unique index that allows many nulls")]
    public void Configure_ShouldConfigureLegacyIdUniqueIndex()
    {
        // Act
        var index = _membershipType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(BowlerMembership.LegacyId)));

        // Assert
        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
        index.FindAnnotation("Npgsql:NullsDistinct")?.Value.ShouldBe(true);
    }

    [Fact(DisplayName = "bowler_id shadow foreign key is not nullable")]
    public void Configure_ShouldConfigureBowlerIdShadowProperty()
    {
        // Act
        var property = _membershipType.FindProperty(BowlerConfiguration.ForeignKeyName)!;

        // Assert
        property.IsShadowProperty().ShouldBeTrue();
        property.ClrType.ShouldBe(typeof(int));
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "bowler foreign key targets Bowler and uses cascade delete")]
    public void Configure_ShouldConfigureBowlerForeignKeyWithCascadeDelete()
    {
        // Act
        var foreignKey = _membershipType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == BowlerConfiguration.ForeignKeyName));

        // Assert
        foreignKey.ShouldNotBeNull();
        foreignKey.PrincipalEntityType.ClrType.ShouldBe(typeof(Bowler));
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact(DisplayName = "membership_type is varchar(32), not nullable, stored as the type's code")]
    public void Configure_ShouldConfigureMembershipTypeColumn()
    {
        // Act
        var property = _membershipType.FindProperty(nameof(BowlerMembership.Type))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("membership_type");
        property.GetMaxLength().ShouldBe(32);
        property.IsNullable.ShouldBeFalse();
        property.GetValueConverter()!.ProviderClrType.ShouldBe(typeof(string));
    }

    [Fact(DisplayName = "cost is numeric(5,2), not nullable")]
    public void Configure_ShouldConfigureCostColumn()
    {
        // Act
        var property = _membershipType.FindProperty(nameof(BowlerMembership.Cost))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("cost");
        property.GetPrecision().ShouldBe(5);
        property.GetScale().ShouldBe(2);
        property.IsNullable.ShouldBeFalse();
    }

    [Theory(DisplayName = "date columns use custom names and are not nullable")]
    [InlineData(nameof(BowlerMembership.StartDate), "start_date", TestDisplayName = "StartDate maps to start_date")]
    [InlineData(nameof(BowlerMembership.EndDate), "end_date", TestDisplayName = "EndDate maps to end_date")]
    public void Configure_ShouldConfigureDateColumns(string propertyName, string columnName)
    {
        // Act
        var property = _membershipType.FindProperty(propertyName)!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(columnName);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "bowler_id and start_date form an index")]
    public void Configure_ShouldConfigureBowlerIdStartDateIndex()
    {
        // Act
        var index = _membershipType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Select(p => p.Name)
                .SequenceEqual([BowlerConfiguration.ForeignKeyName, nameof(BowlerMembership.StartDate)]));

        // Assert
        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeFalse();
    }
}