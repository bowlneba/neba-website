using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Uploads;

[UnitTest]
[Component("Uploads")]
public sealed class StoredFileClaimTests
{
    [Fact(DisplayName = "Resolve returns null when the file is null")]
    public void Resolve_ShouldReturnNull_WhenFileIsNull()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create();

        // Act
        var result = StoredFileClaim.Resolve(null, currentFile);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Resolve returns the stored file when the file names the same file with different metadata")]
    public void Resolve_ShouldReturnCurrentFile_WhenFileNamesSameFile()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 100);
        var file = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 999_999);

        // Act
        var result = StoredFileClaim.Resolve(file, currentFile);

        // Assert
        result.ShouldBeSameAs(currentFile);
    }

    [Fact(DisplayName = "Resolve returns the supplied file when the entity has no file")]
    public void Resolve_ShouldReturnFile_WhenCurrentFileIsNull()
    {
        // Arrange
        var file = StoredFileFactory.Create();

        // Act
        var result = StoredFileClaim.Resolve(file, null);

        // Assert
        result.ShouldBeSameAs(file);
    }

    [Fact(DisplayName = "Resolve returns the supplied file when the path differs")]
    public void Resolve_ShouldReturnFile_WhenPathDiffers()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create(path: "old.png");
        var file = StoredFileFactory.Create(path: "new.png");

        // Act
        var result = StoredFileClaim.Resolve(file, currentFile);

        // Assert
        result.ShouldBeSameAs(file);
    }

    [Fact(DisplayName = "Resolve returns the supplied file when the container differs")]
    public void Resolve_ShouldReturnFile_WhenContainerDiffers()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create(container: "container-a");
        var file = StoredFileFactory.Create(container: "container-b");

        // Act
        var result = StoredFileClaim.Resolve(file, currentFile);

        // Assert
        result.ShouldBeSameAs(file);
    }

    [Fact(DisplayName = "IsSameFile returns true when both files are null")]
    public void IsSameFile_ShouldReturnTrue_WhenBothNull()
    {
        // Arrange & Act
        var result = StoredFileClaim.IsSameFile(null, null);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsSameFile returns false when the file is null and a current file exists")]
    public void IsSameFile_ShouldReturnFalse_WhenFileNullAndCurrentFileExists()
    {
        // Arrange
        var currentFile = StoredFileFactory.Create();

        // Act
        var result = StoredFileClaim.IsSameFile(null, currentFile);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns false when a file is set and no current file exists")]
    public void IsSameFile_ShouldReturnFalse_WhenCurrentFileNull()
    {
        // Arrange
        var file = StoredFileFactory.Create();

        // Act
        var result = StoredFileClaim.IsSameFile(file, null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns true when container and path match and metadata differs")]
    public void IsSameFile_ShouldReturnTrue_WhenContainerAndPathMatch()
    {
        // Arrange
        var file = StoredFileFactory.Create(contentType: "image/png", sizeInBytes: 1);
        var currentFile = StoredFileFactory.Create(contentType: "image/jpeg", sizeInBytes: 2);

        // Act
        var result = StoredFileClaim.IsSameFile(file, currentFile);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsSameFile returns false when the path differs")]
    public void IsSameFile_ShouldReturnFalse_WhenPathDiffers()
    {
        // Arrange
        var file = StoredFileFactory.Create(path: "a.png");
        var currentFile = StoredFileFactory.Create(path: "b.png");

        // Act
        var result = StoredFileClaim.IsSameFile(file, currentFile);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameFile returns false when the container differs")]
    public void IsSameFile_ShouldReturnFalse_WhenContainerDiffers()
    {
        // Arrange
        var file = StoredFileFactory.Create(container: "container-a");
        var currentFile = StoredFileFactory.Create(container: "container-b");

        // Act
        var result = StoredFileClaim.IsSameFile(file, currentFile);

        // Assert
        result.ShouldBeFalse();
    }
}