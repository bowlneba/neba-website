using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A bowler in the Series Field, and where the bowler came from.</summary>
[SuppressMessage("Major Code Smell", "S3453:Classes should not have only \"private\" constructors",
    Justification = "EF materializes it through the private constructor. The internal Create factory arrives with the advancement domain logic.")]
public sealed class SeriesFieldEntry
{
    /// <summary>Unique ID.</summary>
    public required SeriesFieldEntryId Id { get; init; }

    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigation for the real foreign key (same pattern as TournamentResult.Bowler).
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>The Qualifying Stage that produced the entry. Null means the Backfill.</summary>
    public SeriesStageId? SourceStageId { get; private set; }

    /// <summary>Place in the source Stage's tournament. Null for a Backfill entry.</summary>
    public int? Place { get; private set; }

    /// <summary>Open Bowler of the Year points that earned a Backfill entry. Null for a Stage entry.</summary>
    public int? BackfillPoints { get; private set; }

    /// <summary>True when an Add override put the bowler in the field.</summary>
    public bool ViaOverride { get; private set; }

    private SeriesFieldEntry()
    { }
}
