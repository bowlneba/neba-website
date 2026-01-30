using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Messaging;
using Neba.Infrastructure.Telemetry.Tracing;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Telemetry.Tracing;

[UnitTest]
[Component("Infrastructure.Telemetry.Tracing")]
public sealed class TracedQueryHandlerDecoratorTests
{
    public sealed class TestQuery : IQuery<string>;

    public sealed class TestCachedQuery : ICachedQuery<string>
    {
        public string CacheKey => "test-cache-key";
        public TimeSpan Expiry => TimeSpan.FromMinutes(5);
        public IReadOnlyCollection<string> Tags => ["tag1", "tag2"];
    }

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

    [Fact(DisplayName = "Should call inner handler with query")]
    public async Task HandleAsync_ShouldCallInnerHandler_WhenCalled()
    {
        // Arrange
        var query = new TestQuery();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ReturnsAsync("test-response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(query, cancellationToken);

            // Assert
            result.ShouldBe("test-response");
            innerHandlerMock.Verify(
                h => h.HandleAsync(query, cancellationToken),
                Times.Once);
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should pass query through to handler and return result")]
    public async Task HandleAsync_ShouldPassQueryThrough_AndReturnHandlerResult()
    {
        // Arrange
        var query = new TestQuery();
        const string expectedResult = "expected-result";
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ReturnsAsync(expectedResult);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result = await decorator.HandleAsync(query, cancellationToken);

            // Assert
            result.ShouldBe(expectedResult);
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
        var query = new TestQuery();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ThrowsAsync(testException);

        using var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        // Act & Assert
        var exception = await Should.ThrowAsync<TestException>(
            () => decorator.HandleAsync(query, cancellationToken));

        exception.ShouldBe(testException);
    }

    [Fact(DisplayName = "Should pass cancellation token to inner handler")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToInnerHandler()
    {
        // Arrange
        var query = new TestQuery();
        var cancellationToken = new CancellationToken();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
        var stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(1000);
        stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(It.IsAny<long>()))
            .Returns(TimeSpan.FromMilliseconds(10));

        innerHandlerMock
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ReturnsAsync("response");

        using var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        // Act
            await decorator.HandleAsync(query, cancellationToken);

        // Assert
        innerHandlerMock.Verify(
            h => h.HandleAsync(query, cancellationToken),
            Times.Once);
    }

    [Fact(DisplayName = "Should use stopwatch provider for timing measurement")]
    public async Task HandleAsync_ShouldUseStopwatchProvider_ForTiming()
    {
        // Arrange
        var query = new TestQuery();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ReturnsAsync("response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            await decorator.HandleAsync(query, cancellationToken);

            // Assert - verify stopwatch provider methods were called with expected values
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
        var query = new TestQuery();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(query, cancellationToken))
            .ThrowsAsync(testException);

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act & Assert
            await Should.ThrowAsync<TestException>(() => decorator.HandleAsync(query, cancellationToken));

            // Verify that stopwatch provider was used even on exception with expected values
            stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
            stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }

    [Fact(DisplayName = "Should handle cached and non-cached queries")]
    public async Task HandleAsync_ShouldHandle_CachedAndNonCachedQueries()
    {
        // Arrange - non-cached query
        var nonCachedQuery = new TestQuery();
        var innerHandlerMock1 = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
        var stopwatchProviderMock1 = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp1 = 1000;
        stopwatchProviderMock1
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp1);
        stopwatchProviderMock1
            .Setup(s => s.GetElapsedTime(startTimestamp1))
            .Returns(TimeSpan.FromMilliseconds(10));

        var cancellationToken1 = CancellationToken.None;
        innerHandlerMock1
            .Setup(h => h.HandleAsync(nonCachedQuery, cancellationToken1))
            .ReturnsAsync("response1");

        var loggerFactory1 = new LoggerFactory();
        var logger1 = loggerFactory1.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator1 = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock1.Object, stopwatchProviderMock1.Object, logger1);

        // Arrange - cached query
        var cachedQuery = new TestCachedQuery();
        var innerHandlerMock2 = new Mock<IQueryHandler<TestCachedQuery, string>>(MockBehavior.Strict);
        var stopwatchProviderMock2 = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        const long startTimestamp2 = 1000;
        stopwatchProviderMock2
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp2);
        stopwatchProviderMock2
            .Setup(s => s.GetElapsedTime(startTimestamp2))
            .Returns(TimeSpan.FromMilliseconds(5));

        var cancellationToken2 = CancellationToken.None;
        innerHandlerMock2
            .Setup(h => h.HandleAsync(cachedQuery, cancellationToken2))
            .ReturnsAsync("response2");

        var loggerFactory2 = new LoggerFactory();
        var logger2 = loggerFactory2.CreateLogger<TracedQueryHandlerDecorator<TestCachedQuery, string>>();
        var decorator2 = new TracedQueryHandlerDecorator<TestCachedQuery, string>(
            innerHandlerMock2.Object, stopwatchProviderMock2.Object, logger2);

        try
        {
            // Act
            var result1 = await decorator1.HandleAsync(nonCachedQuery, cancellationToken1);
            var result2 = await decorator2.HandleAsync(cachedQuery, cancellationToken2);

            // Assert
            result1.ShouldBe("response1");
            result2.ShouldBe("response2");
            innerHandlerMock1.Verify(h => h.HandleAsync(nonCachedQuery, cancellationToken1), Times.Once);
            innerHandlerMock2.Verify(h => h.HandleAsync(cachedQuery, cancellationToken2), Times.Once);
        }
        finally
        {
            loggerFactory1.Dispose();
            loggerFactory2.Dispose();
        }
    }

    [Fact(DisplayName = "Should handle multiple sequential calls")]
    public async Task HandleAsync_ShouldHandleMultipleSequentialCalls_Correctly()
    {
        // Arrange
        var query1 = new TestQuery();
        var query2 = new TestQuery();
        var innerHandlerMock = new Mock<IQueryHandler<TestQuery, string>>(MockBehavior.Strict);
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
            .Setup(h => h.HandleAsync(It.IsAny<TestQuery>(), cancellationToken))
            .ReturnsAsync("response");

        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<TracedQueryHandlerDecorator<TestQuery, string>>();
        var decorator = new TracedQueryHandlerDecorator<TestQuery, string>(
            innerHandlerMock.Object, stopwatchProviderMock.Object, logger);

        try
        {
            // Act
            var result1 = await decorator.HandleAsync(query1, cancellationToken);
            var result2 = await decorator.HandleAsync(query2, cancellationToken);

            // Assert
            result1.ShouldBe("response");
            result2.ShouldBe("response");
            innerHandlerMock.Verify(
                h => h.HandleAsync(It.IsAny<TestQuery>(), cancellationToken),
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