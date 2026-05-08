using System.Globalization;

using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.Website.Server.Tournaments.Detail;

internal static class TournamentDetailMappingExtensions
{
    extension(TournamentDetailResponse response)
    {
        public TournamentDetailViewModel ToViewModel() => new()
        {
            Id = response.Id,
            Name = response.Name,
            SeasonDescription = response.Season.Description,
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            TournamentType = response.TournamentType,
            StatsEligible = response.StatsEligible,
            EntryFee = response.EntryFee,
            RegistrationUrl = response.RegistrationUrl,
            AddedMoney = response.AddedMoney,
            EntryCount = response.EntryCount,
            PatternLengthCategory = response.PatternLengthCategory,
            LogoUrl = response.LogoUrl,
            BowlingCenterName = response.BowlingCenter?.Name,
            BowlingCenterCity = response.BowlingCenter?.City,
            BowlingCenterState = response.BowlingCenter?.State,
            Sponsors = [.. response.Sponsors.Select(s => s.ToViewModel())],
            OilPatterns = [.. response.OilPatterns.Select(p => p.ToViewModel())],
            Winners = response.Winners,
            Results = [.. response.Results.Select(r => r.ToViewModel())],
        };
    }

    extension(TournamentDetailSponsorResponse response)
    {
        public TournamentDetailSponsorViewModel ToViewModel() => new()
        {
            Name = response.Name,
            Slug = response.Slug,
            LogoUrl = response.LogoUrl,
            WebsiteUrl = response.WebsiteUrl,
            TagPhrase = response.TagPhrase,
        };
    }

    extension(TournamentDetailOilPatternResponse response)
    {
        public TournamentDetailOilPatternViewModel ToViewModel() => new()
        {
            Name = response.Name,
            Length = response.Length,
            Volume = response.Volume,
            LeftRatio = response.LeftRatio,
            RightRatio = response.RightRatio,
            KegelId = response.KegelId,
            Rounds = response.Rounds,
        };
    }

    extension(TournamentResultResponse response)
    {
        public TournamentResultViewModel ToViewModel() => new()
        {
            BowlerName = response.BowlerName,
            Place = response.Place,
            PrizeMoney = response.PrizeMoney,
            Points = response.Points,
            SideCutName = response.SideCutName,
            SideCutIndicator = response.SideCutIndicator,
        };
    }
}
