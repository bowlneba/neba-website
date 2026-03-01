using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.BowlingCenters;
using Neba.Domain.Contact;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class BowlingCenterConfiguration
    : IEntityTypeConfiguration<BowlingCenter>
{
    public const string ForeignKeyName = "bowling_center_id";

    internal static class QueryFilters
    {
        internal const string OpenCentersFilter = "OpenCentersFilter";
    }

    public void Configure(EntityTypeBuilder<BowlingCenter> builder)
    {
        builder.ToTable("bowling_centers", AppDbContext.DefaultSchema);

        builder.HasQueryFilter(QueryFilters.OpenCentersFilter,
            bowlingCenter => bowlingCenter.Status == BowlingCenterStatus.Open);

        builder.ConfigureShadowId();

        builder.Property(bowlingCenter => bowlingCenter.CertificationNumber)
            .HasColumnName("certification_number")
            .HasMaxLength(6)
            .HasConversion(
                certificationNumber => certificationNumber.Value,
                value => new CertificationNumber { Value = value })
            .IsRequired();

        builder.HasAlternateKey(bowlingCenter => bowlingCenter.CertificationNumber);

        builder.Property(bowlingCenter => bowlingCenter.Name)
            .HasColumnName("name")
            .HasMaxLength(127)
            .IsRequired();

        builder.HasAddress(bowlingCenter => bowlingCenter.Address, address =>
        {
            address.ComplexProperty(a => a.Coordinates)
                .IsRequired();
        });

        builder.OwnsMany(bowlingCenter => bowlingCenter.PhoneNumbers, phoneNumbers =>
        {
            phoneNumbers.ToTable("bowling_center_phone_numbers", AppDbContext.DefaultSchema);
            phoneNumbers.WithOwner().HasForeignKey(ForeignKeyName);
            phoneNumbers.HasKey(ForeignKeyName, nameof(PhoneNumber.Type));
            phoneNumbers.WithPhoneNumbers();
        });

        builder.HasEmailAddress(bowlingCenter => bowlingCenter.EmailAddress);

        builder.Property(bowlingCenter => bowlingCenter.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(bowlingCenter => bowlingCenter.Website)
            .HasColumnName("website")
            .HasMaxLength(255)
            .HasConversion(
                website => website!.ToString(),
                urlString => new Uri(urlString));

        builder.Property(bowlingCenter => bowlingCenter.WebsiteId)
            .ValueGeneratedNever();

        builder.HasIndex(bowlingCenter => bowlingCenter.WebsiteId)
            .IsUnique()
            .AreNullsDistinct();

        builder.Property(bowlingCenter => bowlingCenter.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(bowlingCenter => bowlingCenter.LegacyId)
            .IsUnique()
            .AreNullsDistinct();

        builder.OwnsOne(bowlingCenter => bowlingCenter.Lanes, lanes =>
        {
            lanes.OwnsMany(lanes => lanes.Ranges, laneRanges =>
            {
                laneRanges.ToTable("bowling_center_lanes", AppDbContext.DefaultSchema);
                laneRanges.WithOwner().HasForeignKey(ForeignKeyName);
                laneRanges.HasKey(ForeignKeyName, nameof(LaneRange.StartLane));

                laneRanges.Property(range => range.StartLane)
                    .HasColumnName("start_lane")
                    .ValueGeneratedNever()
                    .IsRequired();

                laneRanges.Property(range => range.EndLane)
                    .HasColumnName("end_lane")
                    .IsRequired();

                laneRanges.Property(range => range.PinFallType)
                    .HasColumnName("pin_fall_type")
                    .HasMaxLength(2)
                    .IsFixedLength()
                    .IsRequired();
            });
        });
    }
}