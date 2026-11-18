namespace Neba.Website.Server.Series;

/// <summary>Stage kind names as the API returns them. The API takes the matching SmartEnum value on commands.</summary>
internal static class StageKinds
{
    public const string Qualifying = "Qualifying";
    public const string Survivor = "Survivor";
    public const string Placement = "Placement";

    public static readonly IReadOnlyList<string> All = [Qualifying, Survivor, Placement];

    /// <summary>The API's SmartEnum value for a kind name, as commands send it.</summary>
    public static int ToValue(string kind)
        => kind switch
        {
            Qualifying => 1,
            Survivor => 2,
            Placement => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Stage kind."),
        };
}