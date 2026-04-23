using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Infrastructure.Database.Entities;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class HistoricalTournamentEntriesConfiguration
    : IEntityTypeConfiguration<HistoricalTournamentEntries>
{
    public void Configure(EntityTypeBuilder<HistoricalTournamentEntries> builder)
    {
        builder.ToTable("tournament_entries", AppDbContext.HistoricalSchema);

        builder.HasKey(tournamentEntries => tournamentEntries.TournamentId);

        builder.HasOne(tournamentEntries => tournamentEntries.Tournament)
            .WithMany()
            .HasForeignKey(tournamentEntries => tournamentEntries.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}