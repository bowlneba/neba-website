namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>Oil pattern summary for display on the tournament detail page.</summary>
public sealed record TournamentDetailOilPatternViewModel
{
    /// <summary>Name of the pattern (e.g., "Kegel Broadway").</summary>
    public required string Name { get; init; }

    /// <summary>Length of the pattern in feet.</summary>
    public required int Length { get; init; }

    /// <summary>Tournament rounds that use this pattern.</summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];

    /// <summary>Name and length formatted for display chips.</summary>
    public string Display =>
        Name + " · " + Length.ToString(System.Globalization.CultureInfo.CurrentCulture) + " ft";
}