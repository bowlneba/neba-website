using System.Linq.Expressions;

using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Contact;

namespace Neba.Infrastructure.Database.Configurations;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Unused private types or members should be static

internal static class AddressConfiguration
{
    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        public EntityTypeBuilder<T> HasAddress(
            Expression<Func<T, Address?>> addressExpression,
            Action<ComplexPropertyBuilder<Address>>? configureAddress = null)
        {
            return builder.ComplexProperty(addressExpression, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("street")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.Unit)
                    .HasColumnName("unit")
                    .HasMaxLength(50);

                address.Property(a => a.City)
                    .HasColumnName("city")
                    .HasMaxLength(50)
                    .IsRequired();

                address.Property(a => a.Region)
                    .HasColumnName("region")
                    .HasMaxLength(2)
                    .IsFixedLength()
                    .IsRequired();

                address.Property(a => a.Country)
                    .HasColumnName("country")
                    .HasConversion<SmartEnumConverter<Country, string>>()
                    .HasMaxLength(2)
                    .IsFixedLength()
                    .IsRequired();

                address.Property(a => a.PostalCode)
                    .HasColumnName("postal_code")
                    .HasMaxLength(10)
                    .IsRequired();

                address.ComplexProperty(a => a.Coordinates, coordinates =>
                {
                    coordinates.Property(c => c.Latitude)
                        .HasColumnName("latitude")
                        .IsRequired();

                    coordinates.Property(c => c.Longitude)
                        .HasColumnName("longitude")
                        .IsRequired();
                });

                // Allow overriding defaults and additional configuration (e.g., indexes)
                configureAddress?.Invoke(address);
            });
        }
    }
}
