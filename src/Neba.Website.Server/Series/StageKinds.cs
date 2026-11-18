using Neba.Api.Contracts.Series;

namespace Neba.Website.Server.Series;

/// <summary>Stage kind names as the API returns them. The API takes the matching SmartEnum value on commands.</summary>
internal static class StageKinds
{
    public const string Qualifying = "Qualifying";
    public const string Survivor = "Survivor";
    public const string Placement = "Placement";

    public static readonly IReadOnlyList<string> All = [Qualifying, Survivor, Placement];

    /// <summary>
    /// The API's SmartEnum value for a kind name, as commands send it. An unknown name maps to 0, which is
    /// no kind, so the API rejects it with a validation error instead of the save handler throwing.
    /// </summary>
    public static int ToValue(string kind)
        => kind switch
        {
            Qualifying => StageKindValues.Qualifying,
            Survivor => StageKindValues.Survivor,
            Placement => StageKindValues.Placement,
            _ => 0,
        };
}