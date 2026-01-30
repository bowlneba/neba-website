using ErrorOr;

using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Messaging;
using Neba.Infrastructure.Telemetry.Tracing;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Telemetry.Tracing;

[UnitTest]
[Component("Infrastructure.Telemetry.Tracing")]
public sealed class TracedCommandHandlerDecoratorTests
{
    public sealed class TestCommand : ICommand<string>;

#pragma warning disable S3871 // Test exception classes should be public
    public sealed class TestException : Exception
    {
        public TestException()
            : base("Test exception")
        {
        }

        public TestException(string message)
            : base(message)
        {
        }

        public TestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
#pragma warning restore S3871

    [Fact(DisplayName = "Should call inner handler with command")]
    public async Task HandleAsync_ShouldCallInnerHandler_WhenCalled()
    {
        // Arrange
        var command = new TestCommand();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(42));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync("test-response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.Value.ShouldBe("test-response");
            innerHandlerMock.Verify(
                h => h.HandleAsync(command, cancellationToken),
                Times.Once);
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should pass command through to handler and return result")]
    public async Task HandleAsync_ShouldPassCommandThrough_AndReturnHandlerResult()
    {
        // Arrange
        var command = new TestCommand();
        const string expectedResult = "expected-result";
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(100));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync(expectedResult);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.Value.ShouldBe(expectedResult);
            result.IsError.ShouldBeFalse();
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should return error result from handler without throwing")]
    public async Task HandleAsync_ShouldReturnErrorResult_WhenHandlerReturnsError()
    {
        // Arrange
        var command = new TestCommand();
        var testError = Error.Validation("test-code", "Test validation error");
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(50));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync(testError);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsError.ShouldBeTrue();
            result.FirstError.ShouldBe(testError);
            result.Errors.Count.ShouldBe(1);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should throw exception from handler without catching")]
    public async Task HandleAsync_ShouldThrowException_WhenHandlerThrows()
    {
        // Arrange
        var command = new TestCommand();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(50));
        var testException = new TestException("Test");

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ThrowsAsync(testException);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act & Assert
            var exception = await Should.ThrowAsync<TestException>(
                () => decorator.HandleAsync(command, cancellationToken));
            exception.ShouldBe(testException);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should pass cancellation token to inner handler")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToInnerHandler()
    {
        // Arrange
        var command = new TestCommand();
        var cancellationToken = new CancellationToken();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(1000);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(It.IsAny<long>()))
            .Returns(TimeSpan.FromMilliseconds(10));

        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync("response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            await decorator.HandleAsync(command, cancellationToken);

            // Assert
            innerHandlerMock.Verify(
                h => h.HandleAsync(command, cancellationToken),
                Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should use stopwatch provider for timing measurement")]
    public async Task HandleAsync_ShouldUseStopwatchProvider_ForTiming()
    {
        // Arrange
        var command = new TestCommand();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 5000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(234.5));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync("response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            await decorator.HandleAsync(command, cancellationToken);

            // Assert - verify stopwatch provider methods were called with expected values
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should measure timing even when handler returns error")]
    public async Task HandleAsync_ShouldMeasureTiming_EvenWhenHandlerReturnsError()
    {
        // Arrange
        var command = new TestCommand();
        var testError = Error.Failure("test-failure", "Test failure");
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(42));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync(testError);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            await decorator.HandleAsync(command, cancellationToken);

            // Assert - verify that stopwatch provider was used even on error with expected values
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should measure timing even when handler throws")]
    public async Task HandleAsync_ShouldMeasureTiming_EvenWhenHandlerThrows()
    {
        // Arrange
        var command = new TestCommand();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(42));
        var testException = new TestException("Handler failed");

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ThrowsAsync(testException);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act & Assert
            await Should.ThrowAsync<TestException>(() => decorator.HandleAsync(command, cancellationToken));

            // Verify that stopwatch provider was used even on exception with expected values
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should handle multiple errors from handler")]
    public async Task HandleAsync_ShouldHandleMultipleErrors_FromHandler()
    {
        // Arrange
        var command = new TestCommand();
        var error1 = Error.Validation("code1", "Error 1");
        var error2 = Error.Validation("code2", "Error 2");
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(30));

        var cancellationToken = CancellationToken.None;
        var errors = new List<Error> { error1, error2 };
        innerHandlerMock
            .Setup(h => h.HandleAsync(command, cancellationToken))
            .ReturnsAsync(errors);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsError.ShouldBeTrue();
            result.Errors.Count.ShouldBe(2);
            result.Errors.ShouldContain(error1);
            result.Errors.ShouldContain(error2);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should handle multiple sequential calls")]
    public async Task HandleAsync_ShouldHandleMultipleSequentialCalls_Correctly()
    {
        // Arrange
        var command1 = new TestCommand();
        var command2 = new TestCommand();
        var innerHandlerMock = new Mock<ICommandHandler<TestCommand, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp = 1000;
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(TimeSpan.FromMilliseconds(10));

        var cancellationToken = CancellationToken.None;
        innerHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), cancellationToken))
            .ReturnsAsync("response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedCommandHandlerDecorator<TestCommand, string>>();
        var decorator = new TracedCommandHandlerDecorator<TestCommand, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result1 = await decorator.HandleAsync(command1, cancellationToken);
            var result2 = await decorator.HandleAsync(command2, cancellationToken);

            // Assert
            result1.Value.ShouldBe("response");
            result2.Value.ShouldBe("response");
            innerHandlerMock.Verify(
                h => h.HandleAsync(It.IsAny<TestCommand>(), cancellationToken),
                Times.Exactly(2));
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Exactly(2));
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Exactly(2));
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }
}