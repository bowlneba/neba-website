using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Processing state of a Stage. Any non-pending Stage locks the Series.</summary>
public sealed class StageStatus
    : SmartEnum<StageStatus>
{
    /// <summary>Not yet processed. Every Stage starts pending.</summary>
    public static readonly StageStatus Pending = new(nameof(Pending), 0);

    /// <summary>Advancement has been computed. It can still change until it becomes Official.</summary>
    public static readonly StageStatus Computed = new(nameof(Computed), 1);

    /// <summary>The output is settled and no longer changes on its own. Only a confirmed Recompute resets it.</summary>
    public static readonly StageStatus Official = new(nameof(Official), 2);

    private StageStatus(string name, int value)
        : base(name, value)
    { }
}