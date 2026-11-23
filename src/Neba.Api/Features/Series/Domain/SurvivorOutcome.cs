using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Survivor Round result. Owned by a Survivor Stage.</summary>
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

    internal static ErrorOr<SurvivorOutcome> Create(BowlerId bowlerId, int? blockTotal, bool advanced)
    {
        var created = new SurvivorOutcome { BowlerId = bowlerId };
        var result = created.Update(blockTotal, advanced);

        return result.IsError ? result.Errors : created;
    }

    internal ErrorOr<Updated> Update(int? blockTotal, bool advanced)
    {
        if (blockTotal is < 0 || (advanced && blockTotal is null))
        {
            return SeriesAdvancementErrors.OutcomeInvalid;
        }

        BlockTotal = blockTotal;
        Advanced = advanced;

        return Result.Updated;
    }

    internal SurvivorOutcomeDraft ToDraft()
        => new(BowlerId, BlockTotal, Advanced);
}