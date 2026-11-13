namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// One line of the Hall of Fame point table: what earns points and how many.
/// </summary>
/// <param name="Group">The table section the rule is shown under.</param>
/// <param name="Label">The title or award name.</param>
/// <param name="Points">The points earned.</param>
internal sealed record HallOfFamePointRule(string Group, string Label, int Points);