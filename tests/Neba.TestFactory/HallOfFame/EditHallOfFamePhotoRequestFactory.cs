using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

namespace Neba.TestFactory.HallOfFame;

public static class EditHallOfFamePhotoRequestFactory
{
    public const string ValidId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    public static HallOfFamePhotoInput CreatePhoto(
        string? container = null,
        string? path = null,
        string? contentType = null,
        long? sizeInBytes = null)
        => new()
        {
            Container = container ?? "bowlneba-public",
            Path = path ?? "hall-of-fame/photo/abc.png",
            ContentType = contentType ?? "image/png",
            SizeInBytes = sizeInBytes ?? 2048
        };

    /// <summary>
    /// A request that sets a new photo with a focal point and zoom. Pass <paramref name="photo"/> as null and the
    /// framing as null to build a removal request.
    /// </summary>
    public static EditHallOfFamePhotoRequest Create(
        string? id = null,
        HallOfFamePhotoInput? photo = null,
        int? focalX = null,
        int? focalY = null,
        int? zoom = null)
        => new()
        {
            Id = id ?? ValidId,
            Input = new EditHallOfFamePhotoInput
            {
                Photo = photo,
                FocalX = focalX,
                FocalY = focalY,
                Zoom = zoom
            }
        };
}
