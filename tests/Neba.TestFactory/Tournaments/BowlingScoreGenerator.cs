using System.Collections.Frozen;

using Neba.TestFactory.Attributes;

namespace Neba.TestFactory.Tournaments;

#pragma warning disable CA5394 // Random is acceptable here — used only for test data generation, not security
public sealed class BowlingScoreGenerator
{
    private readonly Random _random;

    private readonly FrozenSet<int> _realisticScores =
    [
        110,
        111, 112, 113, 114, 115, 116, 117, 118, 119, 120,
        121, 122, 123, 124, 125, 126, 127, 128, 129, 130,
        131, 132, 133, 134, 135, 136, 137, 138, 139, 140,
        141, 142, 143, 144, 145, 146, 147, 148, 149, 150,
        151, 152, 153, 154, 155, 156, 157, 158, 159, 160,
        161, 162, 163, 164, 165, 166, 167, 168, 169, 170,
        171, 172, 173, 174, 175, 176, 177, 178, 179, 180,
        181, 182, 183, 184, 185, 186, 187, 188, 189, 190,
        191, 192, 193, 194, 195, 196, 197, 198, 199, 200,
        201, 202, 203, 204, 205, 206, 207, 208, 209, 210,
        211, 212, 213, 214, 215, 216, 217, 218, 219, 220,
        221, 222, 223, 224, 225, 226, 227, 228, 229, 230,
        231, 232, 233, 234, 235, 236, 237, 238, 239, 240,
        250, 255, 256, 257, 258, 259, 260,
        264, 265, 266, 267, 268, 269, 270,
        274, 275, 276, 277, 278, 279, 280,
        285, 286, 287, 288, 289, 290,
        295, 296, 297, 298, 299, 300
    ];

    public BowlingScoreGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int GenerateScore(int min = 110, int max = 300)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(min, 110);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(max, 300);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);

        var validScores = _realisticScores.Where(score => score >= min && score <= max).ToList();

        return validScores.Count == 0
            ? throw new InvalidOperationException($"No valid scores available in the range {min}-{max}.")
            : validScores[_random.Next(validScores.Count)];
    }

    public IReadOnlyCollection<int> GenerateSeries(int games)
    {
        return [.. Enumerable.Range(0, games).Select(_ => GenerateScore())];
    }
}

[UnitTest]
[Component("Tournaments")]
public sealed class BowlingScoreGeneratorTest
{
    [Fact(DisplayName = "GenerateScore should return a score between 110 and 300 by default")]
    public void GenerateScore_ShouldReturnScoreBetween110And300_WhenNoRangeSpecified()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();

        // Act
        int score = generator.GenerateScore();

        // Assert
        score.ShouldBeGreaterThanOrEqualTo(110);
        score.ShouldBeLessThanOrEqualTo(300);
    }

    [Fact(DisplayName = "GenerateScore should return a score within the specified range")]
    public void GenerateScore_ShouldReturnScoreWithinRange_WhenRangeIsSpecified()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int min = 150;
        const int max = 200;

        // Act
        int score = generator.GenerateScore(min, max);

        // Assert
        score.ShouldBeGreaterThanOrEqualTo(min);
        score.ShouldBeLessThanOrEqualTo(max);
    }

    [Fact(DisplayName = "GenerateScore should throw ArgumentOutOfRangeException when min is less than 110")]
    public void GenerateScore_ShouldThrowArgumentOutOfRangeException_WhenMinIsLessThan110()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int min = 100;
        const int max = 200;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => generator.GenerateScore(min, max));
    }

    [Fact(DisplayName = "GenerateScore should throw ArgumentOutOfRangeException when max is greater than 300")]
    public void GenerateScore_ShouldThrowArgumentOutOfRangeException_WhenMaxIsGreaterThan300()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int min = 150;
        const int max = 310;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => generator.GenerateScore(min, max));
    }

    [Fact(DisplayName = "GenerateScore should throw ArgumentOutOfRangeException when min is greater than max")]
    public void GenerateScore_ShouldThrowArgumentOutOfRangeException_WhenMinIsGreaterThanMax()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int min = 200;
        const int max = 199;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => generator.GenerateScore(min, max));
    }

    [Fact(DisplayName = "GenerateScore should throw InvalidOperationException when range contains no valid scores")]
    public void GenerateScore_ShouldThrowInvalidOperationException_WhenRangeContainsNoValidScores()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int min = 261;
        const int max = 263;

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => generator.GenerateScore(min, max));

        // Assert
        exception.Message.ShouldBe($"No valid scores available in the range {min}-{max}.");
    }

    [Fact(DisplayName = "GenerateSeries should return the requested number of games")]
    public void GenerateSeries_ShouldReturnRequestedNumberOfGames()
    {
        // Arrange
        var generator = new BowlingScoreGenerator();
        const int games = 5;

        // Act
        var series = generator.GenerateSeries(games);

        // Assert
        series.Count.ShouldBe(games);
    }
}