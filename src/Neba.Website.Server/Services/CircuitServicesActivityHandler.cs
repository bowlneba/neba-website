using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Neba.Website.Server.Services;

/// <summary>
/// Points <see cref="CircuitServicesAccessor"/> at this circuit's services for the length of each
/// inbound circuit activity (circuit start, events, JS calls, navigation). Async work started
/// during the activity keeps the value after the activity returns.
/// </summary>
internal sealed class CircuitServicesActivityHandler(
    IServiceProvider services,
    CircuitServicesAccessor circuitServicesAccessor)
    : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            circuitServicesAccessor.Services = services;

            try
            {
                await next(context);
            }
            finally
            {
                circuitServicesAccessor.Services = null;
            }
        };
}