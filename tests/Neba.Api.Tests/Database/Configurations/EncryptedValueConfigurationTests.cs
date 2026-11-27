using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database.Configurations;
using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Npgsql;

namespace Neba.Api.Tests.Database.Configurations;

// No production entity has an encrypted value yet, so these tests map their own.
[IntegrationTest]
[Component("Encryption")]
public sealed class EncryptedValueConfigurationTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _database = new();

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _database.DisposeAsync();

    private PersonContext CreateContext()
        => new(new DbContextOptionsBuilder<PersonContext>()
            .UseNpgsql(_database.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options);

    private static EncryptedValue<BowlerPii> Secret(string value, string hash)
        => new(new Encrypted<BowlerPii>(value), new Hashed<BowlerPii>(hash));

    [Fact(DisplayName = "The name should prefix the encrypted and hash columns, with a unique filtered index")]
    public void HasEncryptedValue_ShouldMapColumnsAndIndex()
    {
        // Arrange
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        // Act
        var owned = model.FindEntityType(typeof(Person))!.FindNavigation(nameof(Person.Ssn))!.TargetEntityType;
        var table = StoreObjectIdentifier.Table("people", null);
        var value = owned.FindProperty("Value")!;
        var hash = owned.FindProperty("Hash")!;
        var index = owned.GetIndexes().ShouldHaveSingleItem();

        // Assert
        value.GetColumnName(table).ShouldBe("ssn_encrypted");
        value.GetColumnType().ShouldBe("text");
        hash.GetColumnName(table).ShouldBe("ssn_hash");
        hash.GetMaxLength().ShouldBe(44);
        index.IsUnique.ShouldBeTrue();
        index.Properties.ShouldHaveSingleItem().Name.ShouldBe("Hash");
        index.GetFilter().ShouldBe("ssn_hash IS NOT NULL");
        index.GetDatabaseName().ShouldBe("ix_people_ssn_hash");
    }

    [Fact(DisplayName = "A saved value should load with both parts, and a missing one as null")]
    public async Task HasEncryptedValue_ShouldRoundTrip()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using (var context = CreateContext())
        {
            context.Add(new Person { Id = 1, Ssn = Secret("cipher-1", "hash-1") });
            context.Add(new Person { Id = 2 });
            await context.SaveChangesAsync(ct);
        }

        // Act
        await using var reading = CreateContext();
        var people = await reading.Set<Person>().AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

        // Assert
        people[0].Ssn.ShouldBe(Secret("cipher-1", "hash-1"));
        people[0].Ssn!.ToEncrypted().ShouldBe(new Encrypted<BowlerPii>("cipher-1"));
        people[1].Ssn.ShouldBeNull();
    }

    [Fact(DisplayName = "Two rows should not share a hash, though many may have none")]
    public async Task HasUniqueHash_ShouldRejectADuplicateHash()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = CreateContext();
        context.AddRange(new Person { Id = 1 }, new Person { Id = 2 }, new Person { Id = 3, Ssn = Secret("a", "same") });
        await context.SaveChangesAsync(ct);
        context.Add(new Person { Id = 4, Ssn = Secret("b", "same") });

        // Act
        var act = () => context.SaveChangesAsync(ct);

        // Assert
        var exception = await act.ShouldThrowAsync<DbUpdateException>();
        exception.InnerException.ShouldBeOfType<PostgresException>().ConstraintName.ShouldBe("ix_people_ssn_hash");
    }

    internal sealed class Person
    {
        public int Id { get; set; }

        public EncryptedValue<BowlerPii>? Ssn { get; set; }
    }

    internal sealed class PersonContext(DbContextOptions<PersonContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Person>(person =>
            {
                person.ToTable("people");
                person.Property(p => p.Id).ValueGeneratedNever();
                person.HasEncryptedValue(p => p.Ssn, "ssn").HasUniqueHash();
            });
    }
}