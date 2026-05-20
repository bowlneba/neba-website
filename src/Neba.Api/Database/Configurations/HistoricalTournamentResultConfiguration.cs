using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Entities;

namespace Neba.Api.Database.Configurations;

internal sealed class HistoricalTournamentResultConfiguration
    : IEntityTypeConfiguration<HistoricalTournamentResult>
{
    public void Configure(EntityTypeBuilder<HistoricalTournamentResult> builder)
    {
        builder.ToTable("tournament_results", AppDbContext.HistoricalSchema);

        builder.HasKey(tournamentResult => new { tournamentResult.TournamentId, tournamentResult.BowlerId });

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

        builder.HasOne(tournamentResult => tournamentResult.Tournament)
            .WithMany()
            .HasForeignKey(tournamentResult => tournamentResult.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tournamentResult => tournamentResult.SideCut)
            .WithMany()
            .HasForeignKey(tournamentResult => tournamentResult.SideCutId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}