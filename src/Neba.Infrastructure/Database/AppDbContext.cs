using Microsoft.EntityFrameworkCore;

using Neba.Domain.Bowlers;
using Neba.Domain.BowlingCenters;
using Neba.Domain.HallOfFame;
using Neba.Domain.Seasons;
using Neba.Domain.Sponsors;
using Neba.Domain.Stats;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Converters;

using SmartEnum.EFCore;

namespace Neba.Infrastructure.Database;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public const string DefaultSchema = "app";
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public DbSet<BowlingCenter> BowlingCenters
        => Set<BowlingCenter>();

    public DbSet<Bowler> Bowlers
        => Set<Bowler>();

    public DbSet<Tournament> Tournaments
        => Set<Tournament>();

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

        configurationBuilder.Properties<Uri>()
            .HaveConversion<UriToStringConverter>();
    }
}