using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class TournamentSponsorConfiguration
    : IEntityTypeConfiguration<TournamentSponsor>
{
    public void Configure(EntityTypeBuilder<TournamentSponsor> builder)
    {
        builder.ToTable("tournament_sponsors", AppDbContext.DefaultSchema);

        builder.Property<int>(TournamentConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(tournamentSponsor => tournamentSponsor.SponsorId)
            .IsUlid(SponsorConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(tournamentSponsor => tournamentSponsor.TitleSponsor)
            .HasColumnName("is_title_sponsor")
            .IsRequired();

        builder.Property(tournamentSponsor => tournamentSponsor.SponsorshipAmount)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.HasKey(TournamentConfiguration.ForeignKeyName, nameof(TournamentSponsor.SponsorId));
    }
}