
using Neba.Api.Messaging;

namespace Neba.Api.Telemetry.Tracing;

internal static class TracingConfiguration
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

            //services.Decorate(typeof(ICommandHandler<,>), typeof(TracedCommandHandlerDecorator<,>)); // once we add a command handler uncomment this line
        }
    }
}