namespace Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;

/// <summary>
/// The photo change to apply to an induction.
/// </summary>
public sealed record EditHallOfFamePhotoInput
{
    /// <summary>
    /// The already-uploaded photo to use. <see langword="null"/> with no framing removes the current photo;
    /// <see langword="null"/> with framing keeps the current photo and changes the framing.
    /// </summary>
    public required HallOfFamePhotoInput? Photo { get; init; }

    /// <summary>
    /// Horizontal focal point, 0 to 100. Set with <see cref="FocalY"/> and <see cref="Zoom"/>, or omit all three.
    /// A new photo with all omitted uses the center and no zoom.
    /// </summary>
    public int? FocalX { get; init; }

    /// <summary>
    /// Vertical focal point, 0 to 100.
    /// </summary>
    public int? FocalY { get; init; }

    /// <summary>
    /// Zoom about the focal point, 100 to 300 percent (100 is no zoom).
    /// </summary>
    public int? Zoom { get; init; }
}
