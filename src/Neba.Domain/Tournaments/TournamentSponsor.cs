using Neba.Domain.Sponsors;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Represents the association between a tournament and its sponsor, including details about the sponsorship such as whether it is a title sponsorship and the amount of the sponsorship.
/// </summary>
public sealed class TournamentSponsor
{
    /// <summary>
    /// The unique identifier of the tournament associated with this sponsorship. This is a foreign key that links to the Tournament entity, allowing us to retrieve tournament details such as name and dates when needed. It is required to establish the relationship between the tournament and its sponsor in the database.
    /// </summary>
    internal Tournament Tournament { get; init; } = null!;

    /// <summary>
    /// The unique identifier of the sponsor associated with this tournament sponsorship. This is a foreign key that links to the Sponsor entity, allowing us to retrieve sponsor details such as name and contact information when needed. It is required to establish the relationship between the tournament and its sponsor in the database.
    /// </summary>
    public required SponsorId SponsorId { get; init; }

    /// <summary>
    /// The sponsor associated with this tournament sponsorship. This navigation property allows us to access the sponsor details from the sponsorship entity. It is marked as internal to restrict access to the domain layer, ensuring that external layers interact with tournaments and sponsors through their respective repositories and services rather than directly through this association entity.
    /// </summary>
    internal Sponsor Sponsor { get; init; } = null!;

    /// <summary>
    /// Indicates whether this sponsorship is a title sponsorship for the tournament. A title sponsorship typically involves the sponsor's name being prominently featured in the tournament's official name and marketing materials. This boolean property allows us to differentiate between title sponsors and other types of sponsors, which may have different levels of visibility and involvement in the tournament. It is required to capture the nature of the sponsorship relationship in the database.
    /// </summary>
    public required bool TitleSponsor { get; init; }

    /// <summary>
    /// The amount of the sponsorship provided by the sponsor for this tournament. This decimal property allows us to capture the financial contribution of the sponsor to the tournament, which can be used for reporting, analytics, and determining sponsorship tiers. It is required to quantify the sponsorship relationship in the database.
    /// </summary>
    public required decimal SponsorshipAmount { get; init; }
}