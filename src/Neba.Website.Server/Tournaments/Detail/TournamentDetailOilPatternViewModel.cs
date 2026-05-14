namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>Oil pattern summary for display on the tournament detail page.</summary>
public sealed record TournamentDetailOilPatternViewModel
{
    /// <summary>Name of the pattern (e.g., "Kegel Broadway").</summary>
    public required string Name { get; init; }

    /// <summary>Length of the pattern in feet.</summary>
    public required int Length { get; init; }

    /// <summary>Volume of oil applied in milliliters.</summary>
    public required decimal Volume { get; init; }

    /// <summary>Ratio of inner boards to left outside boards.</summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>Ratio of inner boards to right outside boards.</summary>
    public required decimal RightRatio { get; init; }

    /// <summary>Optional GUID identifying this pattern in the Kegel public pattern library.</summary>
    public Guid? KegelId { get; init; }

    /// <summary>Tournament rounds that use this pattern.</summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];

    /// <summary>Name and length formatted for display chips.</summary>
    public string Display =>
        Name + " · " + Length.ToString(System.Globalization.CultureInfo.CurrentCulture) + " ft";
}