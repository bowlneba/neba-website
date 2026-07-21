using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class OilPatternErrors
{
    public static Error NameRequired
        => Error.Validation("OilPattern.Name.Required", "Name must not be empty.");

    public static Error LengthMustBePositive
        => Error.Validation("OilPattern.Length.MustBePositive", "Length must be greater than zero.");

    public static Error VolumeMustBePositive
        => Error.Validation("OilPattern.Volume.MustBePositive", "Volume must be greater than zero.");

    public static Error KegelIdAlreadyExists(Guid kegelId)
        => Error.Conflict(
            code: "OilPattern.KegelId.AlreadyExists",
            description: "A pattern with this Kegel ID already exists.",
            metadata: new Dictionary<string, object> { { "KegelId", kegelId } });
}