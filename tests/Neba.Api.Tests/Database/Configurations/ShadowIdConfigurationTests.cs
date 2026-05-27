using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("ShadowId")]
public sealed class ShadowIdConfigurationTests
{
    [Fact(DisplayName = "default shadow property maps to id column")]
    public void ConfigureShadowId_ShouldConfigureDefaultColumnName()
    {
        // Act
        var property = BuildProperty();

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(ShadowIdConfiguration.DefaultColumnName);
    }

    [Fact(DisplayName = "default shadow property is the primary key")]
    public void ConfigureShadowId_ShouldSetAsPrimaryKey()
    {
        // Act
        var entityType = BuildEntityType();

        // Assert
        entityType.FindPrimaryKey()!.Properties.Single().Name.ShouldBe(ShadowIdConfiguration.DefaultPropertyName);
    }

    [Fact(DisplayName = "shadow property is generated on add")]
    public void ConfigureShadowId_ShouldConfigureValueGeneratedOnAdd()
    {
        // Act
        var property = BuildProperty();

        // Assert
        property.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
    }

    [Fact(DisplayName = "custom property name and column name are applied")]
    public void ConfigureShadowId_ShouldApplyCustomNames()
    {
        // Act
        var entityType = BuildEntityType("custom_prop", "custom_col");
        var property = entityType.FindProperty("custom_prop")!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("custom_col");
        entityType.FindPrimaryKey()!.Properties.Single().Name.ShouldBe("custom_prop");
    }

    private static IProperty BuildProperty() =>
        BuildEntityType().FindProperty(ShadowIdConfiguration.DefaultPropertyName)!;

    private static IEntityType BuildEntityType(
        string propertyName = ShadowIdConfiguration.DefaultPropertyName,
        string columnName = ShadowIdConfiguration.DefaultColumnName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new TestDbContext(options, propertyName, columnName);
        return context.Model.FindEntityType(typeof(TestOwner))!;
    }

    private sealed class TestOwner
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestDbContext(
        DbContextOptions options,
        string propertyName = ShadowIdConfiguration.DefaultPropertyName,
        string columnName = ShadowIdConfiguration.DefaultColumnName)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner => owner.ConfigureShadowId(propertyName, columnName));
    }
}