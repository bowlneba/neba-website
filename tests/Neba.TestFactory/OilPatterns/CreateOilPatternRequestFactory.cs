using Neba.Api.Contracts.OilPatterns.CreateOilPattern;

namespace Neba.TestFactory.OilPatterns;

public static class CreateOilPatternRequestFactory
{
    public const string ValidName = "Dragon";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 24.5m;
    public const decimal ValidLeftRatio = 5.5m;
    public const decimal ValidRightRatio = 8.0m;

    public static CreateOilPatternRequest Create(
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            KegelId = kegelId
        };
}