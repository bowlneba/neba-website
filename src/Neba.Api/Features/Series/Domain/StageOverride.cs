using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>An admin change to a Qualifying Stage's advancers. Survives every recompute until removed.</summary>
[SuppressMessage("Major Code Smell", "S3453:Classes should not have only \"private\" constructors",
    Justification = "EF materializes it through the private constructor. The internal Create factory arrives with the advancement domain logic.")]
public sealed class StageOverride
{
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
    public bool Applied { get; private set; }

    private StageOverride()
    { }
}
