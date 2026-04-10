using Microsoft.Extensions.DependencyInjection;

namespace Neba.Application.Messaging;

internal static class MessagingConfiguration
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