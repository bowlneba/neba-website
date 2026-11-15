using ErrorOr;

namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// The point of a photo kept in frame when the photo is cropped to a shape, and how far the photo is zoomed in on it.
/// X and Y are the CSS <c>object-position</c> percents; the zoom scales the photo about that same point.
/// </summary>
public sealed record PhotoFocalPoint
{
    /// <summary>
    /// The lowest allowed X or Y percent.
    /// </summary>
    public const int Min = 0;

    /// <summary>
    /// The highest allowed X or Y percent.
    /// </summary>
    public const int Max = 100;

    /// <summary>
    /// The lowest allowed zoom percent (no zoom).
    /// </summary>
    public const int MinZoom = 100;

    /// <summary>
    /// The highest allowed zoom percent.
    /// </summary>
    public const int MaxZoom = 300;

    /// <summary>
    /// The middle of the photo, no zoom.
    /// </summary>
    public static PhotoFocalPoint Default { get; } = new() { X = 50, Y = 50, Zoom = MinZoom };

    /// <summary>
    /// Percent from the left edge, 0 to 100.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Percent from the top edge, 0 to 100.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Zoom in percent, 100 (none) to 300.
    /// </summary>
    public int Zoom { get; init; }

    internal static ErrorOr<PhotoFocalPoint> Create(int x, int y, int zoom)
    {
        var inRange = x is >= Min and <= Max && y is >= Min and <= Max && zoom is >= MinZoom and <= MaxZoom;

        return inRange
            ? new PhotoFocalPoint { X = x, Y = y, Zoom = zoom }
            : HallOfFameErrors.FocalPointOutOfRange;
    }
}