namespace Neba.Website.Server.Tournaments.TeamEligibility;

/// <summary>One bowler found by a name search.</summary>
public sealed record BowlerSearchResultViewModel(string BowlerId, string Name, string? UsbcId)
{
    /// <summary>"Dave Smith (4821-09317)", or the name alone when there is no USBC ID.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(UsbcId) ? Name : Name + " (" + UsbcId + ")";
}