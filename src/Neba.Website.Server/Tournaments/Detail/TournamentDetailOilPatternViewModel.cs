namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// Oil pattern summary for display on the tournament detail page.
/// </summary>
public sealed record TournamentDetailOilPatternViewModel
{
    /// <summary>
    /// Name of the pattern (e.g., "Kegel Broadway").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Length of the pattern in feet.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// The oil volume applied, in milliliters.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// The forward (head) to reverse (tail) oil ratio on the pattern's left side.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// The forward (head) to reverse (tail) oil ratio on the pattern's right side.
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// Tournament rounds that use this pattern.
    /// </summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];

    /// <summary>
    /// Kegel pattern library ID; null when the pattern is not in the Kegel library.
    /// </summary>
    public Guid? KegelId { get; init; }

    /// <summary>
    /// Name and length formatted for display chips.
    /// </summary>
    public string Display =>
        Name + " · " + Length.ToString(System.Globalization.CultureInfo.CurrentCulture) + " ft";

    /// <summary>
    /// Volume and ratio formatted for the card's secondary line.
    /// </summary>
    public string RatioDisplay =>
        Volume.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " mL · ratio "
        + LeftRatio.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + "/"
        + RightRatio.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>
    /// URL to the Kegel pattern library entry; null when KegelId is not set.
    /// </summary>
#pragma warning disable S1075 // External partner URL — intentional hardcoded base
    public Uri? KegelLibraryUrl =>
        KegelId is { } id
            ? new Uri("https://patternlibrary.kegel.net/pattern/" + id, UriKind.Absolute)
            : null;
#pragma warning restore S1075
}