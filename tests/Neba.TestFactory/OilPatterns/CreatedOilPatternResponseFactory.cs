using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

namespace Neba.TestFactory.OilPatterns;

public static class CreatedOilPatternResponseFactory
{
    public static CreatedOilPatternResponse Create(
        string? oilPatternId = null,
        string? name = null,
        int? length = null,
        string? lengthCategory = null,
        string? ratioCategory = null)
        => new()
        {
            OilPatternId = oilPatternId ?? "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
            Name = name ?? CreateOilPatternRequestFactory.ValidName,
            Length = length ?? CreateOilPatternRequestFactory.ValidLength,
            LengthCategory = lengthCategory ?? "Medium",
            RatioCategory = ratioCategory ?? "Challenge"
        };
}
