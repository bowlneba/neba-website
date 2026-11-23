using Neba.Api.Contracts.Series;

namespace Neba.Website.Server.Series;

/// <summary>Stage status names as the API returns them.</summary>
internal static class StageStatuses
{
    public const string Pending = "Pending";
    public const string Computed = "Computed";
    public const string Official = "Official";
}

/// <summary>Tournament status names the Series page reads.</summary>
internal static class TournamentStatuses
{
    public const string Cancelled = "Cancelled";
}

/// <summary>Override kind names as the API returns them. Commands take the matching value.</summary>
internal static class OverrideKinds
{
    public const string Add = "Add";
    public const string Clear = "Clear";

    /// <summary>The API's value for a kind name. An unknown name maps to 0, which the API rejects by name.</summary>
    public static int ToValue(string kind)
        => kind switch
        {
            Add => OverrideKindValues.Add,
            Clear => OverrideKindValues.Clear,
            _ => 0,
        };
}