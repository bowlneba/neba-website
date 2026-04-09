using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

namespace Neba.Website.Server.HallOfFame;

internal static class HallOfFameMappingExtensions
{
    extension(HallOfFameInductionResponse dto)
    {
        public HallOfFameInductionViewModel ToViewModel()
        {
            return new HallOfFameInductionViewModel
            {
                BowlerName = dto.BowlerName,
                InductionYear = dto.Year,
                Categories = dto.Categories,
                PhotoUri = dto.PhotoUri
            };
        }
    }
}