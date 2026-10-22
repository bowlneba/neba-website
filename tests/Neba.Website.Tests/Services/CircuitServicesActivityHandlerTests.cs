using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.CircuitServicesActivityHandler")]
public sealed class CircuitServicesActivityHandlerTests
{
    [Fact(DisplayName = "Should expose the circuit's services during an inbound activity and clear them afterwards")]
    public async Task CreateInboundActivityHandler_ShouldSetServicesOnlyDuringActivity()
    {
        // Arrange
        await using var circuitServices = new ServiceCollection().BuildServiceProvider();
        var accessor = new CircuitServicesAccessor();
        var sut = new CircuitServicesActivityHandler(circuitServices, accessor);
        IServiceProvider? seenDuringActivity = null;

        var handler = sut.CreateInboundActivityHandler(_ =>
        {
            seenDuringActivity = accessor.Services;
            return Task.CompletedTask;
        });

        // Act
        await handler(CreateActivityContext());

        // Assert
        seenDuringActivity.ShouldBeSameAs(circuitServices);
        accessor.Services.ShouldBeNull();
    }

    [Fact(DisplayName = "Should clear the circuit's services even when the activity throws")]
    public async Task CreateInboundActivityHandler_ShouldClearServices_WhenActivityThrows()
    {
        // Arrange
        await using var circuitServices = new ServiceCollection().BuildServiceProvider();
        var accessor = new CircuitServicesAccessor();
        var sut = new CircuitServicesActivityHandler(circuitServices, accessor);
        var handler = sut.CreateInboundActivityHandler(_ => throw new InvalidOperationException("boom"));

        // Act
        var act = () => handler(CreateActivityContext());

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        accessor.Services.ShouldBeNull();
    }

    [Fact(DisplayName = "Should keep the circuit's services for async work that continues after the activity returns")]
    public async Task CreateInboundActivityHandler_ShouldFlowServicesIntoContinuations()
    {
        // Arrange
        await using var circuitServices = new ServiceCollection().BuildServiceProvider();
        var accessor = new CircuitServicesAccessor();
        var sut = new CircuitServicesActivityHandler(circuitServices, accessor);
        var release = new TaskCompletionSource();
        Task<IServiceProvider?>? continuation = null;

        var handler = sut.CreateInboundActivityHandler(_ =>
        {
            continuation = ReadAfterAsync(release.Task, accessor);
            return Task.CompletedTask;
        });

        // Act
        await handler(CreateActivityContext());
        release.SetResult();
#pragma warning disable VSTHRD003 // Awaiting the continuation started inside the activity is the point of the test.
        var seenLater = await continuation!;
#pragma warning restore VSTHRD003

        // Assert
        seenLater.ShouldBeSameAs(circuitServices);
    }

    private static async Task<IServiceProvider?> ReadAfterAsync(Task gate, CircuitServicesAccessor accessor)
    {
#pragma warning disable VSTHRD003 // The gate is released by the test after the activity returns.
        await gate;
#pragma warning restore VSTHRD003
        return accessor.Services;
    }

    // CircuitInboundActivityContext has no public constructor; the handler under test never reads it.
    private static CircuitInboundActivityContext CreateActivityContext()
        => (CircuitInboundActivityContext)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CircuitInboundActivityContext));
}