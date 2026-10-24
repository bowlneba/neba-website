using ErrorOr;

using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.HallOfFame.Domain;

[UnitTest]
[Component("HallOfFame")]
public sealed class HallOfFameInductionTests
{
    [Fact(DisplayName = "A new induction has no photo and no focal point")]
    public void Create_ShouldHaveNoPhotoOrFocalPoint_WhenCreatedWithDefaults()
    {
        // Arrange
        // Act
        var induction = HallOfFameInductionFactory.Create();

        // Assert
        induction.Photo.ShouldBeNull();
        induction.PhotoFocalPoint.ShouldBeNull();
    }

    [Fact(DisplayName = "SetPhoto should set the photo and its focal point and zoom together")]
    public void SetPhoto_ShouldSetPhotoAndFocalPoint_WhenValid()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create();
        var photo = StoredFileFactory.Create();

        // Act
        var result = induction.SetPhoto(photo, 31, 36, 150);

        // Assert
        result.IsError.ShouldBeFalse();
        induction.Photo.ShouldBe(photo);
        induction.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(31, 36, 150));
    }

    [Fact(DisplayName = "SetPhoto should replace an existing photo and focal point")]
    public void SetPhoto_ShouldReplaceExistingPhoto_WhenCalledAgain()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create(photo: StoredFileFactory.Create(path: "old.png"), photoFocalX: 10, photoFocalY: 10, photoZoom: 200);
        var replacement = StoredFileFactory.Create(path: "new.png");

        // Act
        var result = induction.SetPhoto(replacement, 60, 70, 100);

        // Assert
        result.IsError.ShouldBeFalse();
        induction.Photo.ShouldBe(replacement);
        induction.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(60, 70, 100));
    }

    [Fact(DisplayName = "SetPhoto should return a validation error and leave the induction unchanged when the focal point is out of range")]
    public void SetPhoto_ShouldReturnValidationErrorAndNotChange_WhenFocalPointOutOfRange()
    {
        // Arrange
        var original = StoredFileFactory.Create(path: "original.png");
        var induction = HallOfFameInductionFactory.Create(photo: original, photoFocalX: 20, photoFocalY: 30, photoZoom: 120);

        // Act
        var result = induction.SetPhoto(StoredFileFactory.Create(path: "other.png"), 101, 50, 100);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        induction.Photo.ShouldBe(original);
        induction.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(20, 30, 120));
    }

    [Fact(DisplayName = "SetPhoto should throw when the photo is null")]
    public void SetPhoto_ShouldThrow_WhenPhotoIsNull()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create();

        // Act
        void Act() => induction.SetPhoto(null!, 50, 50, 100);

        // Assert
        Should.Throw<ArgumentNullException>(Act);
    }

    [Fact(DisplayName = "ClearPhoto should remove the photo and its focal point")]
    public void ClearPhoto_ShouldClearPhotoAndFocalPoint_WhenPhotoExists()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create(photo: StoredFileFactory.Create());

        // Act
        induction.ClearPhoto();

        // Assert
        induction.Photo.ShouldBeNull();
        induction.PhotoFocalPoint.ShouldBeNull();
    }

    [Fact(DisplayName = "ClearPhoto should do nothing when there is no photo")]
    public void ClearPhoto_ShouldDoNothing_WhenNoPhoto()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create();

        // Act
        induction.ClearPhoto();

        // Assert
        induction.Photo.ShouldBeNull();
        induction.PhotoFocalPoint.ShouldBeNull();
    }

    [Fact(DisplayName = "SetFocalPoint should change the focal point and zoom and keep the photo")]
    public void SetFocalPoint_ShouldChangeFocalPointAndKeepPhoto_WhenPhotoExists()
    {
        // Arrange
        var photo = StoredFileFactory.Create();
        var induction = HallOfFameInductionFactory.Create(photo: photo);

        // Act
        var result = induction.SetFocalPoint(12, 88, 250);

        // Assert
        result.IsError.ShouldBeFalse();
        induction.Photo.ShouldBe(photo);
        induction.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(12, 88, 250));
    }

    [Fact(DisplayName = "SetFocalPoint should return a conflict when there is no photo")]
    public void SetFocalPoint_ShouldReturnConflict_WhenNoPhoto()
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create();

        // Act
        var result = induction.SetFocalPoint(50, 50, 100);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("HallOfFame.Photo.Missing");
        induction.PhotoFocalPoint.ShouldBeNull();
    }

    [Theory(DisplayName = "SetFocalPoint should return a validation error and leave the focal point unchanged when a value is out of range")]
    [InlineData(-1, 50, 100)]
    [InlineData(50, 101, 100)]
    [InlineData(50, 50, 301)]
    public void SetFocalPoint_ShouldReturnValidationErrorAndNotChange_WhenOutOfRange(int x, int y, int zoom)
    {
        // Arrange
        var induction = HallOfFameInductionFactory.Create(photo: StoredFileFactory.Create(), photoFocalX: 20, photoFocalY: 30, photoZoom: 120);

        // Act
        var result = induction.SetFocalPoint(x, y, zoom);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        induction.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(20, 30, 120));
    }
}
