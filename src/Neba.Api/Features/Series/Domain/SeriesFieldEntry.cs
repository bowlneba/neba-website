using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A bowler in the Series Field, and where the bowler came from.</summary>
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

    /// <summary>True for a Backfill entry.</summary>
    public bool IsBackfill
        => SourceStageId is null;

    private SeriesFieldEntry()
    { }

    internal static ErrorOr<SeriesFieldEntry> Create(FieldEntryDraft draft)
    {
        var entry = new SeriesFieldEntry { Id = SeriesFieldEntryId.New(), BowlerId = draft.BowlerId };
        var result = entry.Set(draft);

        return result.IsError ? result.Errors : entry;
    }

    // The source Stage never changes: only the place, points and origin of the entry do.
    internal ErrorOr<Updated> Update(FieldEntryDraft draft)
        => Set(draft with { SourceStageId = SourceStageId });

    internal FieldEntryDraft ToDraft()
        => new(BowlerId, SourceStageId, Place, BackfillPoints, ViaOverride);

    private ErrorOr<Updated> Set(FieldEntryDraft draft)
    {
        var valid = draft.SourceStageId is null
            ? draft.BackfillPoints is > 0 && draft.Place is null && !draft.ViaOverride
            : draft.Place is > 0 && draft.BackfillPoints is null;

        if (!valid)
        {
            return SeriesAdvancementErrors.EntryInvalid;
        }

        SourceStageId = draft.SourceStageId;
        Place = draft.Place;
        BackfillPoints = draft.BackfillPoints;
        ViaOverride = draft.ViaOverride;

        return Result.Updated;
    }
}