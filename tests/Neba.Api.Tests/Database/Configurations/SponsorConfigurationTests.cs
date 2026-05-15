using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Database.Interceptors;
using Neba.Api.Database.Options;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Configurations;

[UnitTest]
[Component("Sponsors")]
public sealed class SponsorConfigurationTests
{
    private readonly IEntityType _sponsorType;
    private readonly IEntityType _phoneNumberType;

    public SponsorConfigurationTests()
    {
        var interceptor = new SlowQueryInterceptor(NullLogger<SlowQueryInterceptor>.Instance, new SlowQueryOptions());
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(interceptor)
            .Options;

        using var context = new AppDbContext(options);
        _sponsorType = context.Model.FindEntityType(typeof(Sponsor))!;
        _phoneNumberType = _sponsorType
            .FindNavigation(nameof(Sponsor.PhoneNumbers))!.TargetEntityType;
    }

    [Fact(DisplayName = "maps to sponsors table in app schema")]
    public void Configure_ShouldMapToSponsorsTable()
    {
        _sponsorType.GetTableName().ShouldBe("sponsors");
        _sponsorType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "domain_id is char(26), not nullable, value generated never")]
    public void Configure_ShouldConfigureDomainIdColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Id))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("domain_id");
        property.GetMaxLength().ShouldBe(26);
        property.IsFixedLength().ShouldBe(true);
        property.ValueGenerated.ShouldBe(ValueGenerated.Never);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "domain_id is an alternate key")]
    public void Configure_ShouldConfigureDomainIdAsAlternateKey()
    {
        var alternateKey = _sponsorType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Sponsor.Id)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "name is varchar(63), not nullable")]
    public void Configure_ShouldConfigureNameColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Name))!;

        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "slug is varchar(63), not nullable")]
    public void Configure_ShouldConfigureSlugColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Slug))!;

        property.GetMaxLength().ShouldBe(63);
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "slug is an alternate key")]
    public void Configure_ShouldConfigureSlugAsAlternateKey()
    {
        var alternateKey = _sponsorType.GetKeys()
            .Where(k => !k.IsPrimaryKey())
            .FirstOrDefault(k => k.Properties.Any(p => p.Name == nameof(Sponsor.Slug)));

        alternateKey.ShouldNotBeNull();
    }

    [Fact(DisplayName = "current_sponsor maps to current_sponsor column, not nullable")]
    public void Configure_ShouldConfigureIsCurrentSponsorColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.IsCurrentSponsor))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("current_sponsor");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "priority is not nullable")]
    public void Configure_ShouldConfigurePriorityColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Priority))!;

        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "tier uses SmartEnumConverter, not nullable")]
    public void Configure_ShouldConfigureTierColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Tier))!;

        property.GetValueConverter().ShouldBeOfType<SmartEnumConverter<SponsorTier, int>>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "category uses SmartEnumConverter, not nullable")]
    public void Configure_ShouldConfigureCategoryColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Category))!;

        property.GetValueConverter().ShouldBeOfType<SmartEnumConverter<SponsorCategory, int>>();
        property.IsNullable.ShouldBeFalse();
    }

    [Fact(DisplayName = "website_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureWebsiteUrlColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.WebsiteUrl))!;

        property.FindAnnotation(RelationalAnnotationNames.ColumnName)!.Value.ShouldBe("website_url");
        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "tag_phrase is varchar(255), nullable")]
    public void Configure_ShouldConfigureTagPhraseColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.TagPhrase))!;

        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "description is varchar(1023), nullable")]
    public void Configure_ShouldConfigureDescriptionColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.Description))!;

        property.GetMaxLength().ShouldBe(1023);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "live_read_text is varchar(2047), nullable")]
    public void Configure_ShouldConfigureLiveReadTextColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.LiveReadText))!;

        property.GetMaxLength().ShouldBe(2047);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "promotional_notes is varchar(4095), nullable")]
    public void Configure_ShouldConfigurePromotionalNotesColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.PromotionalNotes))!;

        property.GetMaxLength().ShouldBe(4095);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "facebook_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureFacebookUrlColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.FacebookUrl))!;

        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "instagram_url is varchar(255), nullable")]
    public void Configure_ShouldConfigureInstagramUrlColumn()
    {
        var property = _sponsorType.FindProperty(nameof(Sponsor.InstagramUrl))!;

        property.GetMaxLength().ShouldBe(255);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact(DisplayName = "phone numbers map to sponsor_phone_numbers table in app schema")]
    public void Configure_ShouldMapPhoneNumbersToTable()
    {
        _phoneNumberType.GetTableName().ShouldBe("sponsor_phone_numbers");
        _phoneNumberType.GetSchema().ShouldBe(AppDbContext.DefaultSchema);
    }

    [Fact(DisplayName = "phone numbers have sponsor_id foreign key")]
    public void Configure_ShouldConfigurePhoneNumberForeignKey()
    {
        var fk = _phoneNumberType.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == SponsorConfiguration.ForeignKeyName));

        fk.ShouldNotBeNull();
    }

    [Fact(DisplayName = "phone numbers have composite PK of sponsor_id and Type")]
    public void Configure_ShouldConfigurePhoneNumberCompositeKey()
    {
        var pkPropertyNames = _phoneNumberType.FindPrimaryKey()!.Properties
            .Select(p => p.Name)
            .ToList();

        pkPropertyNames.ShouldContain(SponsorConfiguration.ForeignKeyName);
        pkPropertyNames.ShouldContain(nameof(PhoneNumber.Type));
    }
}