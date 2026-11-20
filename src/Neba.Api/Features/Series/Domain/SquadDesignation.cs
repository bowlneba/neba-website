using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>
/// The squad a Survivor Round uses. Described by date and start time, as bowlers refer to it;
/// resolved to a <see cref="Tournaments.Domain.SquadId"/> by advancement.
/// </summary>
public sealed record SquadDesignation
{
    /// <summary>The date the squad bowls.</summary>
    public DateOnly Date { get; init; }

    /// <summary>The squad's start time, local to the bowling center.</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>The resolved squad, or null while only described.</summary>
    public SquadId? SquadId { get; init; }

    /// <summary>True once resolved to a squad.</summary>
    public bool IsResolved
        => SquadId is not null;

    private SquadDesignation()
    { }

    internal static SquadDesignation Describe(DateOnly date, TimeOnly startTime)
        => new() { Date = date, StartTime = startTime };

    internal SquadDesignation Resolve(SquadId squadId)
        => this with { SquadId = squadId };

    internal SquadDesignation Unresolve()
        => this with { SquadId = null };
}