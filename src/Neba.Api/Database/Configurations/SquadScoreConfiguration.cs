using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SquadScoreConfiguration : IEntityTypeConfiguration<SquadScore>
{
    public void Configure(EntityTypeBuilder<SquadScore> builder)
    {
        builder.ToTable("squad_scores", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(squadScore => squadScore.Id)
            .IsUlid();

        builder.HasAlternateKey(squadScore => squadScore.Id);

        builder.Property(squadScore => squadScore.SquadId)
            .IsUlid(SquadConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(squadScore => squadScore.Squad)
            .WithMany()
            .HasForeignKey(squadScore => squadScore.SquadId)
            .HasPrincipalKey(squad => squad.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squadScore => squadScore.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(squadScore => squadScore.Bowler)
            .WithMany()
            .HasForeignKey(squadScore => squadScore.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squadScore => squadScore.GameNumber)
            .HasColumnName("game_number")
            .IsRequired();

        builder.Property(squadScore => squadScore.Score)
            .HasColumnName("score")
            .IsRequired();

        builder.HasAlternateKey(squadScore => new
        {
            squadScore.SquadId,
            squadScore.BowlerId,
            squadScore.GameNumber
        });
    }
}