using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Entities;

namespace Neba.Api.Database.Configurations;

internal sealed class HistoricalTournamentResultConfiguration
    : IEntityTypeConfiguration<HistoricalTournamentResult>
{
    public void Configure(EntityTypeBuilder<HistoricalTournamentResult> builder)
    {
        // Explicit pk_/ix_ "historical_tournament_results..." names below, mirroring the
        // "app_" prefix on TournamentResultConfiguration: both entity types map to a table
        // named "tournament_results" (different schemas — historical vs. app), and EF's naming
        // convention doesn't scope by schema, so leaving either side unnamed risks one of them
        // silently getting an auto-suffixed name.
        builder.ToTable("tournament_results", AppDbContext.HistoricalSchema);

        builder.HasKey(tournamentResult => new { tournamentResult.TournamentId, tournamentResult.BowlerId })
            .HasName("pk_historical_tournament_results");

        builder.Property(tournamentResult => tournamentResult.Place)
            .IsRequired(false);

        builder.Property(tournamentResult => tournamentResult.Points)
            .IsRequired();

        builder.Property(tournamentResult => tournamentResult.PrizeMoney)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.HasOne(tournamentResult => tournamentResult.Bowler)
            .WithMany()
            .HasForeignKey(tournamentResult => tournamentResult.BowlerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tournamentResult => tournamentResult.BowlerId)
            .HasDatabaseName("ix_historical_tournament_results_bowler_id");

        builder.HasOne(tournamentResult => tournamentResult.Tournament)
            .WithMany()
            .HasForeignKey(tournamentResult => tournamentResult.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tournamentResult => tournamentResult.SideCut)
            .WithMany()
            .HasForeignKey(tournamentResult => tournamentResult.SideCutId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(tournamentResult => tournamentResult.SideCutId)
            .HasDatabaseName("ix_historical_tournament_results_side_cut_id");
    }
}