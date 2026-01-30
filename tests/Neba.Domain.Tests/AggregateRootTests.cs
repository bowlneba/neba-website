using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests;

[UnitTest]
[Component("Domain")]
public sealed class AggregateRootTests
{
    private sealed class TestAggregateRoot
        : AggregateRoot
    {
        public void RaiseEvent(IDomainEvent domainEvent)
            => AddDomainEvent(domainEvent);
    }

    private sealed class TestDomainEvent(string name)
        : IDomainEvent
    {
        public string Name { get; } = name;
    }

    [Fact(DisplayName = "Adding a domain event adds it to the DomainEvents collection")]
    public void AddDomainEvent_ShouldAddEventToDomainEventsCollection_WhenCalled()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent("TestEvent");

        // Act
        aggregateRoot.RaiseEvent(domainEvent);

        // Assert
        aggregateRoot.DomainEvents.ShouldContain(domainEvent);
    }

    [Fact(DisplayName = "Clearing domain events removes all events from the DomainEvents collection")]
    public void ClearDomainEvents_ShouldRemoveAllEventsFromDomainEventsCollection_WhenCalled()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var domainEvent1 = new TestDomainEvent("TestEvent1");
        var domainEvent2 = new TestDomainEvent("TestEvent2");
        aggregateRoot.RaiseEvent(domainEvent1);
        aggregateRoot.RaiseEvent(domainEvent2);

        // Act
        aggregateRoot.ClearDomainEvents();

        // Assert
        aggregateRoot.DomainEvents.ShouldBeEmpty();
    }
}