using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Infrastructure.Database.Configurations;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Classes should not be static

internal static class ShadowIdConfiguration
{
    public const string DefaultPropertyName = "db_id";

    public const string DefaultColumnName = "id";

    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        public void ConfigureShadowId(
            string propertyName = DefaultPropertyName,
            string columnName = DefaultColumnName)
        {

            builder.Property<int>(propertyName)
                .HasColumnName(columnName)
                .UseIdentityAlwaysColumn();

            builder.HasKey(propertyName);
        }
    }
}
