using ErrorOr;

using FluentValidation.Results;

using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementResultSenderTests
{
    private sealed record Sent(int? Status, bool NotFound, List<ValidationFailure> Failures);

    private static async Task<Sent> RunAsync(params Error[] errors)
    {
        var failures = new List<ValidationFailure>();
        int? status = null;
        var notFound = false;

        await SeriesAdvancementResultSender.SendAsync(
            errors,
            failures.Add,
            (code, _) =>
            {
                status = code;
                return Task.CompletedTask;
            },
            _ =>
            {
                notFound = true;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        return new Sent(status, notFound, failures);
    }

    [Fact(DisplayName = "SendAsync should send 404 and add no failure for a NotFound error")]
    public async Task SendAsync_ShouldSendNotFound_ForNotFoundError()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesErrors.NotFound(SeriesId.New()));

        // Assert
        sent.NotFound.ShouldBeTrue();
        sent.Status.ShouldBeNull();
        sent.Failures.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SendAsync should send 404 for a Stage NotFound error")]
    public async Task SendAsync_ShouldSendNotFound_ForStageNotFoundError()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesAdvancementErrors.StageNotFound(SeriesStageId.New()));

        // Assert
        sent.NotFound.ShouldBeTrue();
        sent.Failures.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SendAsync should send 404 when the first error is NotFound, whatever follows it")]
    public async Task SendAsync_ShouldSendNotFound_WhenFirstErrorIsNotFound()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesErrors.NotFound(SeriesId.New()), SeriesAdvancementErrors.NoResultRow);

        // Assert
        sent.NotFound.ShouldBeTrue();
        sent.Status.ShouldBeNull();
    }

    [Fact(DisplayName = "SendAsync should send 409 with every error when all errors are Conflicts")]
    public async Task SendAsync_ShouldSend409_WhenAllErrorsAreConflicts()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesAdvancementErrors.VersionConflict, SeriesAdvancementErrors.OverrideExists);

        // Assert
        sent.Status.ShouldBe(409);
        sent.NotFound.ShouldBeFalse();
        sent.Failures.Select(f => f.ErrorCode).ShouldBe(["Series.Version.Conflict", "Series.Override.Exists"]);
    }

    [Fact(DisplayName = "SendAsync should send 409 for a Conflict with no metadata, such as Busy")]
    public async Task SendAsync_ShouldSend409_ForConflictWithNoMetadata()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesAdvancementErrors.Busy);

        // Assert
        sent.Status.ShouldBe(409);
        var failure = sent.Failures.Single();
        failure.PropertyName.ShouldBe("Series");
        failure.ErrorCode.ShouldBe("Series.Advancement.Busy");
    }

    [Fact(DisplayName = "SendAsync should send 422 with only the Validation errors when the list mixes Validation and Conflict")]
    public async Task SendAsync_ShouldSend422_WhenListMixesValidationAndConflict()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesAdvancementErrors.VersionConflict, SeriesAdvancementErrors.NoResultRow);

        // Assert
        sent.Status.ShouldBe(422);
        sent.Failures.Single().ErrorCode.ShouldBe("Series.Override.NoResultRow");
    }

    [Fact(DisplayName = "SendAsync should send 422 for a Validation error")]
    public async Task SendAsync_ShouldSend422_ForValidationError()
    {
        // Arrange & Act
        var sent = await RunAsync(SeriesAdvancementErrors.StageNotSurvivor);

        // Assert
        sent.Status.ShouldBe(422);
        sent.Failures.Single().ErrorMessage.ShouldBe(SeriesAdvancementErrors.StageNotSurvivor.Description);
    }

    [Fact(DisplayName = "SendAsync should send 409 with one Series.Stages[n] failure per official Stage order for a ConfirmationRequired error")]
    public async Task SendAsync_ShouldSend409WithOneFailurePerOfficialStage_ForConfirmationRequired()
    {
        // Arrange
        var error = SeriesAdvancementErrors.ConfirmationRequired([2, 3, 5]);

        // Act
        var sent = await RunAsync(error);

        // Assert
        sent.Status.ShouldBe(409);
        sent.NotFound.ShouldBeFalse();
        sent.Failures.Select(f => f.PropertyName).ShouldBe(["Series.Stages[1]", "Series.Stages[2]", "Series.Stages[4]"]);
        sent.Failures.ShouldAllBe(f => f.ErrorCode == "Series.Recompute.ConfirmationRequired" && f.ErrorMessage == error.Description);
    }

    [Fact(DisplayName = "SendAsync should fall back to the Series sender when the OfficialStageOrders metadata is not an int array")]
    public async Task SendAsync_ShouldFallBack_WhenOfficialStageOrdersIsNotAnIntArray()
    {
        // Arrange
        var error = Error.Conflict(
            "Series.Odd", "Odd.", new Dictionary<string, object> { { SeriesAdvancementErrors.OfficialStageOrdersKey, "2, 3" } });

        // Act
        var sent = await RunAsync(error);

        // Assert
        sent.Status.ShouldBe(409);
        sent.Failures.Single().PropertyName.ShouldBe("Series");
    }

#nullable disable
    [Fact(DisplayName = "SendAsync should throw ArgumentNullException when the errors are null")]
    public void SendAsync_ShouldThrow_WhenErrorsAreNull()
    {
        // Arrange & Act
        var act = () => SeriesAdvancementResultSender.SendAsync(
            null, _ => { }, (_, _) => Task.CompletedTask, _ => Task.CompletedTask, TestContext.Current.CancellationToken);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact(DisplayName = "SendAsync should throw ArgumentNullException when a callback is null")]
    public void SendAsync_ShouldThrow_WhenACallbackIsNull()
    {
        // Arrange
        Error[] errors = [SeriesAdvancementErrors.Busy];
        var ct = TestContext.Current.CancellationToken;

        // Act
        var noAddError = () => SeriesAdvancementResultSender.SendAsync(errors, null, (_, _) => Task.CompletedTask, _ => Task.CompletedTask, ct);
        var noSendErrors = () => SeriesAdvancementResultSender.SendAsync(errors, _ => { }, null, _ => Task.CompletedTask, ct);
        var noSendNotFound = () => SeriesAdvancementResultSender.SendAsync(errors, _ => { }, (_, _) => Task.CompletedTask, null, ct);

        // Assert
        Should.Throw<ArgumentNullException>(noAddError);
        Should.Throw<ArgumentNullException>(noSendErrors);
        Should.Throw<ArgumentNullException>(noSendNotFound);
    }
#nullable enable
}