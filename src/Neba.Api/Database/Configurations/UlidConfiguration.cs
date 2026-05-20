using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Api.Database.Configurations;

internal static class UlidConfiguration
{
    extension<TId>(PropertyBuilder<TId> propertyBuilder)
        where TId : struct
    {
        public PropertyBuilder<TId> IsUlid<TEfCoreValueConverter>(
            string columnName = "domain_id")
        {
            return propertyBuilder
                .IsUlid(columnName)
                .HasConversion<TEfCoreValueConverter>();
        }

        public PropertyBuilder<TId> IsUlid(
            string columnName = "domain_id")
        {
            return propertyBuilder
                .HasColumnName(columnName)
                .HasMaxLength(26)
                .IsFixedLength()
                .ValueGeneratedNever();
        }
    }
}