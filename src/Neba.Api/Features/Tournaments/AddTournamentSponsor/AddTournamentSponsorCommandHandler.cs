using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed class AddTournamentSponsorCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<AddTournamentSponsorCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(AddTournamentSponsorCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .Include(t => t.Sponsors)
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        if (!await appDbContext.Sponsors.AnyAsync(s => s.Id == command.SponsorId, cancellationToken))
        {
            return TournamentErrors.SponsorNotFound(command.SponsorId);
        }

        var result = tournament.AddSponsor(command.SponsorId, command.TitleSponsor, command.SponsorshipAmount);

        if (result.IsError)
        {
            return result.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Success;
    }
}