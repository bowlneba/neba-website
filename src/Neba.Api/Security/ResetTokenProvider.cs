using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Neba.Api.Security.Domain;

namespace Neba.Api.Security;

/// <summary>
/// Token provider for forgot-password and admin reset links. It works like the default provider but has a
/// shorter lifespan. The default provider keeps its one-day lifespan so links emailed before this provider
/// existed still work until they expire.
/// </summary>
internal sealed class ResetTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<ResetTokenProviderOptions> options,
    ILogger<ResetTokenProvider> logger)
    : DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider, options, logger)
{
    /// <summary>
    /// The name the provider is registered under.
    /// </summary>
    public const string ProviderName = "Reset";
}

/// <summary>
/// Options for <see cref="ResetTokenProvider"/>. A separate type keeps its lifespan apart from the other providers'.
/// </summary>
#pragma warning disable S2094 // Empty on purpose: a distinct options type is what gives the reset provider its own lifespan.
internal sealed class ResetTokenProviderOptions : DataProtectionTokenProviderOptions;
#pragma warning restore S2094