namespace Neba.Website.Server;

#pragma warning disable CA1708 // Identifiers should differ by more than case

/// <summary>
/// Extension methods to add infrastructure dependencies to the web app builder.
/// </summary>
internal static class InfrastructureConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Adds infrastructure dependencies to the service collection.
        /// </summary>
        /// <returns>
        /// The updated builder.
        /// </returns>
        public WebApplicationBuilder AddInfrastructure()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.AddKeyVault();
        }

        private WebApplicationBuilder AddKeyVault()
        {
            var keyVaultConnectionString = builder.Configuration.GetConnectionString("keyvault");

            if (string.IsNullOrWhiteSpace(keyVaultConnectionString))
            {
                return builder;
            }

            builder.Configuration.AddAzureKeyVaultSecrets(keyVaultConnectionString);

            return builder;
        }
    }
}