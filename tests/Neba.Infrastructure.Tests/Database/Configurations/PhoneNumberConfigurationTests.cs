using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Domain.Contact;
using Neba.Infrastructure.Database.Configurations;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Configurations;


[UnitTest]
[Component("PhoneNumber")]
public sealed class PhoneNumberConfigurationTests
{
    private readonly IEntityType _phoneNumberType;

    public PhoneNumberConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new TestDbContext(options);
        var owner = context.Model.FindEntityType(typeof(TestOwner))!;
        _phoneNumberType = owner.FindNavigation(nameof(TestOwner.PhoneNumbers))!.TargetEntityType;
    }

    [Fact(DisplayName = "phone_type is char(1), not nullable")]
    public void WithPhoneNumbers_ShouldConfigureTypeColumn()
    {
        var property = _phoneNumberType.FindProperty(nameof(PhoneNumber.Type))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("phone_type");
        property.GetMaxLength().ShouldBe(1);
        property.IsFixedLength().ShouldBe(true);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "phone_country_code is varchar(3), not nullable")]
    public void WithPhoneNumbers_ShouldConfigureCountryCodeColumn()
    {
        var property = _phoneNumberType.FindProperty(nameof(PhoneNumber.CountryCode))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("phone_country_code");
        property.GetMaxLength().ShouldBe(3);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "phone_number is varchar(15), not nullable")]
    public void WithPhoneNumbers_ShouldConfigureNumberColumn()
    {
        var property = _phoneNumberType.FindProperty(nameof(PhoneNumber.Number))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("phone_number");
        property.GetMaxLength().ShouldBe(15);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "phone_extension is varchar(10), nullable")]
    public void WithPhoneNumbers_ShouldConfigureExtensionColumn()
    {
        var property = _phoneNumberType.FindProperty(nameof(PhoneNumber.Extension))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("phone_extension");
        property.GetMaxLength().ShouldBe(10);
        property.IsNullable.ShouldBeTrue();
    }

    private sealed class TestOwner
    {
        public int Id { get; init; } = 5;

        public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = [];
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner =>
            {
                owner.HasKey(x => x.Id);
                owner.OwnsMany(x => x.PhoneNumbers, phones => phones.WithPhoneNumbers());
            });
    }
}