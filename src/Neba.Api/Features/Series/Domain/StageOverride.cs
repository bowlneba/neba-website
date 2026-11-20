using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>An admin change to a Qualifying Stage's advancers. Survives every recompute until removed.</summary>
public sealed class StageOverride
{
    internal const int ReasonMaxLength = 500;

    /// <summary>Unique ID.</summary>
    public required StageOverrideId Id { get; init; }

    /// <summary>The bowler added or cleared.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>Add or Clear.</summary>
    public OverrideKind Kind { get; private set; } = OverrideKind.Add;

    /// <summary>Why the admin made the change. Up to 500 characters.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The admin's user ID, not a name. No foreign key: it lives in the security context.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>When the admin made the change (offset 0).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>False when the override's target changed and it no longer takes effect.</summary>
    public bool Applied { get; internal set; }

    private StageOverride()
    { }

    internal static ErrorOr<StageOverride> Create(
        OverrideKind kind, BowlerId bowlerId, string reason, string createdBy, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(kind);

        if (string.IsNullOrWhiteSpace(reason))
        {
            return SeriesAdvancementErrors.OverrideReasonRequired;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            return SeriesAdvancementErrors.OverrideReasonTooLong(ReasonMaxLength);
        }

        return new StageOverride
        {
            Id = StageOverrideId.New(),
            BowlerId = bowlerId,
            Kind = kind,
            Reason = trimmed,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            Applied = true,
        };
    }
}