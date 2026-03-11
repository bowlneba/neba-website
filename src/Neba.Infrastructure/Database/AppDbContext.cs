using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BowlingCenterConfiguration());
        modelBuilder.ApplyConfiguration(new BowlerConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSmartEnum();

        configurationBuilder.Properties<BowlerId>()
            .HaveConversion<UlidTypedIdConverter<BowlerId>>();

        configurationBuilder.Properties<HallOfFameId>()
            .HaveConversion<UlidTypedIdConverter<HallOfFameId>>();
    }
}