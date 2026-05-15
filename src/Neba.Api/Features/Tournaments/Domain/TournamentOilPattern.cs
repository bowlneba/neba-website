using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Associates a tournament with an oil pattern and the specific rounds that pattern was applied to.
/// </summary>
public sealed class TournamentOilPattern
{

    internal Tournament Tournament { get; init; } = null!;

    /// <summary>FK to the associated oil pattern.</summary>
    public OilPatternId OilPatternId { get; init; }

    internal OilPattern OilPattern { get; init; } = null!;

    private readonly List<TournamentRound> _tournamentRounds = [];

    /// <summary>Rounds of the tournament that used this oil pattern.</summary>
    public IReadOnlyCollection<TournamentRound> TournamentRounds
        => _tournamentRounds;

    internal static ErrorOr<TournamentOilPattern> Create(OilPatternId oilPatternId, IReadOnlyCollection<TournamentRound> tournamentRounds)
    {
        ArgumentNullException.ThrowIfNull(tournamentRounds);

        if (tournamentRounds.Count == 0)
        {
            return TournamentOilPatternErrors.NoTournamentRoundsSpecified();
        }

        // we throw an exception here since this is not expected behavior and would indicate a bug in the calling code (e.g. duplicate rounds in the input array), rather than a recoverable error condition that we would want to represent with an Error value
        if (tournamentRounds.Distinct().Count() != tournamentRounds.Count)
        {
            throw new ArgumentException("Duplicate tournament rounds are not allowed.", nameof(tournamentRounds));
        }

        var tournamentOilPattern = new TournamentOilPattern
        {
            OilPatternId = oilPatternId
        };

        foreach (var round in tournamentRounds)
        {
            var result = tournamentOilPattern.AddTournamentRound(round);
            if (result.IsError)
            {
                return result.Errors;
            }
        }

        return tournamentOilPattern;
    }

    /// <summary>Associates a round with this oil pattern; returns an error if already associated.</summary>
    internal ErrorOr<Updated> AddTournamentRound(TournamentRound tournamentRound)
    {
        ArgumentNullException.ThrowIfNull(tournamentRound);

        if (TournamentRounds.Contains(tournamentRound))
        {
            return TournamentOilPatternErrors.TournamentRoundAlreadyAssociatedWithOilPattern(tournamentRound.Name);
        }

        _tournamentRounds.Add(tournamentRound);

        return Result.Updated;
    }
}

internal static class TournamentOilPatternErrors
{
    public static Error NoTournamentRoundsSpecified()
    {
        return Error.Validation(
            code: "TournamentOilPattern.NoRoundsSpecified",
            description: "At least one tournament round must be specified when adding an oil pattern.");
    }

    public static Error TournamentRoundAlreadyAssociatedWithOilPattern(string tournamentRoundName)
        => Error.Validation(
            code: "TournamentOilPattern.RoundAlreadyAssociated",
            description: "Tournament round is already associated with this oil pattern.",
            metadata: new Dictionary<string, object>
            {
                { "TournamentRoundName", tournamentRoundName }
            });
}