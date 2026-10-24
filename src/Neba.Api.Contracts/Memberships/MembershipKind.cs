namespace Neba.Api.Contracts.Memberships;

/// <summary>
/// How long a membership lasts.
/// </summary>
public enum MembershipKind
{
    /// <summary>
    /// Valid for a fixed number of days from the start date.
    /// </summary>
    Temporary = 0,

    /// <summary>
    /// Valid through the end of the calendar year, rolling to the next year late in the current one.
    /// </summary>
    Yearly = 1,
}
