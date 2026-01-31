using Microsoft.Extensions.DependencyInjection;

namespace Neba.Application.Messaging;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Classes should be static

internal static class MessagingExtensions
{
    extension(IServiceCollection services)
    {
        public void AddMessaging()
        {
            services.Scan(scan => scan
                .FromAssemblies(typeof(IApplicationAssemblyMarker).Assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan
                .FromAssemblies(typeof(IApplicationAssemblyMarker).Assembly)
                .AddClasses(classes => classes
                    .AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }
    }
}