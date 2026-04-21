namespace Neba.Website.Server.Tournaments;

/// <summary>Current registration state of a tournament.</summary>
public enum RegistrationStatus
{
    /// <summary>Registration is currently open.</summary>
    Open,

    /// <summary>Registration deadline is approaching.</summary>
    ClosingSoon,

    /// <summary>Registration has closed.</summary>
    Closed,

    /// <summary>Maximum entries have been reached.</summary>
    Full,

    /// <summary>The tournament has concluded.</summary>
    Completed,
}
