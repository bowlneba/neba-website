using ErrorOr;

using FluentValidation.Results;

using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesMutationResultSenderTests
{
    private static async Task<(int Status, List<ValidationFailure> Failures)> RunAsync(params Error[] errors)
    {
        var failures = new List<ValidationFailure>();
        var status = 0;

        await SeriesMutationResultSender.SendAsync(
            errors,
            failures.Add,
            (code, _) =>
            {
                status = code;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        return (status, failures);
    }

    [Fact(DisplayName = "SendAsync should send 422 when any Validation error is present, and only the Validation errors")]
    public async Task SendAsync_ShouldSend422_WhenAnyValidationErrorIsPresent()
    {
        // Arrange
        var validation = SeriesErrors.DuplicateTournament(2, "T");
        var conflict = SeriesErrors.TournamentInAnotherSeries(3, "U", "Other");

        // Act
        var (status, failures) = await RunAsync(conflict, validation);

        // Assert
        status.ShouldBe(422);
        failures.Single().ErrorCode.ShouldBe("Series.Stage.DuplicateTournament");
    }

    [Fact(DisplayName = "SendAsync should send 409 with every error when all errors are Conflicts")]
    public async Task SendAsync_ShouldSend409_WhenAllErrorsAreConflicts()
    {
        // Arrange & Act
        var (status, failures) = await RunAsync(SeriesErrors.Locked, SeriesErrors.TournamentInAnotherSeries(1, "U", "Other"));

        // Assert
        status.ShouldBe(409);
        failures.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "SendAsync should name a Stage error Series.Stages[index] from its StageOrder")]
    public async Task SendAsync_ShouldNameStageErrorByRow()
    {
        // Arrange

        // Act
        var (_, failures) = await RunAsync(SeriesErrors.DuplicateTournament(3, "T"));

        // Assert
        failures.Single().PropertyName.ShouldBe("Series.Stages[2]");
        failures.Single().ErrorMessage.ShouldStartWith("Tournament 3 (T): ");
    }

    [Fact(DisplayName = "SendAsync should name a Finale error by its PropertyKey")]
    public async Task SendAsync_ShouldNameFinaleErrorByProperty()
    {
        // Arrange & Act
        var (_, failures) = await RunAsync(
            SeriesErrors.FinaleCountNotPositive,
            SeriesErrors.StageNotBeforeFinale(new DateOnly(2025, 9, 1), "T", new DateOnly(2025, 9, 2)),
            SeriesErrors.FinaleBowlingCenterNotAvailable("99999"));

        // Assert
        failures.Select(f => f.PropertyName).ShouldBe(
            ["Series.Finale.FinalistCount", "Series.Finale.Date", "Series.Finale.BowlingCenterId"]);
    }

    [Fact(DisplayName = "SendAsync should name an error with no metadata after the Series")]
    public async Task SendAsync_ShouldNameOtherErrorsAfterSeries()
    {
        // Arrange & Act
        var (_, failures) = await RunAsync(SeriesErrors.NameRequired, SeriesErrors.SeasonNotFound(TournamentId.New().ToString()));

        // Assert
        failures.Select(f => f.PropertyName).ShouldAllBe(name => name == "Series");
    }
}