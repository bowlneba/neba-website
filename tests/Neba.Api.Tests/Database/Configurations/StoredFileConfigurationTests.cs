using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("StoredFile")]
public sealed class StoredFileConfigurationTests
{
    [Fact(DisplayName = "file_container is varchar(63), not nullable")]
    public void HasStoredFile_ShouldConfigureContainerColumn_WhenUsingDefaults()
    {
        var property = BuildStoredFileProperty(nameof(StoredFile.Container));

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("file_container");
        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "file_path is varchar(1023), not nullable")]
    public void HasStoredFile_ShouldConfigurePathColumn_WhenUsingDefaults()
    {
        var property = BuildStoredFileProperty(nameof(StoredFile.Path));

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("file_path");
        property.GetMaxLength().ShouldBe(1023);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "file_content_type is varchar(255), not nullable")]
    public void HasStoredFile_ShouldConfigureContentTypeColumn_WhenUsingDefaults()
    {
        var property = BuildStoredFileProperty(nameof(StoredFile.ContentType));

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("file_content_type");
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "file_size_in_bytes is not nullable")]
    public void HasStoredFile_ShouldConfigureSizeInBytesColumn_WhenUsingDefaults()
    {
        var property = BuildStoredFileProperty(nameof(StoredFile.SizeInBytes));

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("file_size_in_bytes");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "custom column names are applied")]
    public void HasStoredFile_ShouldApplyCustomColumnNames_WhenProvided()
    {
        var container = BuildStoredFileProperty(
            nameof(StoredFile.Container),
            containerColumnName: "blob_container",
            filePathColumnName: "blob_path",
            contentTypeColumnName: "blob_content_type",
            sizeInBytesColumnName: "blob_size");

        var path = BuildStoredFileProperty(
            nameof(StoredFile.Path),
            containerColumnName: "blob_container",
            filePathColumnName: "blob_path",
            contentTypeColumnName: "blob_content_type",
            sizeInBytesColumnName: "blob_size");

        var contentType = BuildStoredFileProperty(
            nameof(StoredFile.ContentType),
            containerColumnName: "blob_container",
            filePathColumnName: "blob_path",
            contentTypeColumnName: "blob_content_type",
            sizeInBytesColumnName: "blob_size");

        var sizeInBytes = BuildStoredFileProperty(
            nameof(StoredFile.SizeInBytes),
            containerColumnName: "blob_container",
            filePathColumnName: "blob_path",
            contentTypeColumnName: "blob_content_type",
            sizeInBytesColumnName: "blob_size");

        container.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("blob_container");
        path.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("blob_path");
        contentType.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("blob_content_type");
        sizeInBytes.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("blob_size");
    }

    private static IProperty BuildStoredFileProperty(
        string propertyName,
        string containerColumnName = "file_container",
        string filePathColumnName = "file_path",
        string contentTypeColumnName = "file_content_type",
        string sizeInBytesColumnName = "file_size_in_bytes")
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .Options;

        using var context = new TestDbContext(
            options,
            containerColumnName,
            filePathColumnName,
            contentTypeColumnName,
            sizeInBytesColumnName);

        var ownerType = context.Model.FindEntityType(typeof(TestOwner))!;
        var fileComplexType = ownerType.FindComplexProperty(nameof(TestOwner.File))!.ComplexType;

        return fileComplexType.FindProperty(propertyName)!;
    }

    private sealed class TestOwner
    {
        public int Id { get; init; } = 5;
        public StoredFile? File { get; init; } = new();
    }

    private sealed class TestDbContext(
        DbContextOptions options,
        string containerColumnName,
        string filePathColumnName,
        string contentTypeColumnName,
        string sizeInBytesColumnName)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestOwner>(owner =>
            {
                owner.HasKey(x => x.Id);
                owner.HasStoredFile(
                    x => x.File,
                    containerColumnName,
                    filePathColumnName,
                    contentTypeColumnName,
                    sizeInBytesColumnName);
            });
    }
}