using System.Diagnostics;

using Microsoft.AspNetCore.Http;

using Neba.Api.Auditing;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class AmbientCorrelationContextTests
{
    [Fact(DisplayName = "CorrelationId returns null when no correlation id has been set")]
    public void CorrelationId_WhenNoneSet_ReturnsNull()
    {
        // Arrange & Act
        var correlationId = AmbientCorrelationContext.CorrelationId;

        // Assert
        correlationId.ShouldBeNull();
    }

    [Fact(DisplayName = "SetCorrelationId makes CorrelationId return the given value until disposed")]
    public void SetCorrelationId_MakesCorrelationIdReturnGivenValue_UntilDisposed()
    {
        // Arrange & Act
        using (AmbientCorrelationContext.SetCorrelationId("correlation-1"))
        {
            // Assert
            AmbientCorrelationContext.CorrelationId.ShouldBe("correlation-1");
        }

        AmbientCorrelationContext.CorrelationId.ShouldBeNull();
    }

    [Fact(DisplayName = "Capture returns the current Activity's TraceId when an Activity is active")]
    public void Capture_ShouldReturnActivityTraceId_WhenActivityIsActive()
    {
        // Arrange
        using var activitySource = new ActivitySource(nameof(Capture_ShouldReturnActivityTraceId_WhenActivityIsActive));
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("test-activity");
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" };

        // Act
        var correlationId = AmbientCorrelationContext.Capture(httpContext);

        // Assert
        correlationId.ShouldBe(activity!.TraceId.ToString());
    }

    [Fact(DisplayName = "Capture falls back to the HttpContext's TraceIdentifier when there is no active Activity")]
    public void Capture_ShouldReturnTraceIdentifier_WhenNoActivityIsActive()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" };

        // Act
        var correlationId = AmbientCorrelationContext.Capture(httpContext);

        // Assert
        correlationId.ShouldBe("trace-1");
    }

    [Fact(DisplayName = "Capture returns a generated id when there is no active Activity or HttpContext")]
    public void Capture_ShouldReturnGeneratedId_WhenNoActivityOrHttpContext()
    {
        // Arrange & Act
        var correlationId = AmbientCorrelationContext.Capture(null);

        // Assert
        correlationId.ShouldNotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).ShouldBeTrue();
    }
}
