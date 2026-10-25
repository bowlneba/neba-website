using System.Globalization;

namespace Neba.Website.Server.Components;

/// <summary>
/// Where a photo is framed: the CSS <c>object-position</c> percents (X, Y, 0 to 100) and a zoom in percent
/// (100 is none, 300 is the most). The zoom scales the photo about the same point.
/// </summary>
/// <param name="X">Percent from the left edge, 0 to 100.</param>
/// <param name="Y">Percent from the top edge, 0 to 100.</param>
/// <param name="Zoom">Zoom in percent, 100 to 300.</param>
public readonly record struct FocalPoint(int X, int Y, int Zoom)
{
    /// <summary>
    /// The middle of the photo, no zoom.
    /// </summary>
    public static FocalPoint Default => new(50, 50, 100);

    /// <summary>
    /// Inline style for an <c>&lt;img&gt;</c> with <c>object-fit: cover</c> inside an <c>overflow: hidden</c> shape.
    /// </summary>
    public string ToImageStyle()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"object-position:{X}% {Y}%;transform-origin:{X}% {Y}%;transform:scale({Zoom / 100.0})");
}

/// <summary>
/// The shape a photo is shown in.
/// </summary>
public enum FocalPointShape
{
    /// <summary>
    /// A circle, as in the Hall of Fame avatars.
    /// </summary>
    Circle,

    /// <summary>
    /// A rectangle with its own aspect ratio.
    /// </summary>
    Rectangle
}