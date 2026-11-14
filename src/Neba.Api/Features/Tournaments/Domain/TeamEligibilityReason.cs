namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>One reason a team may not enter, with a stable code and the text shown to the user.</summary>
internal readonly record struct TeamEligibilityReason(string Code, string Message);
