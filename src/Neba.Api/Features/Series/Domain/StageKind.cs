using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A Stage's role in a Series. Values define the required order of kinds.</summary>
public sealed class StageKind
    : SmartEnum<StageKind>
{
    /// <summary>Top N finishers by final tournament position enter the Series Field.</summary>
    public static readonly StageKind Qualifying = new(nameof(Qualifying), 1);

    /// <summary>Series bowlers advance on total score over a designated squad.</summary>
    public static readonly StageKind Survivor = new(nameof(Survivor), 2);

    /// <summary>The final Stage. Ranks by tournament position. With a Finale, the top finishers go to it.</summary>
    public static readonly StageKind Placement = new(nameof(Placement), 3);

    private StageKind(string name, int value)
        : base(name, value)
    { }
}