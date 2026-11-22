using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Neba.Api.BackgroundJobs;
using Neba.Api.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("BackgroundJobs")]
public sealed class DomainEventPublisherTests
{
    private sealed class TestAggregate : AggregateRoot
    {
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private static (Mock<IBackgroundJobClient> Client, List<Job> Jobs, List<IState> States) CreateRecordingClient()
    {
        var jobs = new List<Job>();
        var states = new List<IState>();
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, state) =>
            {
                jobs.Add(job);
                states.Add(state);
            })
            .Returns("job-1");

        return (client, jobs, states);
    }

    private static (Job Job, IState State) PublishOne(IDomainEvent domainEvent)
    {
        var (client, jobs, states) = CreateRecordingClient();

        new DomainEventPublisher(client.Object).Publish(domainEvent);

        return (jobs.ShouldHaveSingleItem(), states.ShouldHaveSingleItem());
    }

    private static void AssertEnqueued(
        (Job Job, IState State) published, IDomainEvent domainEvent, Type expectedJobType)
    {
        published.Job.Type.ShouldBe(expectedJobType);
        published.Job.Method.Name.ShouldBe("HandleAsync");
        published.Job.Args[0].ShouldBeSameAs(domainEvent);
        published.State.ShouldBeOfType<EnqueuedState>();
    }

    [Fact(DisplayName = "Publish should enqueue an IDomainEventJob<TournamentResultsRecorded> with the event as argument")]
    public void Publish_TournamentResultsRecorded_ShouldEnqueueJobForConcreteType()
    {
        // Arrange
        IDomainEvent domainEvent = new TournamentResultsRecorded { TournamentId = TournamentId.New() };

        // Act
        var published = PublishOne(domainEvent);

        // Assert
        AssertEnqueued(published, domainEvent, typeof(IDomainEventJob<TournamentResultsRecorded>));
    }

    [Fact(DisplayName = "Publish should enqueue an IDomainEventJob<TournamentSquadsChanged> with the event as argument")]
    public void Publish_TournamentSquadsChanged_ShouldEnqueueJobForConcreteType()
    {
        // Arrange
        IDomainEvent domainEvent = new TournamentSquadsChanged { TournamentId = TournamentId.New() };

        // Act
        var published = PublishOne(domainEvent);

        // Assert
        AssertEnqueued(published, domainEvent, typeof(IDomainEventJob<TournamentSquadsChanged>));
    }

    [Fact(DisplayName = "Publish should enqueue an IDomainEventJob<SquadScoresReplaced> with the event as argument")]
    public void Publish_SquadScoresReplaced_ShouldEnqueueJobForConcreteType()
    {
        // Arrange
        IDomainEvent domainEvent = new SquadScoresReplaced { TournamentId = TournamentId.New(), SquadId = SquadId.New() };

        // Act
        var published = PublishOne(domainEvent);

        // Assert
        AssertEnqueued(published, domainEvent, typeof(IDomainEventJob<SquadScoresReplaced>));
    }

    [Fact(DisplayName = "Publish should enqueue an IDomainEventJob<SeasonStatsGenerated> with the event as argument")]
    public void Publish_SeasonStatsGenerated_ShouldEnqueueJobForConcreteType()
    {
        // Arrange
        IDomainEvent domainEvent = new SeasonStatsGenerated { SeasonId = SeasonId.New() };

        // Act
        var published = PublishOne(domainEvent);

        // Assert
        AssertEnqueued(published, domainEvent, typeof(IDomainEventJob<SeasonStatsGenerated>));
    }

#nullable disable
    [Fact(DisplayName = "Publish should throw ArgumentNullException when the event is null")]
    public void Publish_ShouldThrow_WhenEventIsNull()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var publisher = new DomainEventPublisher(client.Object);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => publisher.Publish(null));

        // Assert
        exception.ParamName.ShouldBe("domainEvent");
        client.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "PublishAndClear should throw ArgumentNullException when the aggregate is null")]
    public void PublishAndClear_ShouldThrow_WhenAggregateIsNull()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var publisher = new DomainEventPublisher(client.Object);

        // Act
        var exception = Should.Throw<ArgumentNullException>(() => publisher.PublishAndClear(null));

        // Assert
        exception.ParamName.ShouldBe("aggregate");
        client.VerifyNoOtherCalls();
    }
#nullable enable

    [Fact(DisplayName = "PublishAndClear should enqueue every event in order, then clear the aggregate")]
    public void PublishAndClear_ShouldEnqueueEveryEventInOrder_ThenClear()
    {
        // Arrange
        var (client, jobs, _) = CreateRecordingClient();
        var publisher = new DomainEventPublisher(client.Object);
        IDomainEvent first = new TournamentResultsRecorded { TournamentId = TournamentId.New() };
        IDomainEvent second = new SquadScoresReplaced { TournamentId = TournamentId.New(), SquadId = SquadId.New() };
        IDomainEvent third = new SeasonStatsGenerated { SeasonId = SeasonId.New() };
        var aggregate = new TestAggregate();
        aggregate.Raise(first);
        aggregate.Raise(second);
        aggregate.Raise(third);

        // Act
        publisher.PublishAndClear(aggregate);

        // Assert
        jobs.Select(j => j.Args[0]).ShouldBe([first, second, third]);
        jobs.Select(j => j.Type).ShouldBe(
        [
            typeof(IDomainEventJob<TournamentResultsRecorded>),
            typeof(IDomainEventJob<SquadScoresReplaced>),
            typeof(IDomainEventJob<SeasonStatsGenerated>),
        ]);
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "PublishAndClear should publish nothing on a second call")]
    public void PublishAndClear_ShouldPublishNothing_WhenCalledTwice()
    {
        // Arrange
        var (client, jobs, _) = CreateRecordingClient();
        var publisher = new DomainEventPublisher(client.Object);
        var aggregate = new TestAggregate();
        aggregate.Raise(new TournamentResultsRecorded { TournamentId = TournamentId.New() });
        publisher.PublishAndClear(aggregate);

        // Act
        publisher.PublishAndClear(aggregate);

        // Assert
        jobs.Count.ShouldBe(1);
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "PublishAndClear should create no jobs for an aggregate with no events")]
    public void PublishAndClear_ShouldCreateNoJobs_WhenAggregateHasNoEvents()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var publisher = new DomainEventPublisher(client.Object);
        var aggregate = new TestAggregate();

        // Act
        publisher.PublishAndClear(aggregate);

        // Assert
        client.VerifyNoOtherCalls();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "PublishAndClear keeps every event when Create throws midway, so a later call enqueues the first event again (current behavior; handlers are idempotent)")]
    public void PublishAndClear_WhenCreateThrowsOnSecondEvent_KeepsEventsAndRepublishesFirst()
    {
        // Arrange
        var created = new List<Job>();
        var failOnSecond = true;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns((Job job, IState _) =>
            {
                if (failOnSecond && created.Count == 1)
                {
                    throw new InvalidOperationException("storage unavailable");
                }

                created.Add(job);
                return "job-id";
            });

        var publisher = new DomainEventPublisher(client.Object);
        IDomainEvent first = new TournamentResultsRecorded { TournamentId = TournamentId.New() };
        IDomainEvent second = new TournamentSquadsChanged { TournamentId = TournamentId.New() };
        IDomainEvent third = new SeasonStatsGenerated { SeasonId = SeasonId.New() };
        var aggregate = new TestAggregate();
        aggregate.Raise(first);
        aggregate.Raise(second);
        aggregate.Raise(third);

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => publisher.PublishAndClear(aggregate));
        var afterFailure = created.ConvertAll(j => j.Args[0]);
        var eventsAfterFailure = aggregate.DomainEvents.ToList();

        failOnSecond = false;
        publisher.PublishAndClear(aggregate);

        // Assert
        exception.Message.ShouldBe("storage unavailable");
        afterFailure.ShouldBe([first]);
        eventsAfterFailure.ShouldBe([first, second, third]);
        created.Select(j => j.Args[0]).ShouldBe([first, first, second, third]);
        aggregate.DomainEvents.ShouldBeEmpty();
    }
}