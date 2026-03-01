using Microsoft.EntityFrameworkCore;

namespace Neba.Infrastructure.Database;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // configurationBuilder.Properties<BowlingCenterId>()
        //     .HaveConversion<UlidTypedIdConverter<BowlingCenterId>>(); //this is a sample of how to convert typed IDs to and from their string representation in the database. Uncomment and adjust as needed for your actual typed ID types.
    }
}