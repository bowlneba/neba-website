using Ardalis.SmartEnum;

namespace Neba.Domain.Sponsors;

/// <summary>
/// Represents the different categories of sponsorship.
/// </summary>
public sealed class SponsorCategory
    : SmartEnum<SponsorCategory>
{
    /// <summary>
    /// Represents the manufacturer sponsor category.
    /// </summary>
    public static readonly SponsorCategory Manufacturer = new(nameof(Manufacturer), 1);

    /// <summary>
    /// Represents the pro shop sponsor category.
    /// </summary>
    public static readonly SponsorCategory ProShop = new("Pro Shop", 2);

    /// <summary>
    /// Represents the bowling center sponsor category.
    /// </summary>
    public static readonly SponsorCategory BowlingCenter = new("Bowling Center", 3);

    /// <summary>
    /// Represents the financial services sponsor category.
    /// </summary>
    public static readonly SponsorCategory FinancialServices = new("Financial Services", 4);

    /// <summary>
    /// Represents the technology sponsor category.
    /// </summary>
    public static readonly SponsorCategory Technology = new(nameof(Technology), 5);

    /// <summary>
    /// Represents the media sponsor category.
    /// </summary>
    public static readonly SponsorCategory Media = new(nameof(Media), 6);

    /// <summary>
    /// Represents the individual sponsor category.
    /// </summary>
    public static readonly SponsorCategory Individual = new(nameof(Individual), 100);

    /// <summary>
    /// Represents the other sponsor category for sponsors that don't fit into the predefined categories.
    /// </summary>
    public static readonly SponsorCategory Other = new(nameof(Other), 999);

    private SponsorCategory(string name, int value)
        : base(name, value)
    {}
}