using Neba.Application.BackgroundJobs;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class VisibilityTests : ArchitectureTestBase
{
    [Fact(DisplayName = "Query handlers should be internal")]
    public void QueryHandlers_ShouldBeInternal()
        => Classes().That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And().ResideInAssembly(ApplicationAssembly)
            .Should().BeInternal()
            .Check(ArchModel);

    [Fact(DisplayName = "Command handlers should be internal")]
    public void CommandHandlers_ShouldBeInternal()
    {
        var filter = Classes().That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And().ResideInAssembly(ApplicationAssembly);

        if (!filter.GetObjects(ArchModel).Any()) return;

        filter.Should().BeInternal().Check(ArchModel);
    }

    [Fact(DisplayName = "Background job handlers should be internal")]
    public void BackgroundJobHandlers_ShouldBeInternal()
        => Classes().That()
            .ImplementInterface(typeof(IBackgroundJobHandler<>))
            .And().ResideInAssembly(ApplicationAssembly)
            .Should().BeInternal()
            .Check(ArchModel);
}