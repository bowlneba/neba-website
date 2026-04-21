namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>Eligibility restriction for a NEBA tournament entry.</summary>
public enum TournamentEligibility
{
    /// <summary>Open to all eligible NEBA members.</summary>
    Open,

    /// <summary>Restricted to bowlers aged 50 or older.</summary>
    Senior50Plus,

    /// <summary>Restricted to female bowlers.</summary>
    Women,

    /// <summary>Restricted to bowlers who have not previously won a NEBA title.</summary>
    NonChampions,

    /// <summary>Restricted to bowlers who have previously won a NEBA title.</summary>
    Champions
}