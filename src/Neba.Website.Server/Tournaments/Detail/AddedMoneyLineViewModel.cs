namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// One contributor line in the added money breakdown on the tournament detail page.
/// </summary>
public sealed record AddedMoneyLineViewModel
{
    /// <summary>
    /// Contributor display name; "NEBA" for NEBA's own contribution.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Amount contributed, in dollars.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// True for NEBA's own contribution, which always renders last.
    /// </summary>
    public bool IsNeba { get; init; }
}