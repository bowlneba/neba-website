using FastEndpoints;

using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

namespace Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;

internal sealed class EditHallOfFamePhotoSummary : Summary<EditHallOfFamePhotoEndpoint>
{
    public EditHallOfFamePhotoSummary()
    {
        Summary = "Sets, replaces, or removes an inductee's photo and its framing.";
        Description = "Sets the induction's photo to a previously uploaded file with a focal point and zoom, or removes the photo when Photo and the framing are all null. Sending a null Photo with a focal point and zoom keeps the current photo and changes only its framing. Requires the HallOfFame.EditHallOfFamePhoto permission.";

        ExampleRequest = new EditHallOfFamePhotoRequest
        {
            Id = "01JZ8K3M4N5P6Q7R8S9T0V1W2X",
            Input = new EditHallOfFamePhotoInput
            {
                Photo = new HallOfFamePhotoInput
                {
                    Container = "bowlneba-public",
                    Path = "hall-of-fame/photo/01JZ8K3M4N5P6Q7R8S9T0V1W2X.jpg",
                    ContentType = "image/jpeg",
                    SizeInBytes = 214_533
                },
                FocalX = 42,
                FocalY = 28,
                Zoom = 150
            }
        };

        Response(204, "Hall of Fame photo updated.");
        Response(400, "Id, the photo fields, or the framing failed structural validation.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the HallOfFame.EditHallOfFamePhoto permission.");
        Response(404, "No induction exists with the given Id.");
        Response(409, "The framing was sent without a photo, and the induction has no photo.");
        Response(422, "The photo is not a file uploaded through the photo upload endpoint.");
    }
}
