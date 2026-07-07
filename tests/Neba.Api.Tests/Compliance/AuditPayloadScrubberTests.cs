using Neba.Api.Compliance;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Compliance;

[UnitTest]
[Component("Compliance")]
public sealed class AuditPayloadScrubberTests
{
#nullable disable
    [Fact(DisplayName = "Scrub should throw when source is null")]
    public void Scrub_ShouldThrow_WhenSourceIsNull()
    {
        // Arrange
        TestPayload source = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => AuditPayloadScrubber.Scrub(source));
    }
#nullable enable

    [Fact(DisplayName = "Scrub should pass through public and unclassified properties unchanged")]
    public void Scrub_ShouldPassThroughValue_WhenPropertyIsPublicOrUnclassified()
    {
        // Arrange
        var source = new TestPayload
        {
            PublicName = "NEBA Tournament",
            UnclassifiedCount = 42,
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(TestPayload.PublicName)].ShouldBe("NEBA Tournament");
        result[nameof(TestPayload.UnclassifiedCount)].ShouldBe(42);
    }

    [Fact(DisplayName = "Scrub should omit private properties entirely")]
    public void Scrub_ShouldOmitProperty_WhenPropertyIsPrivateData()
    {
        // Arrange
        var source = new TestPayload
        {
            PrivateSsn = "123-45-6789",
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result.ShouldNotContainKey(nameof(TestPayload.PrivateSsn));
    }

    [Fact(DisplayName = "Scrub should star-mask personal string properties")]
    public void Scrub_ShouldMaskValue_WhenPropertyIsPersonalDataAndStringValued()
    {
        // Arrange
        var source = new TestPayload
        {
            PersonalEmail = "bowler@example.com",
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(TestPayload.PersonalEmail)].ShouldBe("b*****************");
    }

    [Fact(DisplayName = "Scrub should pass through empty string unchanged for personal properties")]
    public void Scrub_ShouldReturnEmptyString_WhenPersonalPropertyValueIsEmpty()
    {
        // Arrange
        var source = new TestPayload
        {
            PersonalEmail = string.Empty,
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(TestPayload.PersonalEmail)].ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Scrub should pass through null unchanged for personal properties")]
    public void Scrub_ShouldReturnNull_WhenPersonalPropertyValueIsNull()
    {
        // Arrange
        var source = new TestPayload
        {
            PersonalEmail = null,
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(TestPayload.PersonalEmail)].ShouldBeNull();
    }

    [Fact(DisplayName = "Scrub should pass through non-string personal-data values unmasked")]
    public void Scrub_ShouldPassThroughValue_WhenPersonalPropertyIsNotAString()
    {
        // Arrange
        var source = new TestPayload
        {
            PersonalAge = 34,
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(TestPayload.PersonalAge)].ShouldBe(34);
    }

    [Fact(DisplayName = "Scrub should recursively scrub nested complex properties")]
    public void Scrub_ShouldRecursivelyScrubNestedProperties_WhenPropertyIsComplexType()
    {
        // Arrange
        var source = new OuterPayload
        {
            Input = new TestPayload
            {
                PublicName = "NEBA Tournament",
                PrivateSsn = "123-45-6789",
                PersonalEmail = "bowler@example.com",
            },
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        var nested = result[nameof(OuterPayload.Input)].ShouldBeOfType<Dictionary<string, object?>>();
        nested[nameof(TestPayload.PublicName)].ShouldBe("NEBA Tournament");
        nested.ShouldNotContainKey(nameof(TestPayload.PrivateSsn));
        nested[nameof(TestPayload.PersonalEmail)].ShouldBe("b*****************");
    }

    [Fact(DisplayName = "Scrub should recursively scrub complex properties inside a collection")]
    public void Scrub_ShouldRecursivelyScrubCollectionItems_WhenPropertyIsCollectionOfComplexType()
    {
        // Arrange
        var source = new CollectionPayload
        {
            Items =
            [
                new TestPayload { PrivateSsn = "123-45-6789", PersonalEmail = "bowler@example.com" },
            ],
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        var items = result[nameof(CollectionPayload.Items)].ShouldBeOfType<List<object?>>();
        var nested = items[0].ShouldBeOfType<Dictionary<string, object?>>();
        nested.ShouldNotContainKey(nameof(TestPayload.PrivateSsn));
        nested[nameof(TestPayload.PersonalEmail)].ShouldBe("b*****************");
    }

    [Fact(DisplayName = "Scrub should not recurse infinitely when the object graph is circular")]
    public void Scrub_ShouldNotThrow_WhenObjectGraphIsCircular()
    {
        // Arrange
        var source = new CircularPayload();
        source.Self = source;

        // Act & Assert
        Should.NotThrow(() => AuditPayloadScrubber.Scrub(source));
    }

    [Fact(DisplayName = "Scrub should cache reflected properties across repeated calls for the same type")]
    public void Scrub_ShouldProduceConsistentResults_WhenCalledMultipleTimesForSameType()
    {
        // Arrange
        var first = new TestPayload { PublicName = "First" };
        var second = new TestPayload { PublicName = "Second" };

        // Act
        var firstResult = AuditPayloadScrubber.Scrub(first);
        var secondResult = AuditPayloadScrubber.Scrub(second);

        // Assert
        firstResult[nameof(TestPayload.PublicName)].ShouldBe("First");
        secondResult[nameof(TestPayload.PublicName)].ShouldBe("Second");
    }

    [Fact(DisplayName = "Scrub should omit ApplicationUser's private identity properties")]
    public void Scrub_ShouldOmitPrivateProperties_WhenSourceIsApplicationUser()
    {
        // Arrange
        var source = new ApplicationUser
        {
            PasswordHash = "hashed-password",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = "concurrency-stamp",
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result.ShouldNotContainKey(nameof(ApplicationUser.PasswordHash));
        result.ShouldNotContainKey(nameof(ApplicationUser.SecurityStamp));
        result.ShouldNotContainKey(nameof(ApplicationUser.ConcurrencyStamp));
    }

    [Fact(DisplayName = "Scrub should star-mask ApplicationUser's personal identity properties")]
    public void Scrub_ShouldMaskPersonalProperties_WhenSourceIsApplicationUser()
    {
        // Arrange
        var source = new ApplicationUser
        {
            Email = "bowler@example.com",
            PhoneNumber = "555-123-4567",
            UserName = "bowler",
        };

        // Act
        var result = AuditPayloadScrubber.Scrub(source);

        // Assert
        result[nameof(ApplicationUser.Email)].ShouldBe("b*****************");
        result[nameof(ApplicationUser.PhoneNumber)].ShouldBe("5***********");
        result[nameof(ApplicationUser.UserName)].ShouldBe("b*****");
    }

    private sealed class TestPayload
    {
        [PublicData]
        public string? PublicName { get; set; }

        public int UnclassifiedCount { get; set; }

        [PrivateData]
        public string? PrivateSsn { get; set; }

        [PersonalData]
        public string? PersonalEmail { get; set; }

        [PersonalData]
        public int PersonalAge { get; set; }
    }

    private sealed class OuterPayload
    {
        public TestPayload? Input { get; set; }
    }

    private sealed class CollectionPayload
    {
        public List<TestPayload> Items { get; set; } = [];
    }

    private sealed class CircularPayload
    {
        public CircularPayload? Self { get; set; }
    }
}