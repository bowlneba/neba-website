using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.Features.HallOfFame.EditHallOfFamePhoto;

[UnitTest]
[Component("HallOfFame")]
public sealed class EditHallOfFamePhotoRequestValidatorTests
{
    private readonly EditHallOfFamePhotoRequestValidator _validator = new();

    private static EditHallOfFamePhotoRequest ValidRequest()
        => EditHallOfFamePhotoRequestFactory.Create(photo: EditHallOfFamePhotoRequestFactory.CreatePhoto(), focalX: 50, focalY: 50, zoom: 100);

    private static EditHallOfFamePhotoRequest WithInput(Func<EditHallOfFamePhotoInput, EditHallOfFamePhotoInput> change)
    {
        var request = ValidRequest();
        return request with { Input = change(request.Input) };
    }

    [Fact(DisplayName = "Validate should succeed for a new photo with framing")]
    public void Validate_ShouldSucceed_ForNewPhotoWithFraming()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed for a new photo with no framing")]
    public void Validate_ShouldSucceed_ForNewPhotoWithNoFraming()
    {
        // Arrange
        var request = WithInput(input => input with { FocalX = null, FocalY = null, Zoom = null });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed when the photo and framing are all null (remove the photo)")]
    public void Validate_ShouldSucceed_WhenRemovingPhoto()
    {
        // Arrange
        var request = EditHallOfFamePhotoRequestFactory.Create();

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact(DisplayName = "Validate should succeed for framing with no photo (change the framing of the current photo)")]
    public void Validate_ShouldSucceed_ForFramingWithNoPhoto()
    {
        // Arrange
        var request = EditHallOfFamePhotoRequestFactory.Create(focalX: 0, focalY: 100, zoom: 300);

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "Validate should fail with IdRequired when Id is empty or blank")]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldFailWithIdRequired_WhenIdIsEmpty(string id)
    {
        // Arrange
        var request = ValidRequest() with { Id = id };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.IdRequired");
    }

    [Fact(DisplayName = "Validate should fail with IdInvalidLength when Id is not 26 characters")]
    public void Validate_ShouldFailWithIdInvalidLength_WhenIdIsNotUlidLength()
    {
        // Arrange
        var request = ValidRequest() with { Id = "too-short" };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.IdInvalidLength");
    }

#nullable disable
    [Fact(DisplayName = "Validate should fail with InputRequired when Input is null")]
    public void Validate_ShouldFailWithInputRequired_WhenInputIsNull()
    {
        // Arrange
        var request = ValidRequest() with { Input = null };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.InputRequired");
    }
#nullable enable

    [Theory(DisplayName = "Validate should fail with FocalXOutOfRange when FocalX is outside 0 to 100")]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_ShouldFailWithFocalXOutOfRange_WhenFocalXOutsideRange(int focalX)
    {
        // Arrange
        var request = WithInput(input => input with { FocalX = focalX });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.FocalXOutOfRange");
    }

    [Theory(DisplayName = "Validate should fail with FocalYOutOfRange when FocalY is outside 0 to 100")]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_ShouldFailWithFocalYOutOfRange_WhenFocalYOutsideRange(int focalY)
    {
        // Arrange
        var request = WithInput(input => input with { FocalY = focalY });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.FocalYOutOfRange");
    }

    [Theory(DisplayName = "Validate should fail with ZoomOutOfRange when Zoom is outside 100 to 300")]
    [InlineData(99)]
    [InlineData(301)]
    public void Validate_ShouldFailWithZoomOutOfRange_WhenZoomOutsideRange(int zoom)
    {
        // Arrange
        var request = WithInput(input => input with { Zoom = zoom });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.ZoomOutOfRange");
    }

    [Theory(DisplayName = "Validate should fail with FramingIncomplete when the focal point and zoom are not all set or all omitted")]
    [InlineData(50, null, null)]
    [InlineData(null, 50, null)]
    [InlineData(null, null, 100)]
    [InlineData(50, 50, null)]
    [InlineData(50, null, 100)]
    [InlineData(null, 50, 100)]
    public void Validate_ShouldFailWithFramingIncomplete_WhenFramingIsPartial(int? focalX, int? focalY, int? zoom)
    {
        // Arrange
        var request = WithInput(input => input with { FocalX = focalX, FocalY = focalY, Zoom = zoom });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == "EditHallOfFamePhotoRequest.FramingIncomplete");
    }

    [Theory(DisplayName = "Validate should fail when a required photo field is missing")]
    [InlineData("", "p", "image/png", 1L, "EditHallOfFamePhotoRequest.PhotoContainerRequired")]
    [InlineData("c", "", "image/png", 1L, "EditHallOfFamePhotoRequest.PhotoPathRequired")]
    [InlineData("c", "p", "", 1L, "EditHallOfFamePhotoRequest.PhotoContentTypeRequired")]
    [InlineData("c", "p", "image/png", 0L, "EditHallOfFamePhotoRequest.PhotoSizeInvalid")]
    public void Validate_ShouldFail_WhenPhotoFieldIsInvalid(string container, string path, string contentType, long size, string expectedCode)
    {
        // Arrange
        var request = WithInput(input => input with
        {
            Photo = EditHallOfFamePhotoRequestFactory.CreatePhoto(container, path, contentType, size)
        });

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorCode == expectedCode);
    }
}
