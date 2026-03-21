using Microsoft.EntityFrameworkCore;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;
using Neba.Domain.BowlingCenters;
using Neba.Domain.HallOfFame;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Converters;

using SmartEnum.EFCore;

namespace Neba.Infrastructure.Database;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public const string DefaultSchema = "app";
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public DbSet<BowlingCenter> BowlingCenters
        => Set<BowlingCenter>();

    public DbSet<Bowler> Bowlers
        => Set<Bowler>();

    public DbSet<HallOfFameInduction> HallOfFameInductions
        => Set<HallOfFameInduction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BowlingCenterConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerConfiguration());
        modelBuilder.ApplyConfiguration(new HallOfFameInductionConfiguration());
        modelBuilder.ApplyConfiguration(new SeasonConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerOfTheYearAwardConfiguration());
        modelBuilder.ApplyConfiguration(new HighAverageAwardConfiguration());
        modelBuilder.ApplyConfiguration(new HighBlockAwardConfiguration());
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
    }
}