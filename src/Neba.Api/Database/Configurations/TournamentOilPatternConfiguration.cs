using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Converters;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class TournamentOilPatternConfiguration
    : IEntityTypeConfiguration<TournamentOilPattern>
{
    public void Configure(EntityTypeBuilder<TournamentOilPattern> builder)
    {
        builder.ToTable("tournament_oil_patterns", AppDbContext.DefaultSchema);

        builder.HasKey(TournamentConfiguration.ForeignKeyName, nameof(TournamentOilPattern.OilPatternId));

        builder.Property(tournamentOilPattern => tournamentOilPattern.TournamentRounds)
            .HasColumnName("tournament_rounds")
            .HasConversion<TournamentRoundValueConverter>()
            .IsRequired();
    }
}