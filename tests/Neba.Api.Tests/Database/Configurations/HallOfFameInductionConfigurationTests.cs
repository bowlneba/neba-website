using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;
using Neba.Domain.Storage;
using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("HallOfFame")]
public sealed class HallOfFameInductionConfigurationTests
{
    private readonly IEntityType _inductionType;
    private readonly IComplexType _photoType;

    public HallOfFameInductionConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new TestDbContext(options);
        _inductionType = context.Model.FindEntityType(typeof(HallOfFameInduction))!;
        _photoType = _inductionType.FindComplexProperty(nameof(HallOfFameInduction.Photo))!.ComplexType;
    }

    [Fact(DisplayName = "maps to hall_of_fame_inductions table in app schema")]
    public void Configure_ShouldMapToHallOfFameInductionsTable()
    {
        _inductionType.GetTableName().ShouldBe("hall_of_fame_inductions");
        _inductionType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _inductionType.FindProperty(nameof(HallOfFameInduction.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _inductionType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(HallOfFameInduction.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "bowler_id is char(26), not nullable")]
    public void Configure_ShouldConfigureBowlerIdColumn()
    {
        var property = _inductionType.FindProperty(nameof(HallOfFameInduction.BowlerId))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe(BowlerConfiguration.ForeignKeyName);
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "bowler_id foreign key targets Bowler.Id")]
    public void Configure_ShouldConfigureBowlerForeignKey()
    {
        var foreignKey = _inductionType.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == nameof(HallOfFameInduction.BowlerId)));

        foreignKey.ShouldNotBeNull();
        foreignKey!.PrincipalEntityType.ClrType.ShouldBe(typeof(Bowler));
        foreignKey.PrincipalKey.Properties.Select(p => p.Name).ShouldContain(nameof(Bowler.Id));
    }

    [Fact(DisplayName = "induction_year is not nullable")]
    public void Configure_ShouldConfigureYearColumn()
    {
        var property = _inductionType.FindProperty(nameof(HallOfFameInduction.Year))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("induction_year");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "year has an index")]
    public void Configure_ShouldConfigureYearIndex()
    {
        var index = _inductionType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(HallOfFameInduction.Year)));

        index.ShouldNotBeNull();
    }

    [Fact(DisplayName = "category uses HallOfFameCategoryValueConverter and is not nullable")]
    public void Configure_ShouldConfigureCategoriesColumn()
    {
        var property = _inductionType.FindProperty(nameof(HallOfFameInduction.Categories))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("category");
        property.GetValueConverter().ShouldBeOfType<HallOfFameCategoryValueConverter>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "photo columns use custom names and expected lengths")]
    public void Configure_ShouldConfigurePhotoColumns()
    {
        var container = _photoType.FindProperty(nameof(StoredFile.Container))!;
        var path = _photoType.FindProperty(nameof(StoredFile.Path))!;
        var contentType = _photoType.FindProperty(nameof(StoredFile.ContentType))!;
        var sizeInBytes = _photoType.FindProperty(nameof(StoredFile.SizeInBytes))!;

        container.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("photo_container");
        container.GetMaxLength().ShouldBe(63);

        path.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("photo_file_path");
        path.GetMaxLength().ShouldBe(1023);

        contentType.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("photo_content_type");
        contentType.GetMaxLength().ShouldBe(255);

        sizeInBytes.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("photo_size_in_bytes");
    }

    [Fact(DisplayName = "year and bowler_id form an alternate key")]
    public void Configure_ShouldConfigureYearAndBowlerIdAlternateKey()
    {
        var alternateKey = _inductionType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k =>
                k.Properties.Count == 2 &&
                k.Properties.Any(p => p.Name == nameof(HallOfFameInduction.Year)) &&
                k.Properties.Any(p => p.Name == nameof(HallOfFameInduction.BowlerId)));

        alternateKey.ShouldNotBeNull();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bowler>(bowler =>
            {
                bowler.Ignore(x => x.Name);
                bowler.Ignore(x => x.Gender);

                bowler.Property(x => x.Id)
                    .IsUlid<BowlerId, UlidTypedIdConverter<BowlerId>>();

                bowler.HasKey(x => x.Id);
            });

            modelBuilder.ApplyConfiguration(new HallOfFameInductionConfiguration());

            modelBuilder.Entity<HallOfFameInduction>()
                .Property(x => x.Id)
                .HasConversion<UlidTypedIdConverter<HallOfFameId>>();

            modelBuilder.Entity<HallOfFameInduction>()
                .Property(x => x.BowlerId)
                .HasConversion<UlidTypedIdConverter<BowlerId>>();
        }
    }
}