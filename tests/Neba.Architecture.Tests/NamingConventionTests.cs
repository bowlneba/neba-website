using Neba.Application.BackgroundJobs;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class NamingConventionTests : ArchitectureTestBase
{
    [Fact(DisplayName = "Query handlers should have name ending with QueryHandler")]
    public void QueryHandlers_ShouldHaveNameEndingWith_QueryHandler()
        => Classes().That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And().ResideInAssembly(ApplicationAssembly)
            .Should().HaveNameEndingWith("QueryHandler")
            .Check(ArchModel);

    [Fact(DisplayName = "Command handlers should have name ending with CommandHandler")]
    public void CommandHandlers_ShouldHaveNameEndingWith_CommandHandler()
    {
        var filter = Classes().That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And().ResideInAssembly(ApplicationAssembly);

        if (!filter.GetObjects(ArchModel).Any()) return;

        filter.Should().HaveNameEndingWith("CommandHandler").Check(ArchModel);
    }

    [Fact(DisplayName = "Background job handlers should have name ending with JobHandler")]
    public void BackgroundJobHandlers_ShouldHaveNameEndingWith_JobHandler()
        => Classes().That()
            .ImplementInterface(typeof(IBackgroundJobHandler<>))
            .And().ResideInAssembly(ApplicationAssembly)
            .Should().HaveNameEndingWith("JobHandler")
            .Check(ArchModel);
}