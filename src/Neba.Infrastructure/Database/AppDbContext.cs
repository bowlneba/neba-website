using System.Drawing;

using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;

using Neba.Domain;
using Neba.Domain.Bowlers;
using Neba.Domain.BowlingCenters;
using Neba.Domain.HallOfFame;
using Neba.Domain.Seasons;
using Neba.Domain.Sponsors;
using Neba.Domain.Stats;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Converters;
using Neba.Infrastructure.Database.Entities;

using SmartEnum.EFCore;

namespace Neba.Infrastructure.Database;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public const string DefaultSchema = "app";
    public const string HistoricalSchema = "historical";

    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public DbSet<BowlingCenter> BowlingCenters
        => Set<BowlingCenter>();

    public DbSet<Bowler> Bowlers
        => Set<Bowler>();

    public DbSet<Tournament> Tournaments
        => Set<Tournament>();

    internal DbSet<HistoricalTournamentChampion> HistoricalTournamentChampions
        => Set<HistoricalTournamentChampion>();

    internal DbSet<HistoricalTournamentEntries> HistoricalTournamentEntries
        => Set<HistoricalTournamentEntries>();

    public DbSet<HallOfFameInduction> HallOfFameInductions
        => Set<HallOfFameInduction>();

    public DbSet<Season> Seasons
        => Set<Season>();

    public DbSet<Sponsor> Sponsors
        => Set<Sponsor>();

    public DbSet<BowlerSeasonStats> BowlerSeasonStats
        => Set<BowlerSeasonStats>();

    public DbSet<OilPattern> OilPatterns
        => Set<OilPattern>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BowlingCenterConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerConfiguration());
        modelBuilder.ApplyConfiguration(new HallOfFameInductionConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerOfTheYearAwardConfiguration());
        modelBuilder.ApplyConfiguration(new HighAverageAwardConfiguration());
        modelBuilder.ApplyConfiguration(new HighBlockAwardConfiguration());
        modelBuilder.ApplyConfiguration(new SponsorConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerSeasonStatsConfiguration());
        modelBuilder.ApplyConfiguration(new OilPatternConfiguration());
        modelBuilder.ApplyConfiguration(new TournamentConfiguration());
        modelBuilder.ApplyConfiguration(new TournamentSponsorConfiguration());
        modelBuilder.ApplyConfiguration(new TournamentOilPatternConfiguration());
        modelBuilder.ApplyConfiguration(new SideCutConfiguration());
        modelBuilder.ApplyConfiguration(new SideCutCriteriaGroupConfiguration());
        modelBuilder.ApplyConfiguration(new SideCutCriteriaConfiguration());

        modelBuilder.ApplyConfiguration(new HistoricalTournamentChampionsConfiguration());
        modelBuilder.ApplyConfiguration(new HistoricalTournamentEntriesConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSmartEnum();

        configurationBuilder.Properties<BowlerId>()
            .HaveConversion<UlidTypedIdConverter<BowlerId>>();

        configurationBuilder.Properties<HallOfFameId>()
            .HaveConversion<UlidTypedIdConverter<HallOfFameId>>();

        configurationBuilder.Properties<SeasonId>()
            .HaveConversion<UlidTypedIdConverter<SeasonId>>();

        configurationBuilder.Properties<SeasonAwardId>()
            .HaveConversion<UlidTypedIdConverter<SeasonAwardId>>();

        configurationBuilder.Properties<SponsorId>()
            .HaveConversion<UlidTypedIdConverter<SponsorId>>();

        configurationBuilder.Properties<OilPatternId>()
            .HaveConversion<UlidTypedIdConverter<OilPatternId>>();

        configurationBuilder.Properties<TournamentId>()
            .HaveConversion<UlidTypedIdConverter<TournamentId>>();

        configurationBuilder.Properties<SideCutId>()
            .HaveConversion<UlidTypedIdConverter<SideCutId>>();

        configurationBuilder.Properties<SideCutCriteriaGroupId>()
            .HaveConversion<UlidTypedIdConverter<SideCutCriteriaGroupId>>();

        configurationBuilder.Properties<Uri>()
            .HaveConversion<UriToStringConverter>();

        configurationBuilder.Properties<Color>()
            .HaveConversion<Converters.ColorConverter>();

        configurationBuilder.Properties<LogicalOperator>()
            .HaveConversion<SmartEnumConverter<LogicalOperator, string>>()
            .HaveMaxLength(7);

        configurationBuilder.Properties<Gender>()
            .HaveConversion<SmartEnumConverter<Gender, string>>()
            .HaveMaxLength(1);
    }
}