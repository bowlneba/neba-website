using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Placement Round position. Owned by the Series.</summary>
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

    internal static ErrorOr<SeriesPosition> Create(BowlerId bowlerId, int position, bool bowled, bool? isFinalist)
    {
        var created = new SeriesPosition { BowlerId = bowlerId };
        var result = created.Update(position, bowled, isFinalist);

        return result.IsError ? result.Errors : created;
    }

    internal ErrorOr<Updated> Update(int position, bool bowled, bool? isFinalist)
    {
        if (position <= 0)
        {
            return SeriesAdvancementErrors.PositionInvalid;
        }

        Position = position;
        Bowled = bowled;
        IsFinalist = isFinalist;

        return Result.Updated;
    }

    internal PositionDraft ToDraft()
        => new(BowlerId, Position, Bowled, IsFinalist);
}