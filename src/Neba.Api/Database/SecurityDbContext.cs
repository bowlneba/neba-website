using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database.Converters;
using Neba.Api.Security.Domain;

namespace Neba.Api.Database;

internal sealed class SecurityDbContext(DbContextOptions<SecurityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Ulid>(options)
{
    public const string Schema = "security";
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Ulid>()
            .HaveConversion<UlidConverter>()
            .HaveMaxLength(26)
            .AreFixedLength();
    }
}