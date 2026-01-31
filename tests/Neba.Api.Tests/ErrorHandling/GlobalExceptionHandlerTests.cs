using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.ErrorHandling;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ErrorHandling;

#pragma warning disable CA2201 // Do not raise reserved exception types

[UnitTest]
[Component("Api.ErrorHandling")]
public sealed class GlobalExceptionHandlerTests
{
    [Fact(DisplayName = "Should return 500 status code when exception occurs")]
    public async Task TryHandleAsync_ShouldReturn500StatusCode_WhenExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.HttpContext == httpContext &&
                ctx.Exception == exception &&
                ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError &&
                ctx.ProblemDetails.Detail == "An unhandled exception occurred while processing the request.")))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetailsServiceMock.Verify(
            s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.HttpContext == httpContext &&
                ctx.Exception == exception)),
            Times.Once);
    }

    [Fact(DisplayName = "Should write problem details with correct properties")]
    public async Task TryHandleAsync_ShouldWriteProblemDetails_WithCorrectProperties()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test-path";
        var exception = new Exception("Test error");
        var cancellationToken = CancellationToken.None;

        ProblemDetailsContext? capturedContext = null;
        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => capturedContext = ctx)
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.HttpContext.ShouldBe(httpContext);
        capturedContext.Exception.ShouldBe(exception);
        capturedContext.ProblemDetails.ShouldNotBeNull();
        capturedContext.ProblemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        capturedContext.ProblemDetails.Detail.ShouldBe("An unhandled exception occurred while processing the request.");
    }

    [Fact(DisplayName = "Should return result from problem details service")]
    public async Task TryHandleAsync_ShouldReturnServiceResult_WhenServiceReturnsTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return result from problem details service when false")]
    public async Task TryHandleAsync_ShouldReturnServiceResult_WhenServiceReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(false);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory(DisplayName = "Should handle different exception types")]
    [InlineData(typeof(InvalidOperationException), "Invalid operation", TestDisplayName = "InvalidOperationException")]
    [InlineData(typeof(ArgumentException), "Bad argument", TestDisplayName = "ArgumentException")]
    [InlineData(typeof(NullReferenceException), "Null reference", TestDisplayName = "NullReferenceException")]
    public async Task TryHandleAsync_ShouldHandleDifferentExceptionTypes_WithCorrectDetails(
        Type exceptionType,
        string message)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.Exception == exception &&
                ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError)))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact(DisplayName = "Should set status code before writing problem details")]
    public async Task TryHandleAsync_ShouldSetStatusCode_BeforeWritingProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;
        var statusCodeWhenWriteCalled = 0;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback(() => statusCodeWhenWriteCalled = httpContext.Response.StatusCode)
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        statusCodeWhenWriteCalled.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}