namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>Display category for a NEBA tournament format.</summary>
public enum TournamentType
{
    /// <summary>Individual competition.</summary>
    Singles,

    /// <summary>Two-person team competition.</summary>
    Doubles,

    /// <summary>Three-person team competition.</summary>
    Trios,

    /// <summary>Multi-person team competition (Baker or larger).</summary>
    Team,

    /// <summary>Restricted to bowlers aged 50 or older.</summary>
    Senior,

    /// <summary>Restricted to female bowlers.</summary>
    Women,

    /// <summary>Invitational, Masters, Tournament of Champions, or similar.</summary>
    SpecialEvent,
}