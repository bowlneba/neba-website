using Ardalis.SmartEnum;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Represents the type of pinsetter mechanism used on a tenpin lane.
/// </summary>
public sealed class PinFallType
    : SmartEnum<PinFallType, string>
{
    /// <summary>
    /// Traditional free-fall pinsetters where pins fall naturally when struck. USBC-certified for sanctioned play.
    /// </summary>
    public static readonly PinFallType FreeFall = new("Free Fall", "FF");

    /// <summary>
    /// String pinsetters where pins are attached to strings and reset by a string-based mechanism. Certification and scoring rules may differ from FreeFall.
    /// </summary>
    public static readonly PinFallType StringPin = new("String Pin", "SP");

    private PinFallType(string name, string value)
        : base(name, value)
    { }
}