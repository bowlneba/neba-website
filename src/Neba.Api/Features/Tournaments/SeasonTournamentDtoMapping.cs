using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;

namespace Neba.Api.Features.Tournaments;

internal static class SeasonTournamentDtoMapping
{
    extension(SeasonTournamentDto tournament)
    {
        public SeasonTournamentResponse ToResponse()
            => new()
            {
                Id = tournament.Id.Value.ToString(),
                Name = tournament.Name,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                TournamentType = tournament.TournamentType,
                Status = tournament.Status,
                TitleEligible = tournament.TitleEligible,
                EntryFee = tournament.EntryFee,
                RegistrationUrl = tournament.RegistrationUrl,
                AddedMoney = tournament.AddedMoney,
                SponsorMoney = tournament.SponsorMoney,
                NebaAddedMoney = tournament.NebaAddedMoney,
                Reservations = tournament.Reservations,
                PatternLengthCategory = tournament.PatternLengthCategory,
                PatternRatioCategory = tournament.PatternRatioCategory,
                OilPatternRevealDateTime = tournament.OilPatternRevealDateTime,
                LogoUrl = tournament.LogoUrl,
                Winners = [.. tournament.Winners.Select(w => w.ToDisplayName())],
                BowlingCenter = tournament.BowlingCenter is null ? null : new TournamentBowlingCenterResponse
                {
                    Name = tournament.BowlingCenter.Name,
                    City = tournament.BowlingCenter.City,
                    State = tournament.BowlingCenter.State,
                },
                Sponsors = [.. tournament.Sponsors.Select(s => new TournamentSponsorResponse
                {
                    Name = s.Name,
                    Slug = s.Slug,
                    LogoUrl = s.LogoUrl,
                })],
                OilPatterns = [.. tournament.OilPatterns.Select(op => new TournamentOilPatternResponse
                {
                    Name = op.Name,
                    Length = op.Length,
                    Volume = op.Volume,
                    LeftRatio = op.LeftRatio,
                    RightRatio = op.RightRatio,
                    KegelId = op.KegelId,
                    Rounds = op.TournamentRounds,
                })],
            };
    }
}
