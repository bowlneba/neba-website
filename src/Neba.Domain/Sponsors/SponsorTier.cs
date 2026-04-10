using Ardalis.SmartEnum;

namespace Neba.Domain.Sponsors;

/// <summary>
/// Represents the different tiers of sponsorship.
/// </summary>
public sealed class SponsorTier
    : SmartEnum<SponsorTier>
{
    /// <summary>
    /// Represents the title sponsor tier.
    /// </summary>
    public static readonly SponsorTier TitleSponsor = new("Title Sponsor", 1);

    /// <summary>
    /// Represents the premium sponsor tier.
    /// </summary>
    public static readonly SponsorTier Premier = new(nameof(Premier), 2);

    /// <summary>
    /// Represents the standard sponsor tier.
    /// </summary>
    public static readonly SponsorTier Standard = new(nameof(Standard), 3);

    private SponsorTier(string name, int value)
        : base(name, value)
    { }
}