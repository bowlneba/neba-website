using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Entities;

namespace Neba.Api.Database.Configurations;

internal sealed class HistoricalTournamentEntryConfiguration
    : IEntityTypeConfiguration<HistoricalTournamentEntry>
{
    public void Configure(EntityTypeBuilder<HistoricalTournamentEntry> builder)
    {
        builder.ToTable("tournament_entries", AppDbContext.HistoricalSchema);

        builder.HasKey(tournamentEntries => tournamentEntries.TournamentId);

        builder.HasOne(tournamentEntries => tournamentEntries.Tournament)
            .WithMany()
            .HasForeignKey(tournamentEntries => tournamentEntries.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}