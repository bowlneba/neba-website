using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Neba.Api.Security.Domain;

namespace Neba.Api.Security;

/// <summary>
/// Token provider for invite links. It works like the default provider but has its own lifespan, so an
/// invite can stay valid longer than a password reset link.
/// </summary>
internal sealed class InviteTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<InviteTokenProviderOptions> options,
    ILogger<InviteTokenProvider> logger)
    : DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider, options, logger)
{
    /// <summary>
    /// The name the provider is registered under.
    /// </summary>
    public const string ProviderName = "Invite";
}

/// <summary>
/// Options for <see cref="InviteTokenProvider"/>. A separate type keeps its lifespan apart from the default provider's.
/// </summary>
#pragma warning disable S2094 // Empty on purpose: a distinct options type is what gives the invite provider its own lifespan.
internal sealed class InviteTokenProviderOptions : DataProtectionTokenProviderOptions;
#pragma warning restore S2094