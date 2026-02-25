using Microsoft.EntityFrameworkCore;

using Neba.Domain.BowlingCenters;
using Neba.Infrastructure.Database.Converters;

namespace Neba.Infrastructure.Database;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<BowlingCenterId>()
            .HaveConversion<UlidTypedIdConverter<BowlingCenterId>>();
    }
}