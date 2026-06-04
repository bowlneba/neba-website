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
    /// URL to the Kegel pattern library entry; null when KegelId is not set.
    /// </summary>
#pragma warning disable S1075 // External partner URL — intentional hardcoded base
    public Uri? KegelLibraryUrl =>
        KegelId is { } id
            ? new Uri("https://patternlibrary.kegel.net/pattern/" + id.ToString(), UriKind.Absolute)
            : null;
#pragma warning restore S1075
}