using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Seasons;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class SeasonConfiguration
    : IEntityTypeConfiguration<Season>
{
    internal const string ForeignKeyName = "season_id";

    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(season => season.Id)
            .IsUlid();

        builder.HasAlternateKey(season => season.Id);

        builder.Property(season => season.Description)
            .HasMaxLength(31)
            .IsRequired();

        builder.Property(season => season.StartDate)
            .IsRequired();

        builder.Property(season => season.EndDate)
            .IsRequired();

        builder.Property(season => season.Complete)
            .IsRequired();
    }
}