namespace Neba.Website.Server.Sponsors;

/// <summary>
/// View model representing a tournament sponsored by a sponsor, for display on the sponsor detail page.
/// </summary>
public sealed record SponsorDetailTournamentViewModel
{
    /// <summary>
    /// Unique identifier of the tournament.
    /// </summary>
    public required string TournamentId { get; init; }

    /// <summary>
    /// Display name of the tournament.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Start date of the tournament.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// End date of the tournament.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Indicates whether this sponsor is the tournament's title sponsor.
    /// </summary>
    public required bool TitleSponsor { get; init; }
}