using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Api.Database.Configurations;

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