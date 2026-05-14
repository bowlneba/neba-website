using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class SideCutCriteriaConfiguration
    : IEntityTypeConfiguration<SideCutCriteria>
{
    public void Configure(EntityTypeBuilder<SideCutCriteria> builder)
    {
        builder.ToTable("side_cut_criteria", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(criteria => criteria.MinimumAge);

        builder.Property(criteria => criteria.MaximumAge);

        builder.Property(criteria => criteria.GenderRequirement)
            .HasMaxLength(1);

        builder.HasIndex(SideCutCriteriaGroupConfiguration.ForeignKey);
    }
}