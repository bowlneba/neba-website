using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Processing state of a Stage. Any non-pending Stage locks the Series.</summary>
public sealed class StageStatus
    : SmartEnum<StageStatus>
{
    /// <summary>Not yet processed. Every Stage is pending until phase 2.</summary>
    public static readonly StageStatus Pending = new(nameof(Pending), 0);

    /// <summary>Advancement has been computed but not announced.</summary>
    public static readonly StageStatus Computed = new(nameof(Computed), 1);

    /// <summary>Advancement has been confirmed and announced to bowlers.</summary>
    public static readonly StageStatus Announced = new(nameof(Announced), 2);

    private StageStatus(string name, int value)
        : base(name, value)
    { }
}
