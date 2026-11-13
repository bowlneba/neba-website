using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Contracts.HallOfFame.ListHallOfFamePoints;

namespace Neba.Website.Server.HallOfFame;

internal static class HallOfFameMappingExtensions
{
    extension(HallOfFameInductionResponse dto)
    {
        public HallOfFameInductionViewModel ToViewModel()
        {
            return new HallOfFameInductionViewModel
            {
                Id = dto.Id,
                BowlerName = dto.BowlerName,
                InductionYear = dto.Year,
                Categories = [.. dto.Categories.OrderBy(HallOfFameCategories.OrderOf)],
                PhotoUri = dto.PhotoUri,
                PhotoFocalX = dto.PhotoFocalX,
                PhotoFocalY = dto.PhotoFocalY,
                PhotoZoom = dto.PhotoZoom
            };
        }
    }

    extension(HallOfFamePointsResponse response)
    {
        public HallOfFamePointsViewModel ToViewModel()
        {
            var requirements = new HallOfFamePointRequirements
            {
                Points = response.RequiredPoints,
                Seasons = response.RequiredSeasonsWithPoints,
                MembershipYears = response.RequiredMembershipYears,
                MembershipDataStartYear = response.MembershipDataStartYear
            };

            return new HallOfFamePointsViewModel
            {
                Requirements = requirements,
                RuleGroups = [.. response.Rules
                    .GroupBy(rule => rule.Group)
                    .Select(group => new HallOfFamePointRuleGroupViewModel
                    {
                        Name = group.Key,
                        Rules = [.. group.Select(rule => new HallOfFamePointRuleViewModel { Label = rule.Label, Points = rule.Points })]
                    })],
                Eligible = [.. response.Standings.Where(standing => standing.Eligible).Select(standing => standing.ToViewModel(requirements))],
                NotYetEligible = [.. response.Standings.Where(standing => !standing.Eligible).Select(standing => standing.ToViewModel(requirements))]
            };
        }
    }

    extension(HallOfFamePointsStandingResponse standing)
    {
        public HallOfFamePointsStandingViewModel ToViewModel(HallOfFamePointRequirements requirements)
        {
            ArgumentNullException.ThrowIfNull(requirements);

            return new HallOfFamePointsStandingViewModel
            {
                BowlerId = standing.BowlerId,
                BowlerName = standing.BowlerName,
                TotalPoints = standing.TotalPoints,
                SeasonsWithPoints = standing.SeasonsWithPoints,
                MembershipYears = standing.MembershipYears,
                MembershipHistoryComplete = standing.MembershipHistoryComplete,
                MeetsPoints = standing.MeetsPoints,
                MeetsSeasons = standing.MeetsSeasons,
                MeetsMembershipYears = standing.MembershipYears >= requirements.MembershipYears,
                SeasonsNeeded = Math.Max(0, requirements.Seasons - standing.SeasonsWithPoints),
                MembershipYearsNeeded = Math.Max(0, requirements.MembershipYears - standing.MembershipYears),
                LinePercent = requirements.Points > 0
                    ? Math.Min(100, standing.TotalPoints * 100 / requirements.Points)
                    : 0,
                InductedLine = InductedLine(standing.InductedCategories)
            };
        }
    }

    extension(HallOfFamePointsDetailResponse response)
    {
        public HallOfFamePointsDetailViewModel ToViewModel()
            => new()
            {
                BowlerName = response.BowlerName,
                InductedLine = InductedLine(response.InductedCategories),
                TotalPoints = response.TotalPoints,
                SeasonsWithPoints = response.SeasonsWithPoints,
                MembershipHistoryComplete = response.MembershipHistoryComplete,
                Breakdown = [.. response.Breakdown.Select(entry => new HallOfFamePointsEntryViewModel
                {
                    Source = entry.Source,
                    Year = entry.Date.Year,
                    Points = entry.Points,
                    RunningTotal = entry.RunningTotal,
                    ReachedThreshold = entry.ReachedThreshold
                })],
                MembershipYears = [.. response.MembershipYears.Select(year => new HallOfFameMembershipYearViewModel
                {
                    Year = year.Year,
                    MembershipType = year.MembershipType
                })]
            };
    }

    private static string? InductedLine(IReadOnlyCollection<string> categories)
        => categories.Count == 0
            ? null
            : "In the Hall of Fame for " + string.Join(" and ", categories.OrderBy(HallOfFameCategories.OrderOf));
}