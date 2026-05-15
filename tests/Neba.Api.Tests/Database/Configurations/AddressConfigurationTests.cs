using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Address")]
public sealed class AddressConfigurationTests
{
    private readonly IComplexType _addressType;
    private readonly IComplexType _coordinatesType;

    public AddressConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new TestDbContext(options);
        var owner = context.Model.FindEntityType(typeof(TestOwner))!;
        var addressComplex = owner.FindComplexProperty(nameof(TestOwner.Address))!;
        _addressType = addressComplex.ComplexType;
        _coordinatesType = addressComplex.ComplexType.FindComplexProperty(nameof(Address.Coordinates))!.ComplexType;
    }

    [Fact(DisplayName = "street is varchar(100), not nullable")]
    public void HasAddress_ShouldConfigureStreetColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.Street))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("street");
        property.GetMaxLength().ShouldBe(100);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "unit is varchar(50), nullable")]
    public void HasAddress_ShouldConfigureUnitColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.Unit))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("unit");
        property.GetMaxLength().ShouldBe(50);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "city is varchar(50), not nullable")]
    public void HasAddress_ShouldConfigureCityColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.City))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("city");
        property.GetMaxLength().ShouldBe(50);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "region is char(2), not nullable")]
    public void HasAddress_ShouldConfigureRegionColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.Region))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("region");
        property.GetMaxLength().ShouldBe(2);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "country is char(2), not nullable")]
    public void HasAddress_ShouldConfigureCountryColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.Country))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("country");
        property.GetMaxLength().ShouldBe(2);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "postal_code is varchar(10), not nullable")]
    public void HasAddress_ShouldConfigurePostalCodeColumn()
    {
        var property = _addressType.FindProperty(nameof(Address.PostalCode))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("postal_code");
        property.GetMaxLength().ShouldBe(10);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "latitude maps to latitude column, not nullable")]
    public void HasAddress_ShouldConfigureLatitudeColumn()
    {
        var property = _coordinatesType.FindProperty(nameof(Coordinates.Latitude))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("latitude");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "longitude maps to longitude column, not nullable")]
    public void HasAddress_ShouldConfigureLongitudeColumn()
    {
        var property = _coordinatesType.FindProperty(nameof(Coordinates.Longitude))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("longitude");
        property.IsNullable.ShouldBeFalse();
    }

    private sealed class TestOwner
    {
        public int Id { get; init; } = 5;
        public Address Address { get; init; } = Address.Empty;
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner =>
            {
                owner.HasKey(x => x.Id);
                owner.HasAddress(x => x.Address);
            });
    }
}