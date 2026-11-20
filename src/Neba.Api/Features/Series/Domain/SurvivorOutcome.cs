using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Survivor Round result. Owned by a Survivor Stage.</summary>
[SuppressMessage("Major Code Smell", "S3453:Classes should not have only \"private\" constructors",
    Justification = "EF materializes it through the private constructor. The internal Create factory arrives with the advancement domain logic.")]
public sealed class SurvivorOutcome
{
    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>The bowler's Block Total, or null when the bowler has no full block.</summary>
    public int? BlockTotal { get; private set; }

    /// <summary>True when the bowler advanced.</summary>
    public bool Advanced { get; private set; }

    private SurvivorOutcome()
    { }
}
