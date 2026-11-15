using System.Text.Json.Serialization;

namespace Neba.Api.Contracts.Memberships;

/// <summary>
/// Data describing how often a bowler may buy a membership type. Evaluation lives in the API.
/// </summary>
public sealed record RepeatPolicy
{
    /// <summary>
    /// Which rule applies.
    /// </summary>
    public RepeatPolicyKind Kind { get; }

    /// <summary>
    /// Minimum days since the last membership's start date. Set only for <see cref="RepeatPolicyKind.MinimumDaysSinceLast"/>.
    /// </summary>
    public int? Days { get; }

    [JsonConstructor]
    private RepeatPolicy(RepeatPolicyKind kind, int? days)
        => (Kind, Days) = (kind, days);

    /// <summary>
    /// No limit on repeat purchases.
    /// </summary>
    public static RepeatPolicy Unlimited { get; } = new(RepeatPolicyKind.Unlimited, null);

    /// <summary>
    /// The bowler may hold the type once, ever.
    /// </summary>
    public static RepeatPolicy OncePerLifetime { get; } = new(RepeatPolicyKind.OncePerLifetime, null);

    /// <summary>
    /// Requires at least <paramref name="days"/> days since the start of the bowler's latest membership.
    /// </summary>
    /// <param name="days">Minimum days; must be greater than zero.</param>
    public static RepeatPolicy MinimumDaysSinceLast(int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        return new RepeatPolicy(RepeatPolicyKind.MinimumDaysSinceLast, days);
    }

    /// <summary>
    /// Converts the legacy <c>Frequency</c> value: 0 is unlimited, -1 is once per lifetime, n &gt; 0 is n days.
    /// </summary>
    /// <param name="frequency">The legacy value.</param>
    public static RepeatPolicy FromLegacyFrequency(int frequency)
        => frequency switch
        {
            0 => Unlimited,
            -1 => OncePerLifetime,
            > 0 => MinimumDaysSinceLast(frequency),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Legacy frequency must be -1, 0, or greater than 0.")
        };
}