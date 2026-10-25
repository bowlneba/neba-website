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
                Id = dto.Id,
                BowlerName = dto.BowlerName,
                InductionYear = dto.Year,
                Categories = dto.Categories,
                PhotoUri = dto.PhotoUri,
                PhotoFocalX = dto.PhotoFocalX,
                PhotoFocalY = dto.PhotoFocalY,
                PhotoZoom = dto.PhotoZoom
            };
        }
    }
}