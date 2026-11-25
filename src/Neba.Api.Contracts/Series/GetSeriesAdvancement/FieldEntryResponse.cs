namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>A bowler's place in the Series field.</summary>
public sealed record FieldEntryResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The tournament the bowler came from. Null for the Bowler of the Year point list.</summary>
    public string? SourceStageId { get; init; }

    /// <summary>The place in the source tournament.</summary>
    public int? Place { get; init; }

    /// <summary>The Bowler of the Year points. Null unless a Bowler of the Year point list entry.</summary>
    public int? BackfillPoints { get; init; }

    /// <summary>True when an override put the bowler in the field.</summary>
    public required bool ViaOverride { get; init; }
}