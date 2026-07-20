namespace Neba.Website.Server.Sponsors;

/// <summary>
/// The sponsor category options shown in the Create and Edit Sponsor form dropdowns.
/// </summary>
internal static class SponsorCategoryOptions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Other",
        "Manufacturer",
        "Pro Shop",
        "Bowling Center",
        "Financial Services",
        "Technology",
        "Media",
        "Individual"
    ];
}
