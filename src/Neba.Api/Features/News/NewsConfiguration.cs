namespace Neba.Api.Features.News;

internal static class NewsConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddNews(IConfiguration configuration)
        {
            services.AddOptions<NewsAnnouncementSettings>()
                .Bind(configuration.GetSection(NewsAnnouncementSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
