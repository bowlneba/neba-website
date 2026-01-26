using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Neba.Application.Messaging;

namespace Neba.Infrastructure.Telemetry.Tracing;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Classes should be static

internal static class TracingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds tracing decorators to the service collection.
        /// This method should be called before any caching decorators are added.
        /// </summary>
        public void AddTracing()
        {
            services.Decorate(typeof(IQueryHandler<,>), typeof(TracedQueryHandlerDecorator<,>));
        }
    }
}