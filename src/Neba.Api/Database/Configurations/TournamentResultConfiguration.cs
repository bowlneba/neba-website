using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class TournamentResultConfiguration : IEntityTypeConfiguration<TournamentResult>
{
    public void Configure(EntityTypeBuilder<TournamentResult> builder)
    {
        // Explicit names below (pk_/ak_/ix_ "app_tournament_results...") avoid colliding with
        // Neba.Api.Database.Entities.HistoricalTournamentResultConfiguration, which explicitly
        // names its own constraints/index "historical_tournament_results..." for the same reason:
        // both entity types map to a table named "tournament_results" (different schemas), and
        // EF's naming convention doesn't scope by schema, so leaving either side unnamed risks a
        // silently auto-suffixed name.
        builder.ToTable("tournament_results", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();
        builder.HasKey(ShadowIdConfiguration.DefaultPropertyName)
            .HasName("pk_app_tournament_results");

        builder.Property(result => result.Id)
            .IsUlid();

        builder.HasAlternateKey(result => result.Id)
            .HasName("ak_app_tournament_results_domain_id");

        builder.Property<int>(TournamentConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Tournament>()
            .WithMany(tournament => tournament.Results)
            .HasForeignKey(TournamentConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(result => result.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(result => result.Bowler)
            .WithMany()
            .HasForeignKey(result => result.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(result => result.BowlerId)
            .HasDatabaseName("ix_app_tournament_results_bowler_id");

        builder.Property(result => result.Place)
            .IsRequired();

        builder.Property(result => result.PrizeMoney)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(result => result.Points)
            .IsRequired();

        builder.HasAlternateKey(TournamentConfiguration.ForeignKeyName, nameof(TournamentResult.BowlerId))
            .HasName("ak_app_tournament_results_tournament_id_bowler_id");
    }
}