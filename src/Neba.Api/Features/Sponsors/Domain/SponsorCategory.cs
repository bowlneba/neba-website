using System.Text.Json.Serialization;

using Ardalis.SmartEnum;

using Neba.Api.Domain;

namespace Neba.Api.Features.Sponsors.Domain;

/// <summary>
/// Represents the different categories of sponsorship.
/// </summary>
/// <remarks>
/// Values are powers of two so they can be combined as bit flags if multi-category
/// support is added in the future.
/// </remarks>
public sealed class SponsorCategory
    : SmartEnum<SponsorCategory>
{
    /// <summary>
    /// Represents the other sponsor category for sponsors that don't fit into the predefined categories.
    /// </summary>
    public static readonly SponsorCategory Other = new(nameof(Other), 1);

    /// <summary>
    /// Represents the manufacturer sponsor category.
    /// </summary>
    public static readonly SponsorCategory Manufacturer = new(nameof(Manufacturer), 1 << 1);

    /// <summary>
    /// Represents the pro shop sponsor category.
    /// </summary>
    public static readonly SponsorCategory ProShop = new("Pro Shop", 1 << 2);

    /// <summary>
    /// Represents the bowling center sponsor category.
    /// </summary>
    public static readonly SponsorCategory BowlingCenter = new("Bowling Center", 1 << 3);

    /// <summary>
    /// Represents the financial services sponsor category.
    /// </summary>
    public static readonly SponsorCategory FinancialServices = new("Financial Services", 1 << 4);

    /// <summary>
    /// Represents the technology sponsor category.
    /// </summary>
    public static readonly SponsorCategory Technology = new(nameof(Technology), 1 << 5);

    /// <summary>
    /// Represents the media sponsor category.
    /// </summary>
    public static readonly SponsorCategory Media = new(nameof(Media), 1 << 6);

    /// <summary>
    /// Represents the individual sponsor category.
    /// </summary>
    public static readonly SponsorCategory Individual = new(nameof(Individual), 1 << 7);

    private SponsorCategory(string name, int value)
        : base(name, value)
    { }
}