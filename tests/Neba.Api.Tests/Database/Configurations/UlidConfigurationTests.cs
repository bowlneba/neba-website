// tests/Neba.Infrastructure.Tests/Database/Configurations/UlidConfigurationTests.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.Api.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Ulid")]
public sealed class UlidConfigurationTests
{
    [Fact(DisplayName = "default column name maps to domain_id")]
    public void IsUlid_ShouldConfigureDefaultColumnName()
    {
        var property = BuildProperty();

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
    }

    [Fact(DisplayName = "max length is 26")]
    public void IsUlid_ShouldSetMaxLength()
    {
        var property = BuildProperty();

        property.GetMaxLength().ShouldBe(26);
    }

    [Fact(DisplayName = "is fixed length char(26)")]
    public void IsUlid_ShouldBeFixedLength()
    {
        var property = BuildProperty();

        property.IsFixedLength().ShouldBe(true);
    }

    [Fact(DisplayName = "value is never generated")]
    public void IsUlid_ShouldConfigureValueGeneratedNever()
    {
        var property = BuildProperty();

        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
    }

    [Fact(DisplayName = "custom column name is applied")]
    public void IsUlid_ShouldApplyCustomColumnName()
    {
        var property = BuildProperty("custom_id");

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("custom_id");
    }

    [Fact(DisplayName = "value converter is configured")]
    public void IsUlid_ShouldConfigureValueConverter()
    {
        var property = BuildProperty();

        property.GetValueConverter().ShouldBeOfType<UlidTypedIdConverter<TestId>>();
    }

    private static IProperty BuildProperty(string columnName = "domain_id")
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new TestDbContext(options, columnName);
        return context.Model.FindEntityType(typeof(TestOwner))!
            .FindProperty(nameof(TestOwner.Id))!;
    }

    private readonly struct TestId
    {
        public string Value { get; }
        public TestId(string value) => Value = value;
        public override string ToString() => Value;
    }

    private sealed class TestOwner
    {
        public TestId Id { get; init; } = new(Ulid.NewUlid().ToString());
    }

    private sealed class TestDbContext(
        DbContextOptions options,
        string columnName = "domain_id")
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner =>
            {
                owner.HasKey(x => x.Id);
                owner.Property(x => x.Id).IsUlid<TestId, UlidTypedIdConverter<TestId>>(columnName);
            });
    }
}