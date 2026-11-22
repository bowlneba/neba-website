using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesPositionConfiguration
    : IEntityTypeConfiguration<SeriesPosition>
{
    public void Configure(EntityTypeBuilder<SeriesPosition> builder)
    {
        builder.ToTable("series_positions", AppDbContext.DefaultSchema, table =>
            table.HasCheckConstraint("ck_series_positions_position_positive", "position > 0"));

        // series_id int -> series.id, cascade (set in SeriesConfiguration).
        builder.Property<int>(SeriesConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(position => position.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasKey(SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.BowlerId));

        builder.HasOne(position => position.Bowler)
            .WithMany()
            .HasForeignKey(position => position.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(position => position.BowlerId);

        builder.Property(position => position.Position)
            .IsRequired();

        builder.Property(position => position.Bowled)
            .IsRequired();

        builder.Property(position => position.IsFinalist);

        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.Position));
    }
}