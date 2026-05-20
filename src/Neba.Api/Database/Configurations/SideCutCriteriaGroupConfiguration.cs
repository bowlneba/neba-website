using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SideCutCriteriaGroupConfiguration
    : IEntityTypeConfiguration<SideCutCriteriaGroup>
{
    internal const string ForeignKey = "side_cut_criteria_group_id";

    public void Configure(EntityTypeBuilder<SideCutCriteriaGroup> builder)
    {
        builder.ToTable("side_cut_criteria_groups", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(group => group.Id)
            .IsUlid();

        builder.HasAlternateKey(group => group.Id);

        builder.Property(group => group.LogicalOperator)
            .IsRequired();

        builder.Property(group => group.SortOrder)
            .IsRequired();

        builder.HasIndex(SideCutConfiguration.ForeignKey, nameof(SideCutCriteriaGroup.SortOrder))
            .IsUnique();

        builder.HasMany(group => group.Criteria)
            .WithOne(criteria => criteria.CriteriaGroup)
            .HasForeignKey(ForeignKey)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}