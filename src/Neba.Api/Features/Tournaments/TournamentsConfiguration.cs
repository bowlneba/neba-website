namespace Neba.Api.Features.Tournaments;

internal static class TournamentsConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTournaments(IConfiguration configuration)
        {
            services.AddOptions<OilPatternAnnouncementSettings>()
                .Bind(configuration.GetSection(OilPatternAnnouncementSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}