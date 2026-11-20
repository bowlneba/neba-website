using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Placement Round position. Owned by the Series.</summary>
[SuppressMessage("Major Code Smell", "S3453:Classes should not have only \"private\" constructors",
    Justification = "EF materializes it through the private constructor. The internal Create factory arrives with the advancement domain logic.")]
public sealed class SeriesPosition
{
    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>1-based position. Ties break by bowler ID.</summary>
    public int Position { get; private set; }

    /// <summary>True when the bowler bowled the Placement Stage.</summary>
    public bool Bowled { get; private set; }

    /// <summary>True when the bowler is a Finalist. Null when the Series has no Finale.</summary>
    public bool? IsFinalist { get; private set; }

    private SeriesPosition()
    { }
}
