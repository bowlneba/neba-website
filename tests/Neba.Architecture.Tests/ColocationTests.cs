using Neba.Api.Messaging;
using Neba.Application.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class ColocationTests : ArchitectureTestBase
{
    [Theory(DisplayName = "Handlers should reside in the same namespace as their command, query, or job type")]
    [MemberData(nameof(GetHandlerAndMessageTypePairs))]
    public void Handlers_ShouldResideInSameNamespace_AsTheirMessageType(
        Type handlerType,
        Type messageType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(messageType);

        handlerType.Namespace.ShouldBe(
            messageType.Namespace,
            $"{handlerType.Name} should be in the same namespace as {messageType.Name}");
    }

    public static TheoryData<Type, Type> GetHandlerAndMessageTypePairs()
    {
        Type[] handlerInterfaces =
        [
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>),
            typeof(IBackgroundJobHandler<>),
        ];

        var pairs = new TheoryData<Type, Type>();

        IEnumerable<Type> handlers = ApplicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && t.DeclaringType is null);

        foreach (var handler in handlers)
        {
            foreach (var iface in handler.GetInterfaces())
            {
                if (!iface.IsGenericType)
                {
                    continue;
                }

                var genericDef = iface.GetGenericTypeDefinition();

                if (!handlerInterfaces.Contains(genericDef))
                {
                    continue;
                }

                var messageType = iface.GetGenericArguments()[0];
                pairs.Add(handler, messageType);
                break;
            }
        }

        return pairs;
    }
}