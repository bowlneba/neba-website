using FluentValidation;

namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed class UpdateTournamentStatsRequestValidator : AbstractValidator<UpdateTournamentStatsRequest>
{
    public UpdateTournamentStatsRequestValidator() => RuleFor(r => r.TournamentId).GreaterThan(0);
}