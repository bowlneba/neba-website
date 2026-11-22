using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>What a Stage Override does.</summary>
public sealed class OverrideKind
    : SmartEnum<OverrideKind>
{
    /// <summary>Adds a bowler to the field.</summary>
    public static readonly OverrideKind Add = new(nameof(Add), 1);

    /// <summary>Clears a slot and bars the bowler from the Backfill.</summary>
    public static readonly OverrideKind Clear = new(nameof(Clear), 2);

    private OverrideKind(string name, int value)
        : base(name, value)
    { }
}