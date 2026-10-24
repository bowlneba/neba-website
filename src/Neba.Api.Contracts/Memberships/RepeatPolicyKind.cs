namespace Neba.Api.Contracts.Memberships;

/// <summary>
/// How often a bowler may buy a given membership type.
/// </summary>
public enum RepeatPolicyKind
{
    /// <summary>
    /// No limit.
    /// </summary>
    Unlimited = 0,

    /// <summary>
    /// The bowler may hold this type once, ever.
    /// </summary>
    OncePerLifetime = 1,

    /// <summary>
    /// A minimum number of days must pass since the start of the bowler's latest membership of any type.
    /// </summary>
    MinimumDaysSinceLast = 2,
}
