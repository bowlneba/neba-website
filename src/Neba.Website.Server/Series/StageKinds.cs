namespace Neba.Website.Server.Series;

/// <summary>Stage kind names as the API sends them (SmartEnum names, both directions).</summary>
internal static class StageKinds
{
    public const string Qualifying = "Qualifying";
    public const string Survivor = "Survivor";
    public const string Placement = "Placement";

    public static readonly IReadOnlyList<string> All = [Qualifying, Survivor, Placement];
}