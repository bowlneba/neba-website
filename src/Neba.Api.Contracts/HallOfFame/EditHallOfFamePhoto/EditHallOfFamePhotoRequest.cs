namespace Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

/// <summary>
/// Sets, replaces, or removes an inductee's photo and its framing.
/// </summary>
public sealed record EditHallOfFamePhotoRequest
{
    /// <summary>
    /// The ULID string identifying the induction.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The photo change to apply.
    /// </summary>
    public required EditHallOfFamePhotoInput Input { get; init; }
}
