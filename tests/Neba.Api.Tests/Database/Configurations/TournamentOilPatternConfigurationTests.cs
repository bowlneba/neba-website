using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Converters;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentOilPatternConfigurationTests
{
    private readonly IEntityType _tournamentOilPatternType;

    public TournamentOilPatternConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new TestDbContext(options);
        _tournamentOilPatternType = context.Model.FindEntityType(typeof(TournamentOilPattern))!;
    }

    [Fact(DisplayName = "maps to tournament_oil_patterns table in app schema")]
    public void Configure_ShouldMapToTournamentOilPatternsTable()
    {
        // Act & Assert
        _tournamentOilPatternType.GetTableName().ShouldBe("tournament_oil_patterns");
        _tournamentOilPatternType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "tournament_id and oil_pattern_id form the composite primary key")]
    public void Configure_ShouldConfigureCompositePrimaryKey()
    {
        // Act
        var primaryKey = _tournamentOilPatternType.FindPrimaryKey()!;

        // Assert
        primaryKey.Properties.Select(p => p.Name).ShouldBe(
            [TournamentConfiguration.ForeignKeyName, nameof(TournamentOilPattern.OilPatternId)],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "tournament_rounds uses TournamentRoundValueConverter and is not nullable")]
    public void Configure_ShouldConfigureTournamentRoundsColumn()
    {
        // Act
        var property = _tournamentOilPatternType.FindProperty(nameof(TournamentOilPattern.TournamentRounds))!;

        // Assert
        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("tournament_rounds");
        property.GetValueConverter().ShouldBeOfType<TournamentRoundValueConverter>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "tournament_rounds has a value comparer that compares elements rather than reference")]
    public void Configure_ShouldConfigureTournamentRoundsValueComparer()
    {
        // Arrange
        var property = _tournamentOilPatternType.FindProperty(nameof(TournamentOilPattern.TournamentRounds))!;
        var comparer = property.GetValueComparer();

        IReadOnlyCollection<TournamentRound> first = [TournamentRound.Qualifying, TournamentRound.MatchPlay];
        IReadOnlyCollection<TournamentRound> second = [TournamentRound.Qualifying, TournamentRound.MatchPlay];
        IReadOnlyCollection<TournamentRound> different = [TournamentRound.Cashers];

        // Act & Assert
        comparer.ShouldNotBeNull();
        comparer.Equals(first, second).ShouldBeTrue();
        comparer.Equals(first, different).ShouldBeFalse();
        comparer.GetHashCode(first).ShouldBe(comparer.GetHashCode(second));
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tournament>(tournament =>
            {
                tournament.Ignore(x => x.BowlingCenter);
                tournament.Ignore(x => x.BowlingCenterId);
                tournament.Ignore(x => x.Season);
                tournament.Ignore(x => x.SeasonId);
                tournament.Ignore(x => x.Sponsors);
                tournament.Ignore(x => x.Squads);
                tournament.Ignore(x => x.Articles);
                tournament.Ignore(x => x.Results);
                tournament.Ignore(x => x.Logo);
                tournament.Ignore(x => x.PatternLengthCategory);
                tournament.Ignore(x => x.PatternRatioCategory);
                tournament.Ignore(x => x.TournamentType);
                tournament.Ignore(x => x.ExternalRegistrationUrl);

                tournament.Property(x => x.Id)
                    .HasConversion<UlidTypedIdConverter<TournamentId>>();

                tournament.HasKey(x => x.Id);

                tournament.HasMany(x => x.OilPatterns)
                    .WithOne(tournamentOilPattern => tournamentOilPattern.Tournament)
                    .HasForeignKey(TournamentConfiguration.ForeignKeyName)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OilPattern>(oilPattern =>
            {
                oilPattern.Property(x => x.Id)
                    .HasConversion<UlidTypedIdConverter<OilPatternId>>();

                oilPattern.HasKey(x => x.Id);

                oilPattern.HasMany(x => x.Tournaments)
                    .WithOne(tournamentOilPattern => tournamentOilPattern.OilPattern)
                    .HasPrincipalKey(pattern => pattern.Id)
                    .HasForeignKey(tournamentOilPattern => tournamentOilPattern.OilPatternId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.ApplyConfiguration(new TournamentOilPatternConfiguration());
        }
    }
}