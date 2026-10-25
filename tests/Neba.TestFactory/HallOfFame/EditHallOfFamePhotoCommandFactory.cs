using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.Storage.Domain;

namespace Neba.TestFactory.HallOfFame;

internal static class EditHallOfFamePhotoCommandFactory
{
    public static EditHallOfFamePhotoCommand Create(
        HallOfFameId? id = null,
        StoredFile? photo = null,
        int? focalX = null,
        int? focalY = null,
        int? zoom = null)
        => new()
        {
            HallOfFameId = id ?? HallOfFameId.New(),
            Photo = photo,
            FocalX = focalX,
            FocalY = focalY,
            Zoom = zoom
        };
}