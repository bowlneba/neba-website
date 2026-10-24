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
    /// Valid through Dec 31, or through Dec 31 of the next year when bought on or after Nov 1.
    /// </summary>
    Yearly = 1,
}