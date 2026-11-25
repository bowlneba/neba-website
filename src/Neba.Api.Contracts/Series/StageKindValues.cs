namespace Neba.Api.Contracts.Series;

/// <summary>
/// The SmartEnum values of a tournament kind, as commands send them. The API's StageKind and the website share these.
/// </summary>
public static class StageKindValues
{
    /// <summary>The value of the Qualifying kind.</summary>
    public const int Qualifying = 1;

    /// <summary>The value of the Survivor kind.</summary>
    public const int Survivor = 2;

    /// <summary>The value of the Placement kind.</summary>
    public const int Placement = 3;
}