using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class TournamentResultConfiguration : IEntityTypeConfiguration<TournamentResult>
{
    public void Configure(EntityTypeBuilder<TournamentResult> builder)
    {
        builder.ToTable("tournament_results", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(result => result.Id)
            .IsUlid();

        builder.HasAlternateKey(result => result.Id);

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

        builder.Property(result => result.Place)
            .IsRequired();

        builder.Property(result => result.PrizeMoney)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(result => result.Points)
            .IsRequired();

        builder.HasAlternateKey(TournamentConfiguration.ForeignKeyName, nameof(TournamentResult.BowlerId));
    }
}