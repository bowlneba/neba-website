using ErrorOr;

namespace Neba.Api.Features.HallOfFame.Domain;

internal static class HallOfFameErrors
{
    public static Error InductionNotFound(HallOfFameId id)
        => Error.NotFound(
            code: "HallOfFame.NotFound",
            description: "Hall of Fame induction was not found.",
            metadata: new Dictionary<string, object>
            {
                { "HallOfFameId", id.ToString() }
            });

    public static Error PhotoNotUploaded
        => Error.Validation("HallOfFame.Photo.NotUploaded", "Photo must be a file uploaded through the photo upload endpoint.");

    public static Error NoPhoto
        => Error.Conflict("HallOfFame.Photo.Missing", "The induction has no photo to position.");

    public static Error FocalPointOutOfRange
        => Error.Validation("HallOfFame.Photo.FocalPointOutOfRange", "Focal point must be between 0 and 100 and zoom between 100 and 300.");
}