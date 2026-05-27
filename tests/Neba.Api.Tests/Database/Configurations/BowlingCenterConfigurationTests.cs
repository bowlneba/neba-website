using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contacts.Domain;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("BowlingCenters")]
public sealed class BowlingCenterConfigurationTests
{
    private readonly IEntityType _bowlingCenterType;
    private readonly IEntityType _phoneNumberType;
    private readonly IEntityType _laneRangeType;

    public BowlingCenterConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _bowlingCenterType = context.Model.FindEntityType(typeof(BowlingCenter))!;
        _phoneNumberType = _bowlingCenterType
            .FindNavigation(nameof(BowlingCenter.PhoneNumbers))!.TargetEntityType;
        var laneConfigType = _bowlingCenterType
            .FindNavigation(nameof(BowlingCenter.Lanes))!.TargetEntityType;
        _laneRangeType = laneConfigType
            .FindNavigation(nameof(LaneConfiguration.Ranges))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to bowling_centers table in app schema")]
    public void Configure_ShouldMapToBowlingCentersTable()
    {
        // Act & Assert
        _bowlingCenterType.GetTableName().ShouldBe("bowling_centers");
        _bowlingCenterType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "OpenCentersFilter query filter is configured")]
    public void Configure_ShouldConfigureOpenCentersQueryFilter()
    {
        // Act & Assert
        _bowlingCenterType.GetDeclaredQueryFilters()
            .ShouldContain(f => f.Key == BowlingCenterConfiguration.QueryFilters.OpenCentersFilter);
    }

    [Fact(DisplayName = "certification_number is varchar(6), not nullable")]
    public void Configure_ShouldConfigureCertificationNumberColumn()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.CertificationNumber))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("certification_number");
        property.GetMaxLength().ShouldBe(6);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "certification_number has a value converter")]
    public void Configure_ShouldConfigureCertificationNumberConverter()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.CertificationNumber))!;

        // Assert
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Fact(DisplayName = "certification_number is an alternate key")]
    public void Configure_ShouldConfigureCertificationNumberAsAlternateKey()
    {
        // Act
        var alternateKey = _bowlingCenterType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(BowlingCenter.CertificationNumber)));

        // Assert
        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "name is varchar(127), not nullable")]
    public void Configure_ShouldConfigureNameColumn()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.Name))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("name");
        property.GetMaxLength().ShouldBe(127);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "status maps to status column, not nullable")]
    public void Configure_ShouldConfigureStatusColumn()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.Status))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("status");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "website is varchar(255), nullable")]
    public void Configure_ShouldConfigureWebsiteColumn()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.Website))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("website");
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }


    [Fact(DisplayName = "website_id is never generated")]
    public void Configure_ShouldConfigureWebsiteIdValueGeneratedNever()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.WebsiteId))!;

        // Assert
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "website_id has a unique index")]
    public void Configure_ShouldConfigureWebsiteIdUniqueIndex()
    {
        // Act
        var index = _bowlingCenterType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(BowlingCenter.WebsiteId)));

        // Assert
        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
    }

    [Fact(DisplayName = "legacy_id is never generated")]
    public void Configure_ShouldConfigureLegacyIdValueGeneratedNever()
    {
        // Act
        var property = _bowlingCenterType.FindProperty(nameof(BowlingCenter.LegacyId))!;

        // Assert
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "legacy_id has a unique index")]
    public void Configure_ShouldConfigureLegacyIdUniqueIndex()
    {
        // Act
        var index = _bowlingCenterType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(BowlingCenter.LegacyId)));

        // Assert
        index.ShouldNotBeNull();
        index.IsUnique.ShouldBeTrue();
    }

    [Fact(DisplayName = "phone numbers map to bowling_center_phone_numbers table in app schema")]
    public void Configure_ShouldMapPhoneNumbersToTable()
    {
        // Act & Assert
        _phoneNumberType.GetTableName().ShouldBe("bowling_center_phone_numbers");
        _phoneNumberType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "phone numbers have bowling_center_id foreign key")]
    public void Configure_ShouldConfigurePhoneNumberForeignKey()
    {
        // Act
        var fk = _phoneNumberType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == BowlingCenterConfiguration.ForeignKeyName));

        // Assert
        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "phone numbers have composite PK of bowling_center_id and Type")]
    public void Configure_ShouldConfigurePhoneNumberCompositeKey()
    {
        // Act
        var pkPropertyNames = _phoneNumberType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ToList();

        // Assert
        pkPropertyNames.ShouldContain(BowlingCenterConfiguration.ForeignKeyName);
        pkPropertyNames.ShouldContain(nameof(PhoneNumber.Type));
    }

    [Fact(DisplayName = "lane ranges map to bowling_center_lanes table in app schema")]
    public void Configure_ShouldMapLaneRangesToTable()
    {
        // Act & Assert
        _laneRangeType.GetTableName().ShouldBe("bowling_center_lanes");
        _laneRangeType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "lane ranges have bowling_center_id foreign key")]
    public void Configure_ShouldConfigureLaneRangeForeignKey()
    {
        // Act
        var fk = _laneRangeType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == BowlingCenterConfiguration.ForeignKeyName));

        // Assert
        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "lane ranges have composite PK of bowling_center_id and StartLane")]
    public void Configure_ShouldConfigureLaneRangeCompositeKey()
    {
        // Act
        var pkPropertyNames = _laneRangeType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ToList();

        // Assert
        pkPropertyNames.ShouldContain(BowlingCenterConfiguration.ForeignKeyName);
        pkPropertyNames.ShouldContain(nameof(LaneRange.StartLane));
    }

    [Fact(DisplayName = "start_lane is not nullable, never generated")]
    public void Configure_ShouldConfigureStartLaneColumn()
    {
        // Act
        var property = _laneRangeType.FindProperty(nameof(LaneRange.StartLane))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("start_lane");
        property.IsNullable.ShouldBeFalse();
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "end_lane is not nullable")]
    public void Configure_ShouldConfigureEndLaneColumn()
    {
        // Act
        var property = _laneRangeType.FindProperty(nameof(LaneRange.EndLane))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("end_lane");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "pin_fall_type is char(2), not nullable")]
    public void Configure_ShouldConfigurePinFallTypeColumn()
    {
        // Act
        var property = _laneRangeType.FindProperty(nameof(LaneRange.PinFallType))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("pin_fall_type");
        property.GetMaxLength().ShouldBe(2);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }
}