using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Infrastructure.Database.Entities;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class HistoricalTournamentChampionsConfiguration
    : IEntityTypeConfiguration<HistoricalTournamentChampion>
{
    public void Configure(EntityTypeBuilder<HistoricalTournamentChampion> builder)
    {
        builder.ToTable("tournament_champions", AppDbContext.HistoricalSchema);

        builder.HasKey(tournamentChampion => new { tournamentChampion.TournamentId, tournamentChampion.BowlerId });

        builder.HasOne(tournamentChampion => tournamentChampion.Bowler)
            .WithMany()
            .HasForeignKey(tournamentChampion => tournamentChampion.BowlerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tournamentChampion => tournamentChampion.Tournament)
            .WithMany()
            .HasForeignKey(tournamentChampion => tournamentChampion.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}