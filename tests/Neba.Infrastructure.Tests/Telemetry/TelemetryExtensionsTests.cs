using System.Diagnostics;

using Neba.Infrastructure.Telemetry;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Telemetry;

[UnitTest]
[Component("Infrastructure.Telemetry")]
public sealed class TelemetryExtensionsTests : IDisposable
{
    private readonly ActivitySource _activitySource;

    public TelemetryExtensionsTests()
    {
        _activitySource = new ActivitySource("Neba.Tests.Telemetry");
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }

    private Activity? CreateTestActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        return _activitySource.StartActivity("TestActivity");
    }

    [Fact(DisplayName = "SetCodeAttributes sets function and namespace tags")]
    public void SetCodeAttributes_WithValidActivity_SetsFunctionAndNamespaceTags()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", "Neba.Tests");

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Tests");
    }

    [Fact(DisplayName = "SetCodeAttributes with null activity returns null")]
    public void SetCodeAttributes_WithNullActivity_ReturnsNull()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", "Neba.Tests");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "SetCodeAttributes with empty namespace does not set namespace tag")]
    public void SetCodeAttributes_WithEmptyNamespace_DoesNotSetNamespaceTag()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", string.Empty);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBeNull();
    }

    [Fact(DisplayName = "SetCodeAttributes with null namespace does not set namespace tag")]
    public void SetCodeAttributes_WithNullNamespace_DoesNotSetNamespaceTag()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", null!);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBeNull();
    }

    [Fact(DisplayName = "SetExceptionTags sets all exception-related tags")]
    public void SetExceptionTags_WithValidActivity_SetsAllExceptionTags()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test error message");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Test error message");
        activity.GetTagItem("error.stack_trace").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error message");
    }

    [Fact(DisplayName = "SetExceptionTags with null activity returns null")]
    public void SetExceptionTags_WithNullActivity_ReturnsNull()
    {
        // Arrange
        Activity? activity = null;
        var exception = new InvalidOperationException("Test error message");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "SetExceptionTags handles exception with null stack trace")]
    public void SetExceptionTags_WithNullStackTrace_SetsNullStackTraceTag()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new InvalidOperationException("Test error");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Test error");
    }

    [Fact(DisplayName = "SetExceptionTags handles different exception types")]
    public void SetExceptionTags_WithArgumentException_SetsCorrectErrorType()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new NotSupportedException("Operation not supported");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.NotSupportedException");
    }

    [Fact(DisplayName = "SetCodeAttributes can be chained with SetExceptionTags")]
    public void SetCodeAttributes_ChainingWithSetExceptionTags_WorksCorrectly()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new InvalidOperationException("Chained error");

        // Act
        Activity? result = activity
            .SetCodeAttributes("ChainedFunction", "Neba.Tests.Chaining")
            ?.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("ChainedFunction");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Tests.Chaining");
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Chained error");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }
}
